using System;
using System.Collections.Generic;
using UnityEngine;

namespace PBDFluid
{

    public class ParticlesFromBounds : ParticleSource
    {

        public Bounds Bounds { get; private set; }

        public List<Bounds> Exclusion { get; private set; }

        public ParticlesFromBounds(float spacing, Bounds bounds) : base(spacing)
        {
            Bounds = bounds;
            Exclusion = new List<Bounds>();
            CreateParticles();
        }

        public ParticlesFromBounds(float spacing, Bounds bounds, Bounds exclusion) : base(spacing)
        {
            Bounds = bounds;
            Exclusion = new List<Bounds>();
            Exclusion.Add(exclusion);
            CreateParticles();
        }

        private void CreateParticles()
        {

            int numX = (int)((Bounds.size.x + HalfSpacing) / Spacing);
            int numY = (int)((Bounds.size.y + HalfSpacing) / Spacing);
            int numZ = (int)((Bounds.size.z + HalfSpacing) / Spacing);

            Positions = new List<Vector3>();

            for (int z = 0; z < numZ; z++)
            {
                for (int y = 0; y < numY; y++)
                {
                    for (int x = 0; x < numX; x++)
                    {
                        Vector3 pos = new Vector3();
                        pos.x = Spacing * x + Bounds.min.x + HalfSpacing;
                        pos.y = Spacing * y + Bounds.min.y + HalfSpacing;
                        pos.z = Spacing * z + Bounds.min.z + HalfSpacing;

                        bool exclude = false;
                        for (int i = 0; i < Exclusion.Count; i++)
                        {
                            if (Exclusion[i].Contains(pos))
                            {
                                exclude = true;
                                break;
                            }
                        }

                        if(!exclude)
                            Positions.Add(pos);
                    }
                }
            }

        }

    }

}