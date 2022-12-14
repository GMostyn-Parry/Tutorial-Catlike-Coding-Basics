using UnityEngine;

public class Graph : MonoBehaviour
{
	public enum TransitionMode { Cycle, Random }

	[SerializeField]
	Transform pointPrefab = default;

	[SerializeField, Range(10, 200)]
	int resolution = 10;

	[SerializeField]
	FunctionLibrary.FunctionName function = default;

	[SerializeField]
	TransitionMode transitionMode = TransitionMode.Cycle;

	[SerializeField, Min(0f)]
	float functionDuration = 1f, transitionDuration = 1f;

	Transform[] points;

	float duration;

	bool transitioning;
	FunctionLibrary.FunctionName transitionFunction;

	private void Awake()
	{
		float step = 2f / resolution;
		Vector3 scale = Vector3.one * step;

		points = new Transform[resolution * resolution];
		for(int i = 0; i < points.Length; i++)
		{
			Transform point = Instantiate(pointPrefab);
			point.localScale = scale;
			point.SetParent(transform, false);

			points[i] = point;
		}
	}

	private void Update()
	{
		duration += Time.deltaTime;
		
		if(transitioning)
		{
			if(duration >= transitionDuration)
			{
				duration -= transitionDuration;
				transitioning = false;
			}
		}
		else if(duration >= functionDuration)
		{
			duration -= functionDuration;

			transitioning = true;
			transitionFunction = function;
			PickNextFunction();
		}

		if(transitioning) UpdateFunctionTransition();
		else UpdateFunction();
	}

	private void PickNextFunction()
	{
		function = transitionMode == TransitionMode.Cycle ? FunctionLibrary.GetNextFunctionName(function) : FunctionLibrary.GetRandomFunctionNameOtherThan(function);
	}

	private void UpdateFunction()
	{
		FunctionLibrary.Function functionDelegate = FunctionLibrary.GetFunction(function);
		float step = 2f / resolution;
		float time = Time.time;

		float v = 0.5f * step - 1f;
		for(int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
		{
			if(x == resolution)
			{
				x = 0;
				z++;
				v = (z + 0.5f) * step - 1f;
			}

			float u = (x + 0.5f) * step - 1f;
			points[i].localPosition = functionDelegate(u, v, time);
		}
	}

	private void UpdateFunctionTransition()
	{
		FunctionLibrary.Function fromDelegate = FunctionLibrary.GetFunction(transitionFunction);
		FunctionLibrary.Function toDelegate = FunctionLibrary.GetFunction(function);
		float progress = duration / transitionDuration;
		float step = 2f / resolution;
		float time = Time.time;

		float v = 0.5f * step - 1f;
		for(int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
		{
			if(x == resolution)
			{
				x = 0;
				z++;
				v = (z + 0.5f) * step - 1f;
			}

			float u = (x + 0.5f) * step - 1f;
			points[i].localPosition = FunctionLibrary.Morph(u, v, time, fromDelegate, toDelegate, progress);
		}
	}
}
