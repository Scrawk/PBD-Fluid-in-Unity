using UnityEngine;
using System;
using System.Collections.Generic;

namespace PBDFluid
{

    public enum SIMULATION_SIZE {  LOW, MEDIUM, HIGH }

    public class FluidBodyDemo : MonoBehaviour{
        private const float timeStep = 1.0f / 60.0f;
        private bool m_hasStarted = false;
        public Material m_fluidParticleMat;
        public List<Material> m_boundaryParticleMats;
        public Material m_volumeMat;
        public bool m_drawLines = true;
        public bool m_drawGrid = false;
        public bool m_drawBoundaryParticles = false;
        public bool m_createInnerCup = false;
        public bool m_drawFluidParticles = false;
        public bool m_drawFluidVolume = true;
        public SIMULATION_SIZE m_simulationSize = SIMULATION_SIZE.MEDIUM;
        public bool m_run = true;
        public Mesh m_sphereMesh;
        private FluidBody m_fluid;
        private List<FluidBoundary> m_boundaries;
        private FluidSolver m_solver;
        private RenderVolume m_volume;
        Bounds m_fluidSource, m_outerSource, m_innerSource;
        List<(Bounds,Bounds)> m_sources;
        private bool wasError;

        private void StartDemo()
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
                m_boundaries = new List<FluidBoundary>();
                m_sources = new List<(Bounds, Bounds)>();
                Vector3 min = new Vector3(-10, 0, -2);
                Vector3 max = new Vector3(10,10, 2);
                CreateBoundary(radius, density, min, max);
                if (m_createInnerCup){
                    CreateBoundary(.05f,1000.0f,new Vector3(-5,2.5f,-1.5f),new Vector3(5,7.5f,1.5f));
                    m_createInnerCup = false;
                }
                CreateFluid(radius, density);

                m_fluid.Bounds = m_boundaries[0].Bounds;
                m_solver = new FluidSolver(m_fluid, m_boundaries);

                m_volume = new RenderVolume(m_boundaries[0].Bounds, radius);
                m_volume.CreateMesh(m_volumeMat);
                //TODO: Add several bounds variables in solver
            }
            catch{
                wasError = true;
                throw;
            }
            m_hasStarted = true;
        }

        private void Update()
        {
            if(wasError) return;
            if (!m_hasStarted){
                if (m_run){
                    StartDemo();
                }
                return;
            }
            if (m_createInnerCup){
                CreateBoundary(.05f,1000.0f,new Vector3(-5,2.5f,-1.5f),new Vector3(5,7.5f,1.5f));
                m_createInnerCup = false;
            }
            if (m_run)
            {   
                m_solver.StepPhysics(timeStep, transform.position);
                m_volume.FillVolume(m_fluid, m_solver.Hash, m_solver.Kernel);
            }

            m_volume.Hide = !m_drawFluidVolume;

            if (m_drawBoundaryParticles){
                for (int i=0; i<m_boundaries.Count; i++){
                    m_boundaries[i].Draw(Camera.main, m_sphereMesh, m_boundaryParticleMats[i], 0, Color.red);
                }
            }

            if (m_drawFluidParticles)
                m_fluid.Draw(Camera.main, m_sphereMesh, m_fluidParticleMat, 0);
        }

        private void OnDestroy()
        {
            foreach (FluidBoundary boundary in m_boundaries){
                boundary.Dispose();
            }
            m_boundaries.Clear();
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
                
                //Outer Container Bounds
                foreach ((Bounds,Bounds) boundsTuple in m_sources){
                    DrawBounds(camera, Color.green, boundsTuple.Item1);
                    DrawBounds(camera, Color.green, boundsTuple.Item2);
                }

                // //Inner Cup Bounds
                // DrawBounds(camera, Color.red, m_outerSource2);
                // DrawBounds(camera, Color.red, m_innerSource2);
                
                DrawBounds(camera, Color.blue, m_fluidSource);
            }

            if(m_drawGrid)
            {
                m_solver.Hash.DrawGrid(camera, Color.yellow);
            }
        }

        private void CreateBoundary(float radius, float density, Vector3 min, Vector3 max)
        {
            // Bounds innerBounds = new Bounds();
            // Vector3 min = new Vector3(-8, 0, -2);
            // min = transform.position + min;
            // Vector3 max = new Vector3(8, 10, 2);
            // max = transform.position + max;
            // innerBounds.SetMinMax(min, max);

            Bounds innerBounds = new Bounds();
            
            min = transform.position + min;
            
            max = transform.position + max;
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
            
            m_boundaries.Add(new FluidBoundary(source, radius, density, transform.localToWorldMatrix));
            //m_boundary = new FluidBoundary(source, radius, density, Matrix4x4.identity);
            m_sources.Add((innerBounds,outerBounds));
            m_innerSource = innerBounds;
            m_outerSource = outerBounds;
        }

        private void CreateFluid( float radius, float density)
        {
            Bounds bounds = new Bounds();
            Vector3 min = new Vector3(-8, 0, -1);
            min = transform.position + min;
            Vector3 max = new Vector3(-4, 8, 2);
            max = transform.position + max;

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

            m_fluid = new FluidBody(source, radius, density, transform.localToWorldMatrix);

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
            DrawLines.Draw(cam, m_corners, col, transform.localToWorldMatrix, m_cube);
        }

    }

}
