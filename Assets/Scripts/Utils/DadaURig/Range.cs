using System;
using UnityEngine;

namespace Dada.URig
{
	[Serializable]
	public class Range
	{
		public float minimum;
		public float maximum;

		public Range()
		{
			minimum = float.NegativeInfinity;
			maximum = float.PositiveInfinity;
		}

		public Range(float value)
		{
			minimum = value;
			maximum = value;
		}

		public Range(float min, float max)
		{
			minimum = min;
			maximum = max;
		}

		public bool IsLocked()
		{
			return Mathf.Approximately(minimum, maximum);
		}

		public float Clamp(float value)
		{
			return Mathf.Clamp(value, minimum, maximum);
		}
	}
}
