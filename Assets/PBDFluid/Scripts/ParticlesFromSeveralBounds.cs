using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PBDFluid
{

    public class ParticlesFromSeveralBounds : ParticleSource
    {

        public List<Bounds> sources;
        public List<Bounds> BoundsList { get; private set; }
        public List<Bounds> Exclusion { get; private set; }
        private float diameter;
        private float thickness;
        private Transform demoTransform;
        public ParticlesFromSeveralBounds(float spacing, float diameter, float thickness, Transform demoTransform) : base(spacing)
        {
            this.thickness = thickness;
            this.diameter = diameter;
            this.demoTransform = demoTransform;
            sources = new List<Bounds>();
            BoundsList = new List<Bounds>();
            Exclusion = new List<Bounds>();
        }

        public (Bounds,Bounds) AddBoundary(Bounds bounds)
        {
            Bounds outerBounds = new Bounds();
            var center = demoTransform.position + bounds.center;
            var size = bounds.size;
            outerBounds.SetMinMax(center-(size/2.0f), center+(size/2.0f));
                
            //Make the boundary 1 particle thick.
            //The multiple by 1.2 adds a little of extra
            //thickness in case the radius does not evenly
            //divide into the bounds size. You might have
            //particles missing from one side of the source
            //bounds other wise.
            size.x -= diameter * thickness * 1.2f;
            size.y -= diameter * thickness * 1.2f;
            size.z -= diameter * thickness * 1.2f;
            Bounds innerBounds = new Bounds();
            innerBounds.SetMinMax(center-(size/2.0f), center+(size/2.0f));
            //The source will create a array of particles
            //evenly spaced between the inner and outer bounds.
            return (outerBounds, innerBounds);
        }
        public void AddBounds(Bounds bounds)
        {
            sources.Add(bounds);
            var (outerBounds, innerBounds) = AddBoundary(bounds);
            BoundsList.Add(outerBounds);
            Exclusion.Add(innerBounds);
        }

        public void AddBoundses(List<Bounds> boundsList)
        {
            sources.AddRange(boundsList);
            foreach (var source in sources)
            {
                var (outerBounds, innerBounds) = AddBoundary(source);
                BoundsList.Add(outerBounds);
                Exclusion.Add(innerBounds);
            }
        }

        public override void UpdateBounds()
        {
            BoundsList.Clear();
            Exclusion.Clear();
            foreach (var source in sources)
            {
                var (outerBounds, innerBounds) = AddBoundary(source);
                BoundsList.Add(outerBounds);
                Exclusion.Add(innerBounds);
            }
            CreateParticles();
        }

        public override void CreateParticles()
        {
            Positions = new List<Vector3>();
            for (int i=0; i<BoundsList.Count; i++)
            {
                Bounds bounds = BoundsList[i];
                Bounds exclusion = Exclusion[i];
                int numX = (int) ((bounds.size.x + HalfSpacing) / Spacing);
                int numY = (int) ((bounds.size.y + HalfSpacing) / Spacing);
                int numZ = (int) ((bounds.size.z + HalfSpacing) / Spacing);

                for (int z = 0; z < numZ; z++)
                {
                    for (int y = 0; y < numY; y++)
                    {
                        for (int x = 0; x < numX; x++)
                        {
                            Vector3 pos = new Vector3();
                            pos.x = Spacing * x + bounds.min.x + HalfSpacing;
                            pos.y = Spacing * y + bounds.min.y + HalfSpacing;
                            pos.z = Spacing * z + bounds.min.z + HalfSpacing;

                            if (!exclusion.Contains(pos))
                                Positions.Add(pos);
                        }
                    }
                }
            }
            Debug.Log("Particles = "+Positions.Count);
        }
    }

}