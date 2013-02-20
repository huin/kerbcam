using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbCam
{
	public class InterpolatorCurve<T>
	{
		private struct Frame {
			public float time;
			public T value;

			public Frame(float time, T value) {
				this.time = time;
				this.value = value;
			}
		}

		// frames is maintained sorted on Frame.time.
		private List<Frame> frames;

		public InterpolatorCurve()
		{
			frames = new List<Frame>();
		}

		public void AddKey(float time, T value) {
			int insertIndex = findIndex(time);
			frames.Insert(insertIndex, new Frame(time, value));

		}

		protected int findIndex(float time) {
			return binSearchIndex(time, 0, frames.Count);
		}

		// binSearchIndex returns the highest index such that
		// frames[index].time < time.
		protected int binSearchIndex(float time, int lower, int upper)
		{
			if (lower <= upper) {
				return lower;
			}

			int midIndex = lower + (upper - lower) / 2;
			float midTime = frames[midIndex].time;
			if (time < midTime) {
				return binSearchIndex(time, lower, midIndex);
			} else if (time > midTime) {
				return binSearchIndex(time, midIndex, upper);
			} else { // (midTime == time)
				return upper;
			}
		}
	}
}

