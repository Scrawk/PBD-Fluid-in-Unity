using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace PBDFluid
{

    public class ParticlesFromSeveralBounds : ParticleSource
    {
        public ParticlesFromBounds[] particlesFromBoundsArray;
        public Vector3[] boundsVectors;
        public List<int> particle2MatrixMap;


        public ParticlesFromSeveralBounds(float spacing, ParticlesFromBounds[] particlesFromBoundsArray, Vector3[] boundsVectors) : base(spacing){
            this.particlesFromBoundsArray = particlesFromBoundsArray;
            this.boundsVectors = boundsVectors;
        }

        public override void CreateParticles()
        {
            Positions = new List<Vector3>();
            particle2MatrixMap = new List<int>();
            for (int boundsIndex = 0; boundsIndex < particlesFromBoundsArray.Length; boundsIndex++){
                particlesFromBoundsArray[boundsIndex].CreateParticles();
                Debug.Log($"Boundary at index {boundsIndex} has {particlesFromBoundsArray[boundsIndex].NumParticles} particles!");
                Assert.IsTrue(particlesFromBoundsArray[boundsIndex].NumParticles > 0,
                    $"particlesFromBounds at index {boundsVectors} has 0 NumParticles! its bounds is: {particlesFromBoundsArray[boundsIndex].Bounds}");
                for (int particleIdx = 0; particleIdx < particlesFromBoundsArray[boundsIndex].NumParticles; particleIdx++) {
                    Positions.Add(particlesFromBoundsArray[boundsIndex].Positions[particleIdx]);
                    // Map each particle to a matrix index
                    particle2MatrixMap.Add(boundsIndex);
                }
            }

            Assert.IsTrue(Positions.Count > 0);
        }

    }

}