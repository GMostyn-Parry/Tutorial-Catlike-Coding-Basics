using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

public class Fractal : MonoBehaviour
{
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	struct UpdateFractalLevelJob : IJobFor
	{
		public float scale;
		public float deltaTime;

		[ReadOnly]
		public NativeArray<FractalPart> parents;
		
		public NativeArray<FractalPart> parts;

		[WriteOnly]
		public NativeArray<float3x4> matrices;

		public void Execute(int i)
		{
			FractalPart parent = parents[i / 5];
			FractalPart part = parts[i];
			part.spinAngle += part.spinVelocity * deltaTime;

			float3 upAxis = mul(mul(parent.worldRotation, part.rotation), up());
			float3 sagAxis = cross(up(), upAxis);

			float sagMagnitude = length(sagAxis);
			quaternion baseRotation;
			if(sagMagnitude > 0f)
			{
				sagAxis /= sagMagnitude;
				quaternion sagRotation = quaternion.AxisAngle(sagAxis, part.maxSagAngle * sagMagnitude);
				baseRotation = mul(sagRotation, parent.worldRotation);
			}
			else
			{
				baseRotation = parent.worldRotation;
			}

			part.worldRotation = mul(baseRotation, mul(part.rotation, quaternion.RotateY(part.spinAngle)));
			part.worldPosition = parent.worldPosition + mul(part.worldRotation, float3(0f, scale * 1.5f, 0f));
			parts[i] = part;
			
			float3x3 rotation = float3x3(part.worldRotation) * scale;
			matrices[i] = float3x4(rotation.c0, rotation.c1, rotation.c2, part.worldPosition);
		}
	}

	private struct FractalPart
	{
		public float3 worldPosition;
		public quaternion rotation, worldRotation;
		public float maxSagAngle, spinAngle, spinVelocity;
	}

	[SerializeField, Range(3, 8)]
	int depth = 4;

	[SerializeField]
	Mesh mesh = default, leafMesh = default;

	[SerializeField]
	Material material = default;

	[SerializeField]
	Gradient gradientA = default, gradientB = default;

	[SerializeField]
	Color leafColourA = default, leafColourB = default;

	[SerializeField, Range(0f, 90f)]
	float maxSagAngleA = 15f, maxSagAngleB = 25f;

	[SerializeField, Range(0f, 90f)]
	float spinSpeedA = 20f, spinSpeedB = 25f;

	[SerializeField, Range(0f, 1f)]
	float reverseSpinChance = 0.25f;

	static private quaternion[] rotations =
	{
		quaternion.identity,
		quaternion.RotateZ(-PI * 0.5f), quaternion.RotateZ(PI * 0.5f),
		quaternion.RotateX(PI * 0.5f), quaternion.RotateX(-PI * 0.5f)
	};

	static private readonly int colourAID = Shader.PropertyToID("_ColourA"), colourBID = Shader.PropertyToID("_ColourB"), matricesID = Shader.PropertyToID("_Matrices"), sequenceNumbersID = Shader.PropertyToID("_SequenceNumbers");

	static private MaterialPropertyBlock propertyBlock;

	private NativeArray<FractalPart>[] parts;

	private NativeArray<float3x4>[] matrices;

	private ComputeBuffer[] matricesBuffers;

	private Vector4[] sequenceNumbers;

	private void OnEnable()
	{
		parts = new NativeArray<FractalPart>[depth];
		matrices = new NativeArray<float3x4>[depth];
		matricesBuffers = new ComputeBuffer[depth];
		sequenceNumbers = new Vector4[depth];

		int stride = 12 * 4;
		for(int i = 0, length = 1; i < parts.Length; i++, length *= 5)
		{
			parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
			matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
			matricesBuffers[i] = new ComputeBuffer(length, stride);
			sequenceNumbers[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
		}

		parts[0][0] = CreatePart(0);
		for(int levelIndex = 1; levelIndex < parts.Length; levelIndex++)
		{
			NativeArray<FractalPart> levelParts = parts[levelIndex];
			for(int fractalPartIndex = 0; fractalPartIndex < levelParts.Length; fractalPartIndex += 5)
			{
				for(int childIndex = 0; childIndex < 5; childIndex++)
				{
					levelParts[fractalPartIndex + childIndex] = CreatePart(childIndex);
				}
			}
		}

		if(propertyBlock == null)
		{
			propertyBlock = new MaterialPropertyBlock();
		}
	}

	private void OnDisable()
	{
		for(int i = 0; i < matricesBuffers.Length; i++)
		{
			matricesBuffers[i].Release();
			parts[i].Dispose();
			matrices[i].Dispose();
		}

		parts = null;
		matrices = null;
		matricesBuffers = null;
		sequenceNumbers = null;
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;

		FractalPart rootPart = parts[0][0];
		rootPart.spinAngle += rootPart.spinVelocity * deltaTime;
		rootPart.worldRotation = mul(transform.rotation, mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle)));
		rootPart.worldPosition = transform.position;
		parts[0][0] = rootPart;

		float objectScale = transform.lossyScale.x;
		float3x3 rotation = float3x3(rootPart.worldRotation) * objectScale;
		matrices[0][0] = float3x4(rotation.c0, rotation.c1, rotation.c2, rootPart.worldPosition);

		float scale = objectScale;
		JobHandle jobHandle = default;
		for(int levelIndex = 1; levelIndex < parts.Length; levelIndex++)
		{
			scale *= 0.5f;

			jobHandle = new UpdateFractalLevelJob
			{
				scale = scale,
				deltaTime = deltaTime,
				parents = parts[levelIndex - 1],
				parts = parts[levelIndex],
				matrices = matrices[levelIndex]
			}.ScheduleParallel(parts[levelIndex].Length, 5, jobHandle);
		}
		jobHandle.Complete();

		Bounds bounds = new Bounds(rootPart.worldPosition, Vector3.one * objectScale * 3f);
		int leafIndex = matricesBuffers.Length - 1;
		for(int i = 0; i < matricesBuffers.Length; i++)
		{
			ComputeBuffer buffer = matricesBuffers[i];
			buffer.SetData(matrices[i]);

			Color colourA, colourB;
			Mesh instanceMesh;
			if(i == leafIndex)
			{
				colourA = leafColourA;
				colourB = leafColourB;
				instanceMesh = leafMesh;
			}
			else
			{
				float gradientInterpolator = i / (matricesBuffers.Length - 2f);
				colourA = gradientA.Evaluate(gradientInterpolator);
				colourB = gradientB.Evaluate(gradientInterpolator);
				instanceMesh = mesh;
			}
			propertyBlock.SetColor(colourAID, colourA);
			propertyBlock.SetColor(colourBID, colourB);
			propertyBlock.SetBuffer(matricesID, buffer);
			propertyBlock.SetVector(sequenceNumbersID, sequenceNumbers[i]);

			Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, bounds, buffer.count, propertyBlock);
		}
	}

	private void OnValidate()
	{
		if(parts != null && enabled)
		{
			OnDisable();
			OnEnable();
		}
	}

	private FractalPart CreatePart(int childIndex)
	{
		return new FractalPart
		{
			maxSagAngle = radians(Random.Range(maxSagAngleA, maxSagAngleB)),
			rotation = rotations[childIndex],
			spinVelocity = radians(Random.Range(spinSpeedA, spinSpeedB)) * (Random.value < reverseSpinChance ? -1f : 1f)
		};
	}
}
