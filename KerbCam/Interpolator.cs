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
        private List<Key<Value>> frames = new List<Key<Value>>();

        public Key<Value> this[int index] {
            get { return frames[index]; }
        }

        public int Count {
            get { return frames.Count; }
        }

        public float MinParam {
            get {
                if (frames.Count == 0)
                    return 0f;
                else
                    return frames[0].param;
            }
        }

        public float MaxParam {
            get {
                if (frames.Count == 0)
                    return 0f;
                else
                    return frames[frames.Count - 1].param;
            }
        }

        public int AddKey(float param, Value value) {
            Key<Value> frame = new Key<Value>(param, value);
            if (frames.Count == 0) {
                frames.Add(frame);
                return frames.Count - 1;
            } else {
                int index = FindInsertIndex(param);
                frames.Insert(index, frame);
                return index;
            }

        }

        private int FindInsertIndex(float param) {
            var v = new SortedList<float, Value>();

            if (frames.Count == 0) {
                return 0;
            }
            int lowerIndex = FindLowerIndex(param);
            return lowerIndex + 1;
        }

        // FindIndex returns the highest index such that values[index].param <= param.
        // Returns -1 if param < values[0].param.
        public int FindLowerIndex(float param) {
            return BinSearchFindLowerIndex(param, 0, frames.Count - 1);
        }

        private int BinSearchFindLowerIndex(float param, int lower, int upper) {
            float upperParam = frames[upper].param;
            if (param >= upperParam)
                return upper;

            float lowerParam = frames[lower].param;
            if (param < lowerParam)
                return lower - 1;
            else if (upper - lower == 1)
                return lower;

            int midIndex = lower + (upper - lower) / 2;
            float midParam = frames[midIndex].param;
            if (param == midParam) {
                return midIndex;
            } else if (param < midParam) {
                return BinSearchFindLowerIndex(param, lower, midIndex);
            } else /* (param > midParam) */ {
                return BinSearchFindLowerIndex(param, midIndex, upper);
            }
        }

        public void RemoveAt(int index) {
            frames.RemoveAt(index);
        }

        /// <summary>
        /// Moves the key at the given index to the new param.
        /// </summary>
        /// <param name="index">Index of the key to move.</param>
        /// <param name="param">Parameter.</param>
        /// <returns>The new index.</returns>
        public int MoveKeyAt(int index, float param) {
            Value value = frames[index].value;
            frames.RemoveAt(index);
            return AddKey(param, value);
        }
    }

    public class Interpolator<Value> {

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

        private ParamSeries<Value> paramSeries = new ParamSeries<Value>();

        private IValueInterpolator interpolator;

        public Interpolator(IValueInterpolator interpolator) {
            this.interpolator = interpolator;
        }

        public ParamSeries<Value> Keys {
            get { return paramSeries; }
        }

        public Value Evaluate(float param) {
            if (paramSeries.Count == 0)
                return default(Value);

            int lower = paramSeries.FindLowerIndex(param);
            if (lower < 0)
                return paramSeries[0].value;
            if (lower >= paramSeries.Count - 1)
                return paramSeries[paramSeries.Count - 1].value;

            float lowerT = paramSeries[lower].param;
            float upperT = paramSeries[lower + 1].param;
            float range = upperT - lowerT;

            // Avoid nasty divide-by-zero math for keys that are very close together in param.
            if (range < 1e-7f) {
                range = 1e-7f;
            }

            Value a = paramSeries[lower].value;
            Value b = paramSeries[lower + 1].value;

            return interpolator.Evaluate(a, b, (param - lowerT) / range);
        }
    }
}

