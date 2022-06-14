using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class TimeCounter
{
	Stopwatch stopwatch = new Stopwatch();
	float[] measurements;

	public float mean { get; private set; }

	public TimeCounter(int samplesCount = 100) { mean = 0.0f; measurements = new float[samplesCount]; }

	public void Start()
	{
		stopwatch.Restart();
	}

	public void Stop(bool updateMean = true)
	{
		stopwatch.Stop();
		measurements[Time.frameCount % measurements.Length] = 1000.0f * stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
		
		if (updateMean)
		{
			mean = 0;
			foreach (var item in measurements)
				mean += item;
			mean /= measurements.Length;
		}
	}
}