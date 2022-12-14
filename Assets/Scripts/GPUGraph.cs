using UnityEngine;

public class GPUGraph : MonoBehaviour
{
	public enum TransitionMode { Cycle, Random }

	static readonly int positionsID = Shader.PropertyToID("_Positions");
	static readonly int resolutionID = Shader.PropertyToID("_Resolution");
	static readonly int stepID = Shader.PropertyToID("_Step");
	static readonly int timeID = Shader.PropertyToID("_Time");
	static readonly int transitionProgressID = Shader.PropertyToID("_TransitionProgress");

	const int maxResolution = 1000;

	[SerializeField]
	ComputeShader computeShader = default;

	[SerializeField]
	Material material = default;

	[SerializeField]
	Mesh mesh = default;

	[SerializeField, Range(10, maxResolution)]
	int resolution = 10;

	[SerializeField]
	FunctionLibrary.FunctionName function = default;

	[SerializeField]
	TransitionMode transitionMode = TransitionMode.Cycle;

	[SerializeField, Min(0f)]
	float functionDuration = 1f, transitionDuration = 1f;

	float duration;

	bool transitioning;
	FunctionLibrary.FunctionName transitionFunction;

	ComputeBuffer positionsBuffer;

	private void OnEnable()
	{
		positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4);
	}

	private void OnDisable()
	{
		positionsBuffer.Release();
		positionsBuffer = null;
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

		UpdateFunctionOnGPU();
	}

	private void PickNextFunction()
	{
		function = transitionMode == TransitionMode.Cycle ? FunctionLibrary.GetNextFunctionName(function) : FunctionLibrary.GetRandomFunctionNameOtherThan(function);
	}

	private void UpdateFunctionOnGPU()
	{
		float step = 2f / resolution;
		computeShader.SetInt(resolutionID, resolution);
		computeShader.SetFloat(stepID, step);
		computeShader.SetFloat(timeID, Time.time);

		if(transitioning)
		{
			computeShader.SetFloat(transitionProgressID, Mathf.SmoothStep(0f, 1f, duration / transitionDuration));
		}

		int kernelIndex = (int)function + (int)(transitioning ? transitionFunction : function) * FunctionLibrary.FunctionCount;
		computeShader.SetBuffer(kernelIndex, positionsID, positionsBuffer);

		int groups = Mathf.CeilToInt(resolution / 8f);
		computeShader.Dispatch(kernelIndex, groups, groups, 1);

		material.SetBuffer(positionsID, positionsBuffer);
		material.SetFloat(stepID, step);

		Bounds bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
		Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);
	}
}
