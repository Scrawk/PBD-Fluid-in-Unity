using System;
using System.Collections.Generic;
using UnityEngine;

namespace PBDFluid
{

    public class GridHash : IDisposable
    {

        private const int THREADS = 128;
        private const int READ = 0;
        private const int WRITE = 1;

        public int TotalParticles { get; private set; }

        public Bounds Bounds;

        public float CellSize { get; private set; }

        public float InvCellSize { get; private set; }

        public int Groups { get; private set; }

        /// <summary>
        /// Contains the particles hash value (x) and
        /// the particles index in its position array (y)
        /// </summary>
        public ComputeBuffer IndexMap { get; private set; }

        /// <summary>
        /// Is a 3D grid representing the hash cells.
        /// Contans where the sorted hash values start (x) 
        /// and end (y) for each cell.
        /// </summary>
        public ComputeBuffer Table { get; private set; }

        private BitonicSort m_sort;

        private ComputeShader m_shader;

        private int m_hashKernel, m_clearKernel, m_mapKernel;

        public GridHash(Bounds bounds, int numParticles, float cellSize)
        {
            TotalParticles = numParticles;
            CellSize = cellSize;
            InvCellSize = 1.0f / CellSize;

            Groups = TotalParticles / THREADS;
            if (TotalParticles % THREADS != 0) Groups++;

            Vector3 min, max;
            min = bounds.min;

            max.x = min.x + (float)Math.Ceiling(bounds.size.x / CellSize);
            max.y = min.y + (float)Math.Ceiling(bounds.size.y / CellSize);
            max.z = min.z + (float)Math.Ceiling(bounds.size.z / CellSize);

            Bounds = new Bounds();
            Bounds.SetMinMax(min, max);

            int width = (int)Bounds.size.x;
            int height = (int)Bounds.size.y;
            int depth = (int)Bounds.size.z;

            int size = width * height * depth;

            IndexMap = new ComputeBuffer(TotalParticles, 2 * sizeof(int));
            Table = new ComputeBuffer(size, 2 * sizeof(int));

            m_sort = new BitonicSort(TotalParticles);

            m_shader = Resources.Load("GridHash") as ComputeShader;
            m_hashKernel = m_shader.FindKernel("HashParticles");
            m_clearKernel = m_shader.FindKernel("ClearTable");
            m_mapKernel = m_shader.FindKernel("MapTable");
        }

        public Bounds WorldBounds
        {
            get
            {
                Vector3 min = Bounds.min;
                Vector3 max = min + Bounds.size * CellSize;

                Bounds bounds = new Bounds();
                bounds.SetMinMax(min, max);

                return bounds;
            }
        }

        public void Dispose()
        {
            m_sort.Dispose();

            if (IndexMap != null)
            {
                IndexMap.Release();
                IndexMap = null;
            }

            if (Table != null)
            {
                Table.Release();
                Table = null;
            }
        }

        public void Process(ComputeBuffer particles)
        {
            if (particles.count != TotalParticles)
                throw new ArgumentException("particles.Length != TotalParticles");

            m_shader.SetInt("NumParticles", TotalParticles);
            m_shader.SetInt("TotalParticles", TotalParticles);
            m_shader.SetFloat("HashScale", InvCellSize);
            m_shader.SetVector("HashSize", Bounds.size);
            m_shader.SetVector("HashTranslate", Bounds.min);

            m_shader.SetBuffer(m_hashKernel, "Particles", particles);
            m_shader.SetBuffer(m_hashKernel, "Boundary", particles); //unity 2018 complains if boundary not set in kernel
            m_shader.SetBuffer(m_hashKernel, "IndexMap", IndexMap);

            //Assign the particles hash to x and index to y.
            m_shader.Dispatch(m_hashKernel, Groups, 1, 1);

            MapTable();
        }

        public void Process(ComputeBuffer particles, ComputeBuffer boundary)
        {
            int numParticles = particles.count;
            int numBoundary = boundary.count;

            if (numParticles + numBoundary != TotalParticles)
                throw new ArgumentException("numParticles + numBoundary != TotalParticles");

            m_shader.SetInt("NumParticles", numParticles);
            m_shader.SetInt("TotalParticles", TotalParticles);
            m_shader.SetFloat("HashScale", InvCellSize);
            m_shader.SetVector("HashSize", Bounds.size);
            m_shader.SetVector("HashTranslate", Bounds.min);

            m_shader.SetBuffer(m_hashKernel, "Particles", particles);
            m_shader.SetBuffer(m_hashKernel, "Boundary", boundary);
            m_shader.SetBuffer(m_hashKernel, "IndexMap", IndexMap);

            //Assign the particles hash to x and index to y.
            m_shader.Dispatch(m_hashKernel, Groups, 1, 1);

            MapTable();
        }

        private void MapTable()
        {
            //First sort by the hash values in x.
            //Uses bitonic sort but any other method will work.
            m_sort.Sort(IndexMap);

            m_shader.SetInt("TotalParticles", TotalParticles);
            m_shader.SetBuffer(m_clearKernel, "Table", Table);

            //Clear the previous tables values as not all
            //locations will be written to when mapped.
            m_shader.Dispatch(m_clearKernel, Groups, 1, 1);

            m_shader.SetBuffer(m_mapKernel, "IndexMap", IndexMap);
            m_shader.SetBuffer(m_mapKernel, "Table", Table);

            //For each hash cell find where the index map
            //start and ends for that hash value.
            m_shader.Dispatch(m_mapKernel, Groups, 1, 1);
        }

        /// <summary>
        /// Draws the hash grid for debugging.
        /// </summary>
        public void DrawGrid(Camera cam, Color col)
        {

            float width = Bounds.size.x;
            float height = Bounds.size.y;
            float depth = Bounds.size.z;

            DrawLines.LineMode = LINE_MODE.LINES;

            for (float y = 0; y <= height; y++)
            {
                for (float x = 0; x <= width; x++)
                {
                    Vector3 a = Bounds.min + new Vector3(x, y, 0) * CellSize;
                    Vector3 b = Bounds.min + new Vector3(x, y, depth) * CellSize;

                    DrawLines.Draw(cam, a, b, col, Matrix4x4.identity);
                }

                for (float z = 0; z <= depth; z++)
                {
                    Vector3 a = Bounds.min + new Vector3(0, y, z) * CellSize;
                    Vector3 b = Bounds.min + new Vector3(width, y, z) * CellSize;

                    DrawLines.Draw(cam, a, b, col, Matrix4x4.identity);
                }
            }

            for (float z = 0; z <= depth; z++)
            {
                for (float x = 0; x <= width; x++)
                {
                    Vector3 a = Bounds.min + new Vector3(x, 0, z) * CellSize;
                    Vector3 b = Bounds.min + new Vector3(x, height, z) * CellSize;

                    DrawLines.Draw(cam, a, b, col, Matrix4x4.identity);
                }
            }

        }

    }
}
