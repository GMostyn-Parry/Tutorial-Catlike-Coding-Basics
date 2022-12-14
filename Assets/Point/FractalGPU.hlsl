#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
StructuredBuffer<float3x4> _Matrices;
#endif

void ConfigureProcedural()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	float3x4 surfaceMarix = _Matrices[unity_InstanceID];
	unity_ObjectToWorld._m00_m01_m02_m03 = surfaceMarix._m00_m01_m02_m03;
	unity_ObjectToWorld._m10_m11_m12_m13 = surfaceMarix._m10_m11_m12_m13;
	unity_ObjectToWorld._m20_m21_m22_m23 = surfaceMarix._m20_m21_m22_m23;
	unity_ObjectToWorld._m30_m31_m32_m33 = float4(0.0, 0.0, 0.0, 1.0);
#endif
}

float4 _ColourA, _ColourB;

float4 _SequenceNumbers;

float4 GetFractalColour()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	float4 colour;
	colour.rgb = lerp(_ColourA.rgb, _ColourB.rgb, frac(unity_InstanceID * _SequenceNumbers.x + _SequenceNumbers.y));
	colour.a = lerp(_ColourA.a, _ColourB.a, frac(unity_InstanceID * _SequenceNumbers.z + _SequenceNumbers.w));
	return colour;
#else
	return _ColourA;
#endif
}

void ShaderGraphFunction_float(float3 In, out float3 Out, out float4 FractalColour)
{
	Out = In;
	FractalColour = GetFractalColour();
}

void ShaderGraphFunction_half(half3 In, out half3 Out, out half4 FractalColour)
{
	Out = In;
	FractalColour = GetFractalColour();
}
