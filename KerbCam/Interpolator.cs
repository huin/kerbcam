using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbCam {

    public class CubicHermiteSpline {
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
        public static float P(float t, float p0, float m0, float p1, float m1) {
            float t2 = t * t;
            float t3 = t2 * t;
            return (
                ((2 * t3) - (3 * t2) + 1) * p0
                + (t3 - (2 * t2) + t) * m0
                + ((-2 * t3) + (3 * t2)) * p1
                + (t3 - t2) * m1
                );
        }
    }

    public struct Key<Value> {
        public float param;
        public Value value;

        public Key(float param, Value value) {
            this.param = param;
            this.value = value;
        }

        public override string ToString() {
            return string.Format("Key({0}, {1})", param, value);
        }
    }

    public class ParamSeries<Value> {
        // frames is maintained sorted on Frame.param.
        private List<Key<Value>> keys = new List<Key<Value>>();

        public Key<Value> this[int index] {
            get { return keys[index]; }
        }

        public int Count {
            get { return keys.Count; }
        }

        public float MinParam {
            get {
                if (keys.Count == 0)
                    return 0f;
                else
                    return keys[0].param;
            }
        }

        public float MaxParam {
            get {
                if (keys.Count == 0)
                    return 0f;
                else
                    return keys[keys.Count - 1].param;
            }
        }

        public int AddKey(float param, Value value) {
            Key<Value> frame = new Key<Value>(param, value);
            if (keys.Count == 0) {
                keys.Add(frame);
                return keys.Count - 1;
            } else {
                int index = FindInsertIndex(param);
                keys.Insert(index, frame);
                return index;
            }

        }

        private int FindInsertIndex(float param) {
            var v = new SortedList<float, Value>();

            if (keys.Count == 0) {
                return 0;
            }
            int lowerIndex = FindLowerIndex(param);
            return lowerIndex + 1;
        }

        // FindIndex returns the highest index such that this[index].param <= param.
        // Returns -1 if param < this[0].param.
        public int FindLowerIndex(float param) {
            return BinSearchFindLowerIndex(param, 0, keys.Count - 1);
        }

        private int BinSearchFindLowerIndex(float param, int lower, int upper) {
            float upperParam = keys[upper].param;
            if (param >= upperParam)
                return upper;

            float lowerParam = keys[lower].param;
            if (param < lowerParam)
                return lower - 1;
            else if (upper - lower == 1)
                return lower;

            int midIndex = lower + (upper - lower) / 2;
            float midParam = keys[midIndex].param;
            if (param == midParam) {
                return midIndex;
            } else if (param < midParam) {
                return BinSearchFindLowerIndex(param, lower, midIndex);
            } else /* (param > midParam) */ {
                return BinSearchFindLowerIndex(param, midIndex, upper);
            }
        }

        public void RemoveAt(int index) {
            keys.RemoveAt(index);
        }

        /// <summary>
        /// Moves the key at the given index to the new param.
        /// </summary>
        /// <param name="index">Index of the key to move.</param>
        /// <param name="param">Parameter.</param>
        /// <returns>The new index.</returns>
        public int MoveKeyAt(int index, float param) {
            Value value = keys[index].value;
            keys.RemoveAt(index);
            return AddKey(param, value);
        }
    }

    /// <summary>
    /// Interpolator that interpolates using 2 points.
    /// </summary>
    /// <typeparam name="Value"></typeparam>
    public class Interpolator2<Value> : ParamSeries<Value> {

        public interface IValueInterpolator {
            /**
             * Interpolates between two values, param is scaled 
             * <param name="a">The value for param=0.</param>
             * <param name="b">The value for param=1.</param>
             * <param name="param">The interpolation parameter, this varies
             * between 0 and 1.</param>
             */
            Value Evaluate(Value a, Value b, float param);
        }

        private IValueInterpolator interpolator;

        public Interpolator2(IValueInterpolator interpolator) {
            this.interpolator = interpolator;
        }

        public Value Evaluate(float param) {
            if (Count == 0)
                return default(Value);

            int lower = FindLowerIndex(param);
            if (lower < 0)
                return this[0].value;
            if (lower >= Count - 1)
                return this[Count - 1].value;

            float lowerT = this[lower].param;
            float upperT = this[lower + 1].param;
            float range = upperT - lowerT;

            // Avoid nasty divide-by-zero math for keys that are very close together in param.
            if (range < 1e-7f) {
                range = 1e-7f;
            }

            Value a = this[lower].value;
            Value b = this[lower + 1].value;

            return interpolator.Evaluate(a, b, (param - lowerT) / range);
        }
    }
}

