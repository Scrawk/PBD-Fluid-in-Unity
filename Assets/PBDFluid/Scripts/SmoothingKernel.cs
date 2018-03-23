using System;
using UnityEngine;

namespace PBDFluid
{

    public class SmoothingKernel
    {

        public float POLY6 { get; private set; }

        public float SPIKY_GRAD { get; private set; }

        public float VISC_LAP { get; private set; }

        public float Radius { get; private set; }

        public float InvRadius { get; private set; }

        public float Radius2 { get; private set; }

        public SmoothingKernel(float radius)
        {
            Radius = radius;
            Radius2 = radius * radius;
            InvRadius = 1.0f / radius;

            float PI = Mathf.PI;

            POLY6 = 315.0f / (65.0f * PI * Mathf.Pow(Radius, 9.0f));

            SPIKY_GRAD = -45.0f / (PI * Mathf.Pow(Radius, 6.0f));

            VISC_LAP = 45.0f / (PI * Mathf.Pow(Radius, 6.0f));
        }

        float Pow3(float v)
        {
            return v * v * v;
        }

        float Pow2(float v)
        {
            return v * v * v;
        }

        public float Poly6(Vector3 p)
        {
            float r2 = p.sqrMagnitude;
            return Math.Max(0, POLY6 * Pow3(Radius2 - r2));
        }

        public Vector3 SpikyGrad(Vector3 p)
        {
            float r = p.magnitude;

            if(r < Radius)
                return p.normalized * SPIKY_GRAD * Pow2(Radius - r);
            else
                return Vector3.zero;
        }

        public float ViscLap(Vector3 p)
        {
            float r = p.magnitude;

            if (r < Radius)
                return VISC_LAP * (Radius - r);
            else
                return 0;
        }

    }

}