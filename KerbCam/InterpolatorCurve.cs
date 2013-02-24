using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbCam {
    public class InterpolatorCurve<T> {
        public class Frame {
            private float tInternal;
            private T valueInternal;

            public Frame(float t, T value) {
                this.tInternal = t;
                this.valueInternal = value;
            }

            public float t {
                get { return tInternal; }
            }

            public T value {
                get { return valueInternal; }
            }

            public override string ToString() {
                return string.Format("Frame({0}, {1})", tInternal, valueInternal);
            }
        }

        public interface IValueInterpolator {
            /**
             * Interpolates between two values, t is scaled 
             * <param name="a">The value for t=0.</param>
             * <param name="b">The value for t=1.</param>
             * <param name="t">The interpolation parameter, this varies
             * between 0 and 1.</param>
             */
            T Evaluate(T a, T b, float t);
        }

        // frames is maintained sorted on Frame.time.
        private List<Frame> frames;

        private IValueInterpolator interpolator;

        public InterpolatorCurve(IValueInterpolator interpolator) {
            frames = new List<Frame>();
            this.interpolator = interpolator;
        }

        public Frame this[int index] {
            get { return frames[index]; }
        }

        public int NumKeys {
            get { return frames.Count; }
        }

        public float MinTime {
            get {
                if (frames.Count == 0)
                    return 0f;
                else
                    return frames[0].t;
            }
        }

        public float MaxTime {
            get {
                if (frames.Count == 0)
                    return 0f;
                else
                    return frames[frames.Count - 1].t;
            }
        }

        public T Evaluate(float t) {
            if (frames.Count == 0)
                return default(T);

            int lower = FindLowerIndex(t);
            if (lower < 0)
                return frames[0].value;
            if (lower >= frames.Count - 1)
                return frames[frames.Count - 1].value;

            float lowerT = frames[lower].t;
            float upperT = frames[lower + 1].t;
            float range = upperT - lowerT;

            // Avoid nasty divide-by-zero math for keys that are very close together in time.
            if (range < 1e-7f) {
                range = 1e-7f;
            }

            T a = frames[lower].value;
            T b = frames[lower + 1].value;

            return interpolator.Evaluate(a, b, (t - lowerT) / range);
        }

        public int AddKey(float time, T value) {
            Frame frame = new Frame(time, value);
            if (frames.Count == 0) {
                frames.Add(frame);
                return frames.Count - 1;
            } else {
                int index = FindInsertIndex(time);
                frames.Insert(index, frame);
                return index;
            }

        }

        private int FindInsertIndex(float time) {
            var v = new SortedList<float, T>();

            if (frames.Count == 0) {
                return 0;
            }
            int lowerIndex = FindLowerIndex(time);
            return lowerIndex + 1;
        }

        // FindIndex returns the highest index such that values[index].time <= time.
        // Returns -1 if time < values[0].time.
        public int FindLowerIndex(float time) {
            return BinSearchFindLowerIndex(time, 0, frames.Count - 1);
        }

        private int BinSearchFindLowerIndex(float time, int lower, int upper) {
            float upperTime = frames[upper].t;
            if (time >= upperTime)
                return upper;

            float lowerTime = frames[lower].t;
            if (time < lowerTime)
                return lower - 1;
            else if (upper - lower == 1)
                return lower;

            int midIndex = lower + (upper - lower) / 2;
            float midTime = frames[midIndex].t;
            if (time == midTime) {
                return midIndex;
            } else if (time < midTime) {
                return BinSearchFindLowerIndex(time, lower, midIndex);
            } else /* (time > midTime) */ {
                return BinSearchFindLowerIndex(time, midIndex, upper);
            }
        }

        public void RemoveAt(int index) {
            frames.RemoveAt(index);
        }

        /// <summary>
        /// Moves the key at the given index to the new time.
        /// </summary>
        /// <param name="index">Index of the key to move.</param>
        /// <param name="t">Time.</param>
        /// <returns>The new index.</returns>
        public int MoveKeyAt(int index, float t) {
            T value = frames[index].value;
            frames.RemoveAt(index);
            return AddKey(t, value);
        }
    }
}

