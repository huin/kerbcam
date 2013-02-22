using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbCam
{
	public class InterpolatorCurve<T>
	{
		private struct Frame
        {
			public float t;
			public T value;

			public Frame(float time, T value)
            {
				this.t = time;
				this.value = value;
			}

            public override string ToString()
            {
                return string.Format("Frame({0}, {1})", t, value);
            }
		}

        public interface IValueInterpolator
        {
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

		public InterpolatorCurve(IValueInterpolator interpolator)
		{
            frames = new List<Frame>();
            this.interpolator = interpolator;
		}

        public T Evaluate(float t)
        {
            if (frames.Count == 0)
                return default(T);

            int lower = FindLowerIndex(t);
            if (lower < 0)
                return frames[0].value;
            if (lower >= frames.Count-1)
                return frames[frames.Count-1].value;

            float lowerT = frames[lower].t;
            float upperT = frames[lower + 1].t;
            float range = upperT - lowerT;
            T a = frames[lower].value;
            T b = frames[lower + 1].value;

            return interpolator.Evaluate(a, b, range*(t - lowerT));
        }

        public float TimeAt(int index)
        {
            return frames[index].t;
        }

		public void AddKey(float time, T value)
        {
            Frame frame = new Frame(time, value);
            if (frames.Count == 0)
            {
                frames.Add(frame);
            }
            else
            {
                frames.Insert(FindInsertIndex(time), frame);
            }

		}

        private int FindInsertIndex(float time)
        {
            var v = new SortedList<float, T>();

            if (frames.Count == 0)
            {
                return 0;
            }
            int lowerIndex = FindLowerIndex(time);
            return lowerIndex + 1;
        }

        // FindIndex returns the highest index such that values[index].time <= time.
        // Returns -1 if time < values[0].time.
        public int FindLowerIndex(float time)
        {
            return BinSearchFindLowerIndex(time, 0, frames.Count - 1);
        }

        private int BinSearchFindLowerIndex(float time, int lower, int upper)
		{
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
            if (time == midTime)
            {
                return midIndex;
            }
            else if (time < midTime)
            {
                return BinSearchFindLowerIndex(time, lower, midIndex);
            }
            else // (time > midTime)
            {
                return BinSearchFindLowerIndex(time, midIndex, upper);
            }
		}
	}
}

