using UnityEngine;
using static UnityEngine.Mathf;

public static class FunctionLibrary
{
	public delegate Vector3 Function(float u, float v, float time);

	public enum FunctionName{Wave, MultiWave, Ripple, Sphere, Torus};

	static Function[] functions = { Wave, MultiWave, Ripple, Sphere, Torus };

	public static int FunctionCount => functions.Length;

	public static Function GetFunction(FunctionName name)
	{
		return functions[(int)name];
	}

	public static FunctionName GetNextFunctionName(FunctionName name)
	{
		return (FunctionName)((int)(name + 1) % functions.Length);
	}

	public static FunctionName GetRandomFunctionNameOtherThan(FunctionName name)
	{
		FunctionName choice = (FunctionName)Random.Range(1, functions.Length);
		return choice == name ? 0 : choice;
	}

	public static Vector3 Morph(float u, float v, float time, Function from, Function to, float progress)
	{
		return Vector3.LerpUnclamped(from(u, v, time), to(u, v, time), SmoothStep(0f, 1f, progress));
	}

	public static Vector3 Wave(float u, float v, float time)
	{
		Vector3 point;
		point.x = u;
		point.z = v;
		point.y = Sin(PI * (u + v + time));

		return point;
	}

	public static Vector3 MultiWave(float u, float v, float time)
	{
		Vector3 point;
		point.x = u;
		point.z = v;

		point.y = Sin(PI * (u + time * 0.5f));
		point.y += Sin(2f * PI * (v + time)) * 0.5f;
		point.y += Sin(PI * (u + v + time * 0.25f));
		point.y *= 1f / 2.5f;

		return point;
	}

	public static Vector3 Ripple(float u, float v, float time)
	{
		Vector3 point;
		point.x = u;
		point.z = v;

		float distance = Sqrt(u * u + v * v);
		point.y = Sin(PI * (4f * distance - time));
		point.y /= 1f + 10f * distance;

		return point;
	}

	public static Vector3 Sphere(float u, float v, float time)
	{
		float radius = 0.9f + 0.1f * Sin(PI * (12f * u + 8f * v + time));
		float scale = radius * Cos(0.5f * PI * v);

		Vector3 point;
		point.x = scale * Sin(PI * u);
		point.y = radius * Sin(PI * 0.5f * v);
		point.z = scale * Cos(PI * u);

		return point;
	}

	public static Vector3 Torus(float u, float v, float time)
	{
		float majorRadius = 0.7f + 0.1f * Sin(PI * (8f * u + 0.5f * time));
		float minorRadius = 0.15f + 0.05f * Sin(PI * (16f * u + 8f * v + 3f * time));
		float scale = majorRadius + minorRadius * Cos(PI * v);

		Vector3 point;
		point.x = scale * Sin(PI * u);
		point.y = minorRadius * Sin(PI * v);
		point.z = scale * Cos(PI * u);

		return point;
	}
}
