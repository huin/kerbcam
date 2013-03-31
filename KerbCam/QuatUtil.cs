using System;
using UnityEngine;

namespace KerbCam {
    /// <summary>
    /// Utility functions for Quaternions.
    /// </summary>
    public class QuatUtil {
        /// TODO: Fix this.
        /// <summary>
        /// Hermite Quaternion Curve by Kim, Kim and Shin.
        /// </summary>
        /// <param name="t">Interpolation parameter, between 0 and 1.</param>
        /// <param name="qa">Start orientation.</param>
        /// <param name="qb">End orientation.</param>
        /// <param name="wa">Start angular velocity.</param>
        /// <param name="wb">End angular velocity.</param>
        public static Quaternion HermiteQuaternion(float t, ref Quaternion qa, ref Quaternion wa, ref Quaternion qb, ref Quaternion wb) {
            float t2 = t * t;
            float t3 = t2 * t;

            float b1 = 1 - (1 - 3 * t + 3 * t2 - t3);
            float b2 = 3 * t2 - 2 * t3;
            float b3 = t3;

            Quaternion w1, w2, w3;
            w1 = Mul(ref wa, ONE_THIRD);
            w3 = Mul(ref wb, ONE_THIRD);

            // w2 = log(inv(exp(w1)) * inv(qa) * qb * exp(w3))
            w2 = Quaternion.Inverse(Exp(ref w1))
                * Quaternion.Inverse(qa)
                * qb
                * Exp(ref w3);
            Log(out w2, ref w2);

            // q0 * exp(w1 * b1) * exp(w2 * b2) * exp(w3*b3)
            // Values of w1, w2, w3 are destroyed by multiplication in the
            // process.
            Mul(out w1, ref w1, b1); Mul(out w2, ref w2, b2); Mul(out w3, ref w3, b3);
            Exp(out w1, ref w1); Exp(out w2, ref w2); Exp(out w3, ref w3);
            return qa * w1 * w2 * w3;
        }
        
        /// <summary>
        /// Broken implementation of slerp, used to test the basic quaternion
        /// functions.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) {
            Quaternion diff = RotateDiff(a, b);
            Pow(out diff, ref diff, t);
            Quaternion r = a * diff;
            Normalize(out r, ref r);
            return r;
        }

        public static Quaternion RotateDiff(Quaternion a, Quaternion b) {
            return Quaternion.Inverse(a) * b;
        }

        private const float NEAR_ZERO = 1e-7f;
        private const float ONE_THIRD = 1f / 3f;

        public static Quaternion Normalize(ref Quaternion q) {
            Quaternion r;
            Normalize(out r, ref q);
            return r;
        }

        public static void Normalize(out Quaternion r, ref Quaternion q) {
            float len = LenVec(ref q);
            if (len > 0f) {
                Div(out r, ref q, len);
            } else {
                r = Quaternion.identity;
            }
        }

        public static Quaternion Div(ref Quaternion q, float denom) {
            Quaternion r;
            Div(out r, ref q, denom);
            return r;
        }

        public static void Div(out Quaternion r, ref Quaternion q, float denom) {
            Mul(out r, ref q, 1f / denom);
        }

        public static void Div(out Quaternion r, ref Quaternion a, ref Quaternion b) {
            float factor = 1f / AbsSq(ref b);
            r = new Quaternion {
                x = factor * (-a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y),
                y = factor * (-a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x),
                z = factor * (-a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w),
                w = factor * (a.w * b.w + a.x * b.x + a.y * b.y + a.z * b.z)
            };
        }

        public static Quaternion Mul(ref Quaternion q, float denom) {
            Quaternion r;
            Mul(out r, ref q, denom);
            return r;
        }

        public static void Mul(out Quaternion r, ref Quaternion q, float factor) {
            r = new Quaternion { x = q.x * factor, y = q.y * factor, z = q.z * factor, w = q.w * factor };
        }

        public static Quaternion Add(ref Quaternion a, ref Quaternion b) {
            Quaternion r;
            Add(out r, ref a, ref b);
            return r;
        }

        public static void Add(out Quaternion r, ref Quaternion a, ref Quaternion b) {
            r = new Quaternion { x = a.x + b.x, y = a.y + b.y, z = a.z + b.z, w = a.w + b.w };
        }

        public static Quaternion LogInv(ref Quaternion a, ref Quaternion b) {
            Quaternion result = a;
            Quaternion invB = Quaternion.Inverse(b);
            Add(out result, ref result, ref invB);
            Log(out result, ref result);
            return result;
        }

        public static Quaternion SquadInterpolate(float t,
            Quaternion q0, Quaternion q1,
            Quaternion s0, Quaternion s1
            ) {
            return Quaternion.Slerp(
                Quaternion.Slerp(q0, q1, t),
                Quaternion.Slerp(s0, s1, t),
                2f * t * (1f - t));
        }

        public static Quaternion SquadTangent(Quaternion start, Quaternion mid, Quaternion end) {
            Quaternion log1 = LogInv(ref end, ref mid);
            Quaternion log2 = LogInv(ref start, ref mid);

            Quaternion tmp = log1;
            Add(out tmp, ref tmp, ref log2);
            Mul(out tmp, ref tmp, -0.25f);
            Exp(out tmp, ref tmp);

            return tmp * mid;
        }

        public static float Abs(ref Quaternion q) {
            return (float)Math.Sqrt(AbsSq(ref q));
        }

        public static float AbsSq(ref Quaternion q) {
            return q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
        }

        public static float LenVec(ref Quaternion q) {
            return (float)Math.Sqrt(LenVecSq(ref q));
        }

        public static float LenVecSq(ref Quaternion q) {
            return q.x * q.x + q.y * q.y + q.z * q.z;
        }

        public static void Pow(out Quaternion r, ref Quaternion q, float power) {
            Log(out r, ref q);
            Mul(out r, ref r, power);
            Exp(out r, ref r);
        }

        public static Quaternion Exp(ref Quaternion q) {
            Quaternion r;
            Exp(out r, ref q);
            return r;
        }

        public static void Exp(out Quaternion r, ref Quaternion q) {
            float lenV = LenVec(ref q);
            float e = (float)Math.Exp(q.w);
            if (lenV > NEAR_ZERO) {
                float sn = (float)Math.Sin(lenV) / lenV;
                float c = (float)Math.Cos(lenV);
                float esn = e * sn;
                r = new Quaternion { x = esn * q.x, y = esn * q.y, z = esn * q.z, w = e * c };
            } else {
                r = new Quaternion { x = 0, y = 0, z = 0, w = e };
            }
        }

        public static Quaternion Log(ref Quaternion q) {
            Quaternion r;
            Log(out r, ref q);
            return r;
        }

        public static void Log(out Quaternion r, ref Quaternion q) {
            float lenVSq = LenVecSq(ref q);
            float lenV = (float)Math.Sqrt(lenVSq);
            if (lenV > NEAR_ZERO) {
                float len = (float)Math.Sqrt(q.w * q.w + lenVSq);
                float ac = (float)Math.Acos(q.w / len) / lenV;

                r.x = q.x * ac;
                r.y = q.y * ac;
                r.z = q.y * ac;
                r.w = (float)Math.Log(len);
            } else {
                r = new Quaternion { x = 0, y = 0, z = 0, w = 0 };
            }
        }
    }
}