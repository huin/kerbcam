using System;
using UnityEngine;

namespace KerbCam {
    /// <summary>
    /// Utility functions for Quaternions.
    /// </summary>
    public class QuatUtil {
        /// <summary>
        /// Hermite Quaternion Curve by Kim, Kim and Shin.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="qa"></param>
        /// <param name="wa"></param>
        /// <param name="qb"></param>
        /// <param name="wb"></param>
        public static Quaternion HermiteQuaternion(float t, Quaternion qa, Quaternion wa, Quaternion qb, Quaternion wb) {
            float t2 = t * t;
            float t3 = t2 * t;
            float b1 = 1 - (1 - 3 * t + 3 * t2 - t3);
            float b2 = 3 * t2 - 2 * t3;
            float b3 = t3;

            Quaternion w1, w2, w3;
            w1 = wa; Div(ref w1, 3f);
            w3 = wb; Div(ref w3, 3f);

            // w2 = log(inv(exp(w1)) * inv(qa) * qb * exp(w3))
            w2 = w1;
            Exp(ref w1);
            w2 = Quaternion.Inverse(w1);
            w2 *= Quaternion.Inverse(qa);
            w2 *= qb;
            Quaternion tmp;
            tmp = w3; Exp(ref w3);
            w2 *= w3;
            Log(ref w1);

            // q0 * exp(w1 * b1) * exp(w2 * b2) * exp(w3*b3)
            // Values of w1, w2, w3 are destroyed by multiplication in the
            // process.
            Mul(ref w1, b1); Mul(ref w2, b2); Mul(ref w3, b3);
            Exp(ref w1); Exp(ref w2); Exp(ref w3);
            // tmp gets reused.
            tmp = qa;
            tmp *= w1; tmp *= w2; tmp *= w3;
            return tmp;
        }

        public static void Normalize(ref Quaternion r) {
            float len = LenVec(r);
            if (len > 0f) {
                Div(ref r, len);
            } else {
                r = Quaternion.identity;
            }
        }

        public static void Div(ref Quaternion r, float denom) {
            float factor = 1f / denom;
            r.x *= factor;
            r.y *= factor;
            r.z *= factor;
            r.w *= factor;
        }

        public static void Mul(ref Quaternion r, float factor) {
            r.x *= factor;
            r.y *= factor;
            r.z *= factor;
            r.w *= factor;
        }

        public static Quaternion LogInv(Quaternion a, Quaternion b) {
            Quaternion result = Quaternion.Inverse(a);
            result *= b;
            Normalize(ref result);
            Log(ref result);
            return result;
        }

        public static Quaternion SquadInterpolate(float t,
            Quaternion a, Quaternion am,
            Quaternion b, Quaternion bm
            ) {
            Quaternion rotAtoB = Quaternion.Slerp(a, b, t);
            Quaternion rotAmtoBm = Quaternion.Slerp(am, bm, t);
            return Quaternion.Slerp(rotAtoB, rotAmtoBm, 2f * t * (1f - t));
        }

        public static Quaternion SquadTangent(Quaternion start, Quaternion mid, Quaternion end) {
            Quaternion log1 = LogInv(mid, start);
            Quaternion log2 = LogInv(mid, end);

            Quaternion tmp;
            tmp.x = -0.25f * (log1.x + log2.x);
            tmp.y = -0.25f * (log1.y + log2.y);
            tmp.z = -0.25f * (log1.z + log2.z);
            tmp.w = -0.25f * (log1.w + log2.w);
            Exp(ref tmp);
            return mid * tmp;
        }

        public static float Abs(Quaternion q) {
            return (float)Math.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        }

        public static float LenVec(Quaternion q) {
            return (float)Math.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z);
        }

        public static void Exp(ref Quaternion r) {
            float lenV = LenVec(r);
            float e = (float)Math.Exp(r.w);
            float sin = (float)Math.Sin(lenV);

            // Mutate r below:
            r.w = e * (float)Math.Cos(lenV);
            if (Math.Abs(sin) > 0f) {
                float factor = e * sin / lenV;
                r.x *= factor;
                r.y *= factor;
                r.z *= factor;
            }
        }

        public static void Log(ref Quaternion r) {
            float abs = Abs(r);
            if (abs > 0f) {
                float acos = (float)Math.Acos(r.w / abs);
                float lenV = LenVec(r);

                // Mutate r below:
                r.w = (float)Math.Log(abs);
                if (lenV > 0f) {
                    float factor = acos / lenV;
                    r.x *= factor;
                    r.y *= factor;
                    r.z *= factor;
                }
            }
        }
    }
}