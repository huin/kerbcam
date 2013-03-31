using System;

namespace KerbCam {
    /// <summary>
    /// Utility functions for splines.
    /// </summary>
    public class SplineUtil {
        public static bool AreParamsClose(float t0, float t1) {
            float dt = t0 - t1;
            return Math.Abs(dt) < 1e-7;
        }

        /// <summary>
        /// Interpolates a value using a the Cubic Hermite spline formula.
        /// http://en.wikipedia.org/wiki/Cubic_Hermite_spline
        /// </summary>
        /// <param name="t">The parameter, this should typically be between 0 and 1.</param>
        /// <param name="p0">The value at t=0.</param>
        /// <param name="m0">The tangent at t=0.</param>
        /// <param name="p1">The value at t=1.</param>
        /// <param name="m1">The tangent at t=1.</param>
        /// <returns>The interpolated value.</returns>
        public static float CubicHermite(float t, float p0, float m0, float p1, float m1) {
            float t2 = t * t;
            float t3 = t2 * t;
            return (
                ((2 * t3) - (3 * t2) + 1) * p0
                + (t3 - (2 * t2) + t) * m0
                + ((-2 * t3) + (3 * t2)) * p1
                + (t3 - t2) * m1
                );
        }

        public static float T(float ta, float tb, float tc, float pa, float pb, float pc) {
            float vab = (pb - pa) / (tb - ta);
            float vbc = (pc - pb) / (tc - tb);
            return (vab + vbc) / 2;
        }
    }
}