using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbCam {
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
             * <param name="a">The value for t=0.</param>
             * <param name="b">The value for t=1.</param>
             * <param name="t">The interpolation parameter, this varies
             * between 0 and 1.</param>
             */
            Value Evaluate(Value a, Value b, float t);
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

            return interpolator.Evaluate(
                this[lower].value, this[lower + 1].value,
                (param - lowerT) / range);
        }
    }

    /// <summary>
    /// Interpolator that interpolates using 4 points.
    /// </summary>
    /// <typeparam name="Value"></typeparam>
    public class Interpolator4<Value> : ParamSeries<Value> {
        public interface IValueInterpolator {
            /// <summary>
            /// Interpolates between two values, with the surrounding values, param is scaled 
            /// <param name="k0">The key prior to a.</param>
            /// <param name="haveK0">Is k0 present.</param>
            /// <param name="k1">The key for t=0.</param>
            /// <param name="k2">The key for t=1.</param>
            /// <param name="k3">The key following b.</param>
            /// <param name="haveK3">Is k3 present.</param>
            /// <param name="t">The interpolation parameter, this varies
            /// between 0 and 1 between k1 and k2.</param>
            Value Evaluate(Key<Value> k0, bool haveK0, Key<Value> k1, Key<Value> k2, Key<Value> k3, bool haveK3, float t);
        }

        private IValueInterpolator interpolator;

        public Interpolator4(IValueInterpolator interpolator) {
            this.interpolator = interpolator;
        }

        public Value Evaluate(float param) {
            if (Count == 0)
                return default(Value);

            int k1Index = FindLowerIndex(param);
            if (k1Index < 0)
                return this[0].value;
            if (k1Index >= Count - 1)
                return this[Count - 1].value;

            int k2Index = k1Index + 1;

            float k1Param = this[k1Index].param;
            float K2Param = this[k2Index].param;
            float range = K2Param - k1Param;

            // Avoid nasty divide-by-zero math for keys that are very close together in param.
            if (range < 1e-7f) {
                range = 1e-7f;
            }

            int k0Index = k1Index - 1;
            bool haveK0 = k0Index >= 0;
            Key<Value> k0;
            if (haveK0) {
                k0 = this[k0Index];
            } else {
                k0 = default(Key<Value>);
            }

            int k3Index = k1Index + 2;
            bool haveK3 = k3Index < Count;
            Key<Value> k3;
            if (haveK3) {
                k3 = this[k3Index];
            } else {
                k3 = default(Key<Value>);
            }

            return interpolator.Evaluate(
                k0, haveK0,
                this[k1Index], this[k2Index],
                k3, haveK3,
                (param - k1Param) / range);
        }
    }
}

