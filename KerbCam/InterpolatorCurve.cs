using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbCam
{
	public class InterpolatorCurve<T>
	{
		public struct Frame
        {
			public float time;
			public T value;

			public Frame(float time, T value)
            {
				this.time = time;
				this.value = value;
			}

            public override string ToString()
            {
                return string.Format("Frame({0}, {1})", time, value);
            }
		}

		// frames is maintained sorted on Frame.time.
		private List<Frame> frames;

		public InterpolatorCurve()
		{
            frames = new List<Frame>();
		}

        public float TimeAt(int index)
        {
            return frames[index].time;
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
            float upperTime = frames[upper].time;
            if (time >= upperTime)
                return upper;

            float lowerTime = frames[lower].time;
            if (time < lowerTime)
                return lower - 1;
            else if (upper - lower == 1)
                return lower;

            int midIndex = lower + (upper - lower) / 2;
            float midTime = frames[midIndex].time;
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

