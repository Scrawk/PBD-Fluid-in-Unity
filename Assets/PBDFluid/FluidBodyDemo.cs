using UnityEngine;
using System.Collections.Generic;

namespace PBDFluid
{

    public enum SIMULATION_SIZE {  LOW, MEDIUM, HIGH }

    public class FluidBodyDemo : MonoBehaviour
    {
        //Constants
        private const float timeStep = 1.0f / 60.0f;
        
        //Serialized Fields
        public Camera m_mainCamera;
        public Bounds simulationBounds;
        public Bounds fluidBounds;
        public List<Bounds> boundaryInfos = new List<Bounds>();

        [Header("Materials")]
        public Material m_fluidParticleMat;
        public Material m_boundaryParticleMat;
        public Material m_volumeMat;

        [Header("Render Booleans")]
        public bool m_drawLines = true;
        public bool m_drawGrid = false;
        public bool m_drawBoundaryParticles = false;
        public bool m_drawFluidParticles = false;
        public bool m_drawFluidVolume = true;

        [Header("Simulation Settings")]
        public SIMULATION_SIZE m_simulationSize = SIMULATION_SIZE.MEDIUM;

        private float radius = 0.01f;
        private float density;

        private bool m_hasStarted = false;
        public bool m_run = true;
        public Mesh m_sphereMesh;
        private FluidBody m_fluid;
        private FluidBoundary _boundary;
        private FluidSolver m_solver;
        private RenderVolume m_volume;
        Bounds m_fluidSource;
        private bool wasError;
        private ParticlesFromSeveralBounds particleSource;
        private ComputeBuffer particles2BoundsBuffer;
        private ComputeBuffer boundsVectorsBuffer;

        private ComputeBuffer GenerateParticles2BoundsBuffer()
        {
            var particle2BoundsArray = particleSource.particle2MatrixMap.ToArray();
            var buffer = new ComputeBuffer(particle2BoundsArray.Length, sizeof(int));
            buffer.SetData(particle2BoundsArray);
            return buffer;
        }
        
        private ComputeBuffer GenerateBoundsVectorsBuffer()
        {
            var buffer = new ComputeBuffer(particleSource.boundsVectors.Length, 3 * sizeof(float));
            buffer.SetData(particleSource.boundsVectors);
            return buffer;
        }
        
        private void StartDemo()
        {
            radius = 0.08f;
            density = 1000.0f;

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

                CreateBoundaries();
                CreateFluid(radius, density, fluidBounds.center, fluidBounds.size);
                var bounds = simulationBounds;
                bounds.center += transform.position;
                m_fluid.Bounds = bounds;
                particles2BoundsBuffer = GenerateParticles2BoundsBuffer();
                boundsVectorsBuffer = GenerateBoundsVectorsBuffer();
                m_solver = new FluidSolver(m_fluid, bounds, _boundary,particles2BoundsBuffer,boundsVectorsBuffer);

                m_volume = new RenderVolume(bounds, radius);
                m_volume.CreateMesh(m_volumeMat);
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
            if (m_run){
                m_solver.StepPhysics(timeStep);
                m_volume.FillVolume(m_fluid, m_solver.Hash, m_solver.Kernel);
            }

            m_volume.Hide = !m_drawFluidVolume;

            if (m_drawBoundaryParticles){
                _boundary.Draw(m_mainCamera, m_sphereMesh, m_boundaryParticleMat, 0, Color.red);
            }

            if (m_drawFluidParticles){
                m_fluid.Draw(m_mainCamera, m_sphereMesh, m_fluidParticleMat, 0);
            }
        }

        private void OnDestroy()
        {
            particles2BoundsBuffer.Dispose();
            boundsVectorsBuffer.Dispose();
            _boundary.Dispose();
            m_fluid.Dispose();
            m_solver.Dispose();
            m_volume.Dispose();
        }

        private void OnRenderObject()
        {
            Camera camera = Camera.current;
            if (camera != Camera.main) return;

            if (m_drawLines){                
                //Outer Container Bounds
                // foreach (Bounds bounds in particleSource.BoundsList){
                //     DrawBounds(camera, Color.green, bounds);
                // }
                // foreach (Bounds bounds in particleSource.Exclusion){
                //     DrawBounds(camera, Color.red, bounds);
                // }

                DrawBounds(camera, Color.blue, m_fluidSource);
            }

            if(m_drawGrid){
                m_solver.Hash.DrawGrid(camera, Color.yellow);
            }
        }

        private ParticlesFromBounds CreateBoundary(Bounds bounds)
        {
            Bounds outerBounds = new Bounds();
            var center = transform.position + bounds.center;
            var size = bounds.size;
            outerBounds.SetMinMax(center-(size/2.0f), center+(size/2.0f));
            // outerBounds.SetMinMax(size/2.0f, size/2.0f);
                
            //Make the boundary 1 particle thick.
            //The multiple by 1.2 adds a little of extra
            //thickness in case the radius does not evenly
            //divide into the bounds size. You might have
            //particles missing from one side of the source
            //bounds other wise.
            float thickness = 1;
            float diameter = radius * 2;
            size.x -= diameter * thickness * 1.2f;
            size.y -= diameter * thickness * 1.2f;
            size.z -= diameter * thickness * 1.2f;
            Bounds innerBounds = new Bounds();
            innerBounds.SetMinMax(center-(size/2.0f), center+(size/2.0f));
            //The source will create a array of particles
            //evenly spaced between the inner and outer bounds.
            return new ParticlesFromBounds(diameter, outerBounds,innerBounds);
        }
        private void CreateBoundaries() {
            ParticlesFromBounds[] particlesFromBoundsArray = new ParticlesFromBounds[boundaryInfos.Count];
            Vector3[] boundsVectors = new Vector3[boundaryInfos.Count];
            for (var i = 0; i < boundaryInfos.Count; i++) {
                particlesFromBoundsArray[i] = CreateBoundary(boundaryInfos[i]);
                boundsVectors[i] = boundaryInfos[i].center;
            }
            particleSource = new ParticlesFromSeveralBounds(radius * 2, particlesFromBoundsArray, boundsVectors);

            particleSource.CreateParticles();
            
            _boundary = new FluidBoundary(particleSource, radius, density, transform.localToWorldMatrix);
        }

        private void CreateFluid( float radius, float density, Vector3 center, Vector3 size)
        {
            Bounds bounds = new Bounds();
            center += transform.position;
            size.x += radius;
            size.y += radius;
            size.z += radius;

            bounds.SetMinMax(center-size/2.0f, center+size/2.0f);

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
            DrawLines.Draw(cam, m_corners, col, Matrix4x4.identity, m_cube);
        }

        private void OnDrawGizmos() {
            //Simulation Bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position,simulationBounds.size);

            //Water Box
            DrawFluidBodyGizmo();
            
            float thickness = 1;
            float diameter = radius * 2;
            //Extra Boundaries
            boundaryInfos.ForEach(bounds => DrawBoundaryGizmo(bounds,diameter,thickness));
        }

        private void DrawFluidBodyGizmo()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position+fluidBounds.center,fluidBounds.size);
        }
        private void DrawBoundaryGizmo(Bounds bounds, float diameter, float thickness) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position+bounds.center,bounds.size);
            Vector3 newsize = bounds.size;
            newsize.x -= diameter * thickness * 1.2f;
            newsize.y -= diameter * thickness * 1.2f;
            newsize.z -= diameter * thickness * 1.2f;
            Gizmos.DrawWireCube(transform.position+bounds.center,newsize);
        }

    }

}
