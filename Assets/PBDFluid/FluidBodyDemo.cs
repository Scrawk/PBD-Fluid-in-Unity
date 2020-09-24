using UnityEngine;
using System;
using System.Collections.Generic;

namespace PBDFluid
{

    public enum SIMULATION_SIZE {  LOW, MEDIUM, HIGH }

    public class FluidBodyDemo : MonoBehaviour
    {

        private const float timeStep = 1.0f / 60.0f;

        public Material m_fluidParticleMat;

        public Material m_boundaryParticleMat;

        public Material m_volumeMat;

        public bool m_drawLines = true;

        public bool m_drawGrid = false;

        public bool m_drawBoundaryParticles = false;

        public bool m_drawFluidParticles = false;

        public bool m_drawFluidVolume = true;

        public SIMULATION_SIZE m_simulationSize = SIMULATION_SIZE.MEDIUM;

        public bool m_run = true;

        public Mesh m_sphereMesh;

        private FluidBody m_fluid;

        private FluidBoundary m_boundary;

        private FluidSolver m_solver;

        private RenderVolume m_volume;

        Bounds m_fluidSource, m_outerSource, m_innerSource;

        private bool wasError;

        private void Start()
        {
            float radius = 0.08f;
            float density = 1000.0f;

            //A smaller radius means more particles.
            //If the number of particles is to low or high
            //the bitonic sort shader will throw a exception 
            //as it has a set range it can handle but this 
            //can be manually changes in the BitonicSort.cs script.

            //A smaller radius may also requre more solver steps
            //as the boundary is thinner and the fluid many step
            //through and leak out.

            switch(m_simulationSize)
            {
                case SIMULATION_SIZE.LOW:
                    radius = 0.1f;
                    break;

                case SIMULATION_SIZE.MEDIUM:
                    radius = 0.08f;
                    break;

                case SIMULATION_SIZE.HIGH:
                    radius = 0.06f;
                    break;
            }

            try
            {
                CreateBoundary(radius, density);
                CreateFluid(radius, density);

                m_fluid.Bounds = m_boundary.Bounds;

                m_solver = new FluidSolver(m_fluid, m_boundary);

                m_volume = new RenderVolume(m_boundary.Bounds, radius);
                m_volume.CreateMesh(m_volumeMat);
            }
            catch
            {
                wasError = true;
                throw;
            }
        }

        private void Update()
        {
            if(wasError) return;

            if (m_run)
            {
                m_solver.StepPhysics(timeStep);
                m_volume.FillVolume(m_fluid, m_solver.Hash, m_solver.Kernel);
            }

             m_volume.Hide = !m_drawFluidVolume;

            if (m_drawBoundaryParticles)
                m_boundary.Draw(Camera.main, m_sphereMesh, m_boundaryParticleMat, 0);

            if (m_drawFluidParticles)
                m_fluid.Draw(Camera.main, m_sphereMesh, m_fluidParticleMat, 0);
        }

        private void OnDestroy()
        {
            m_boundary.Dispose();
            m_fluid.Dispose();
            m_solver.Dispose();
            m_volume.Dispose();
        }

        private void OnRenderObject()
        {
            Camera camera = Camera.current;
            if (camera != Camera.main) return;

            if (m_drawLines)
            {
                //DrawBounds(camera, Color.green, m_boundary.Bounds);
                //DrawBounds(camera, Color.blue, m_fluid.Bounds);

                DrawBounds(camera, Color.green, m_outerSource);
                DrawBounds(camera, Color.red, m_innerSource);
                DrawBounds(camera, Color.blue, m_fluidSource);
            }

            if(m_drawGrid)
            {
                m_solver.Hash.DrawGrid(camera, Color.yellow);
            }
        }

        private void CreateBoundary(float radius, float density)
        {
            Bounds innerBounds = new Bounds();
            Vector3 min = new Vector3(-8, 0, -2);
            Vector3 max = new Vector3(8, 10, 2);
            innerBounds.SetMinMax(min, max);

            //Make the boundary 1 particle thick.
            //The multiple by 1.2 adds a little of extra
            //thickness in case the radius does not evenly
            //divide into the bounds size. You might have
            //particles missing from one side of the source
            //bounds other wise.

            float thickness = 1;
            float diameter = radius * 2;
            min.x -= diameter * thickness * 1.2f;
            min.y -= diameter * thickness * 1.2f;
            min.z -= diameter * thickness * 1.2f;

            max.x += diameter * thickness * 1.2f;
            max.y += diameter * thickness * 1.2f;
            max.z += diameter * thickness * 1.2f;

            Bounds outerBounds = new Bounds();
            outerBounds.SetMinMax(min, max);

            //The source will create a array of particles
            //evenly spaced between the inner and outer bounds.
            ParticleSource source = new ParticlesFromBounds(diameter, outerBounds, innerBounds);
            Debug.Log("Boundary Particles = " + source.NumParticles);

            m_boundary = new FluidBoundary(source, radius, density, Matrix4x4.identity);

            m_innerSource = innerBounds;
            m_outerSource = outerBounds;
        }

        private void CreateFluid( float radius, float density)
        {
            Bounds bounds = new Bounds();
            Vector3 min = new Vector3(-8, 0, -1);
            Vector3 max = new Vector3(-4, 8, 2);

            min.x += radius;
            min.y += radius;
            min.z += radius;

            max.x -= radius;
            max.y -= radius;
            max.z -= radius;

            bounds.SetMinMax(min, max);

            //The source will create a array of particles
            //evenly spaced inside the bounds. 
            //Multiple the spacing by 0.9 to pack more
            //particles into bounds.
            float diameter = radius * 2;
            ParticlesFromBounds source = new ParticlesFromBounds(diameter * 0.9f, bounds);
            Debug.Log("Fluid Particles = " + source.NumParticles);

            m_fluid = new FluidBody(source, radius, density, Matrix4x4.identity);

            m_fluidSource = bounds;
        }

        private static IList<int> m_cube = new int[]
        {
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        };

        Vector4[] m_corners = new Vector4[8];
        public void GetCorners(Bounds b)
        {
            m_corners[0] = new Vector4(b.min.x, b.min.y, b.min.z, 1);
            m_corners[1] = new Vector4(b.min.x, b.min.y, b.max.z, 1);
            m_corners[2] = new Vector4(b.max.x, b.min.y, b.max.z, 1);
            m_corners[3] = new Vector4(b.max.x, b.min.y, b.min.z, 1);

            m_corners[4] = new Vector4(b.min.x, b.max.y, b.min.z, 1);
            m_corners[5] = new Vector4(b.min.x, b.max.y, b.max.z, 1);
            m_corners[6] = new Vector4(b.max.x, b.max.y, b.max.z, 1);
            m_corners[7] = new Vector4(b.max.x, b.max.y, b.min.z, 1);
        }

        private void DrawBounds(Camera cam, Color col, Bounds bounds)
        {
            GetCorners(bounds);
            DrawLines.LineMode = LINE_MODE.LINES;
            DrawLines.Draw(cam, m_corners, col, Matrix4x4.identity, m_cube);
        }

    }

}
