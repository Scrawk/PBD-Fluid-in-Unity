using UnityEngine;
using System;
using System.Collections.Generic;

namespace PBDFluid
{

    public enum LINE_MODE { LINES, TRIANGLES, TETRAHEDRON  };

    public class DrawLines
    {

        public static LINE_MODE LineMode = LINE_MODE.LINES;

        private static List<Vector4> m_vertices = new List<Vector4>();

        private static Material m_lineMaterial;
        private static Material LineMaterial
        {
            get
            {
                if (m_lineMaterial == null)
                    m_lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                return m_lineMaterial;
            }
        }

        public static void Draw(Camera camera, IList<Vector4> vertices, Color color, Matrix4x4 localToWorld, IList<int> indices = null)
        {
            if (camera == null || vertices == null) return;
            m_vertices.Clear();

            int count = vertices.Count;
            for (int i = 0; i < count; i++)
                m_vertices.Add(vertices[i]);

            DrawVertices(camera, m_vertices, color, localToWorld, indices);
        }

        public static void Draw(Camera camera, IList<Vector3> vertices, Color color, Matrix4x4 localToWorld, IList<int> indices = null)
        {
            if (camera == null || vertices == null) return;
            m_vertices.Clear();

            int count = vertices.Count;
            for (int i = 0; i < count; i++)
                m_vertices.Add(vertices[i]);

            DrawVertices(camera, m_vertices, color, localToWorld, indices);
        }

        public static void Draw(Camera camera, Vector3 a, Vector3 b, Color color, Matrix4x4 localToWorld)
        {
            if (camera == null) return;
            m_vertices.Clear();

            m_vertices.Add(a);
            m_vertices.Add(b);

            DrawVertices(camera, m_vertices, color, localToWorld, null);
        }

        public static void Draw(Camera camera, IList<Vector2> vertices, Color color, Matrix4x4 localToWorld, IList<int> indices = null)
        {
            if (camera == null || vertices == null) return;

            int count = vertices.Count;
            for (int i = 0; i < count; i++)
                m_vertices.Add(vertices[i]);

            DrawVertices(camera, m_vertices, color, localToWorld, indices);
        }

        private static void DrawVertices(Camera camera, IList<Vector4> vertices, Color color, Matrix4x4 localToWorld, IList<int> indices)
        {
            switch (LineMode)
            {
                case LINE_MODE.LINES:
                    DrawVerticesAsLines(camera, color, vertices, localToWorld, indices);
                    break;

                case LINE_MODE.TRIANGLES:
                    DrawVerticesAsTriangles(camera, color, vertices, localToWorld, indices);
                    break;

                case LINE_MODE.TETRAHEDRON:
                    DrawVerticesAsTetrahedron(camera, color, vertices, localToWorld, indices);
                    break;
            }
        }

        private static void DrawVerticesAsLines(Camera camera, Color color, IList<Vector4> vertices, Matrix4x4 localToWorld, IList<int> indices)
        {
            if (camera == null || vertices == null) return;
            if (vertices.Count < 2) return;

            GL.PushMatrix();

            GL.LoadIdentity();
            GL.modelview = camera.worldToCameraMatrix * localToWorld;
            GL.LoadProjectionMatrix(camera.projectionMatrix);

            LineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(color);

            int vertexCount = vertices.Count;

            if(indices != null)
            {
                for (int i = 0; i < indices.Count / 2; i++)
                {
                    int i0 = indices[i * 2 + 0];
                    int i1 = indices[i * 2 + 1];

                    if (i0 < 0 || i0 >= vertexCount) continue;
                    if (i1 < 0 || i1 >= vertexCount) continue;

                    GL.Vertex(vertices[i0]);
                    GL.Vertex(vertices[i1]);
                }
            }
            else
            {
                for (int i = 0; i < vertexCount-1; i++)
                {
                    GL.Vertex(vertices[i ]);
                    GL.Vertex(vertices[i + 1]);
                }
            }

            GL.End();

            GL.PopMatrix();
        }

        private static void DrawVerticesAsTriangles(Camera camera, Color color, IList<Vector4> vertices, Matrix4x4 localToWorld, IList<int> indices)
        {
            if (camera == null || vertices == null) return;
            if (vertices.Count < 3) return;

            GL.PushMatrix();

            GL.LoadIdentity();
            GL.MultMatrix(camera.worldToCameraMatrix);
            GL.LoadProjectionMatrix(camera.projectionMatrix);

            LineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(color);

            int vertexCount = vertices.Count;

            if(indices != null)
            {
                for (int i = 0; i < indices.Count / 3; i++)
                {
                    int i0 = indices[i * 3 + 0];
                    int i1 = indices[i * 3 + 1];
                    int i2 = indices[i * 3 + 2];

                    if (i0 < 0 || i0 >= vertexCount) continue;
                    if (i1 < 0 || i1 >= vertexCount) continue;
                    if (i2 < 0 || i2 >= vertexCount) continue;

                    GL.Vertex(vertices[i0]);
                    GL.Vertex(vertices[i1]);

                    GL.Vertex(vertices[i0]);
                    GL.Vertex(vertices[i2]);

                    GL.Vertex(vertices[i2]);
                    GL.Vertex(vertices[i1]);
                }
            }
            else
            {
                for (int i = 0; i < vertexCount / 3; i++)
                {
                    Vector3 v0 = vertices[i * 3 + 0];
                    Vector3 v1 = vertices[i * 3 + 1];
                    Vector3 v2 = vertices[i * 3 + 2];

                    GL.Vertex(v0);
                    GL.Vertex(v1);

                    GL.Vertex(v0);
                    GL.Vertex(v2);

                    GL.Vertex(v2);
                    GL.Vertex(v1);
                }
            }

            GL.End();

            GL.PopMatrix();
        }

        private static void DrawVerticesAsTetrahedron(Camera camera, Color color, IList<Vector4> vertices, Matrix4x4 localToWorld, IList<int> indices)
        {
            if (camera == null || vertices == null) return;
            if (vertices.Count < 4) return;

            GL.PushMatrix();

            GL.LoadIdentity();
            GL.MultMatrix(camera.worldToCameraMatrix);
            GL.LoadProjectionMatrix(camera.projectionMatrix);

            LineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(color);

            int vertexCount = vertices.Count;

            if(indices != null)
            {
                for (int i = 0; i < indices.Count / 4; i++)
                {
                    int i0 = indices[i * 4 + 0];
                    int i1 = indices[i * 4 + 1];
                    int i2 = indices[i * 4 + 2];
                    int i3 = indices[i * 4 + 3];

                    if (i0 < 0 || i0 >= vertexCount) continue;
                    if (i1 < 0 || i1 >= vertexCount) continue;
                    if (i2 < 0 || i2 >= vertexCount) continue;
                    if (i3 < 0 || i3 >= vertexCount) continue;

                    GL.Vertex(vertices[i0]);
                    GL.Vertex(vertices[i1]);

                    GL.Vertex(vertices[i0]);
                    GL.Vertex(vertices[i2]);

                    GL.Vertex(vertices[i0]);
                    GL.Vertex(vertices[i3]);

                    GL.Vertex(vertices[i1]);
                    GL.Vertex(vertices[i2]);

                    GL.Vertex(vertices[i3]);
                    GL.Vertex(vertices[i2]);

                    GL.Vertex(vertices[i1]);
                    GL.Vertex(vertices[i3]);
                }
            }
            else
            {
                for (int i = 0; i < vertexCount / 4; i++)
                {
                    Vector3 v0 = vertices[i * 4 + 0];
                    Vector3 v1 = vertices[i * 4 + 1];
                    Vector3 v2 = vertices[i * 4 + 2];
                    Vector3 v3 = vertices[i * 4 + 3];


                    GL.Vertex(v0);
                    GL.Vertex(v1);

                    GL.Vertex(v0);
                    GL.Vertex(v2);

                    GL.Vertex(v0);
                    GL.Vertex(v3);

                    GL.Vertex(v1);
                    GL.Vertex(v2);

                    GL.Vertex(v3);
                    GL.Vertex(v2);

                    GL.Vertex(v1);
                    GL.Vertex(v3);
                }
            }

            GL.End();

            GL.PopMatrix();
        }

    }

}