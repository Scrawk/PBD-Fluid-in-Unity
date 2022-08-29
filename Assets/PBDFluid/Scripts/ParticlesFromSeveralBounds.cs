using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PBDFluid
{

    public class ParticlesFromSeveralBounds : ParticleSource
    {
        public ParticlesFromBounds[] particlesFromBoundsArray;
        public Matrix4x4[] boundTransformationMatrixArray;
        public List<int> particle2MatrixMap;


        public ParticlesFromSeveralBounds(float spacing, ParticlesFromBounds[] particlesFromBoundsArray, Matrix4x4[] matrixArray) : base(spacing){
            this.particlesFromBoundsArray = particlesFromBoundsArray;
            boundTransformationMatrixArray = matrixArray;
        }

        public override void CreateParticles()
        {
            Positions = new List<Vector3>();
            for (int boundsIndex = 0; boundsIndex < particlesFromBoundsArray.Length; boundsIndex++){
                particlesFromBoundsArray[boundsIndex].CreateParticles();
                Debug.Log("Hoyo "+particlesFromBoundsArray[boundsIndex].NumParticles);
                for (int particleIdx = 0; particleIdx < particlesFromBoundsArray[boundsIndex].NumParticles; particleIdx++) {
                    Positions.Add(particlesFromBoundsArray[boundsIndex].Positions[particleIdx]);
                    // Map each particle to a matrix index
                    particle2MatrixMap[boundsIndex * particleIdx] = boundsIndex;
                }
            }
        }

    }

}