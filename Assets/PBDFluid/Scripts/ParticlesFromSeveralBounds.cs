using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PBDFluid
{

    public class ParticlesFromSeveralBounds : ParticleSource
    {

        public List<Bounds> BoundsList { get; private set; }

        public List<Bounds> Exclusion { get; private set; }

        public ParticlesFromSeveralBounds(float spacing) : base(spacing)
        {
            BoundsList = new List<Bounds>();
            Exclusion = new List<Bounds>();
        }

        public void AddBounds(Bounds bounds, Bounds exclusion)
        {  
           BoundsList.Add(bounds);
           Exclusion.Add(exclusion);
        }

        public void CreateParticles()
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