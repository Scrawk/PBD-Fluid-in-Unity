using System;
using System.Collections.Generic;
using UnityEngine;

namespace PBDFluid
{

    public abstract class ParticleSource
    {

        public int NumParticles { get { return Positions.Count; } }

        public IList<Vector3> Positions { get; protected set; }

        public float Spacing { get; private set; }

        public float HalfSpacing {  get { return Spacing * 0.5f; } }

        public ParticleSource(float spacing)
        {
            Spacing = spacing;
        }

    }

}