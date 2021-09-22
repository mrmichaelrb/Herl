#ifndef COMMON_HLSL
#define COMMON_HLSL

#define TIME_SECONDS (_Time.y)

#define GPU_THREAD_GROUP_SIZE (32)

static const float Pi = 3.1415927;
static const float TwoPi = 2.0 * Pi;
static const float PiUnderOne = 1.0 / Pi;
static const float TwoPiUnderOne = 1.0 / TwoPi;

static const float Sqrt2 = 1.4142136;
static const float Sqrt3 = 1.7320508;
static const float SqrtOneThird = 0.5773503;

static const float EighthArcCosine = 0.707107;

static const float AlmostFull = 0.999;
static const float AlmostHalf = AlmostFull * 0.5;

static const half4 NoColor = half4(0.0, 0.0, 0.0, 0.0);

static const float3 Origin = float3(0.0, 0.0, 0.0);
static const float3 Up = float3(0.0, 1.0, 0.0);
static const float3 Down = -Up;
static const float3 Forward = float3(0.0, 0.0, 1.0);
static const float3 Back = -Forward;
static const float3 Right = float3(1.0, 0.0, 0.0);
static const float3 Left = -Right;

static const float NoBias = 0.0;
static const float3 NoScale = float3(1.0, 1.0, 1.0);
static const float2 UvNoOffset = float2(0.0, 0.0);
static const float2 UvNoScale = float2(1.0, 1.0);
static const float2 UvBackfaceMirror = float2(-1.0, 1.0);

static const float AlmostZeroVelocity = 1.0;

static const float4x4 Rotate3DXAxis180 = float4x4(1.0, 0.0, 0.0, 0.0, 0.0, -1.0, 0.0, 0.0, 0.0, 0.0, -1.0, 0.0, 0.0, 0.0, 0.0, 1.0);
static const float4x4 Rotate3DYAxis180 = float4x4(-1.0, 0.0, 0.0, 0.0, 0.0, 1, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0);

uniform float3 WorldPosition;

uniform float HashOffset;
  // This hash stays constant when lerping between worlds
uniform float HashOffsetStatic;

uniform float NoiseTextureSize;
uniform float NoiseTexelSize;
uniform float NoiseTextureScale;
uniform float2 NoiseTextureOffset;
uniform Texture2D NoiseTexture;

// Named with underscores so Unity will recognize and initialze samplers
SamplerState Noise_point_repeat_sampler;
SamplerState Noise_linear_repeat_sampler;

float NanToZero(float v)
{
  return isnan(v) ? 0.0 : v;
}

float Modulus(float x, float y)
{
  return (floor(x / y) * -y) + x;
}

float2 Modulus(float2 x, float y)
{
  return (floor(x / y) * -y) + x;
}

float LengthSquared(float a)
{
  return a * a;
}

float LengthSquared(float2 a)
{
  return dot(a, a);
}

float LengthSquared(float3 a)
{
  return dot(a, a);
}

float DistanceSquared(float2 a, float2 b)
{
  float2 distance = a - b;
  return LengthSquared(distance);
}

float DistanceSquared(float3 a, float3 b)
{
  float3 distance = a - b;
  return LengthSquared(distance);
}

float SquareStep(float2 direction)
{
  float2 squareStep = abs(direction);
  float squareStepLeg = max(squareStep.x, squareStep.y);
  squareStep /= squareStepLeg;

  return length(squareStep);
}

int ArrayIndex(int3 i, int2 size)
{
  return (i.y * size.x) + i.x;
}

int ArrayIndex(int3 i, int3 size)
{
  return (i.z * size.x * size.y) + (i.y * size.x) + i.x;
}

float AsinFast(float f)
{
  return (((0.6981317f * f * f) + 0.8726646f) * f);
}

float3 PrimaryAxis(float3 v)
{
  float3 magnitude = abs(v);
  
  bool xPrimary = ((magnitude.x > magnitude.y) && (magnitude.x > magnitude.z));
  bool yPrimary = ((magnitude.y > magnitude.x) && (magnitude.y > magnitude.z));
  bool zPrimary = !(xPrimary || yPrimary);
  
  float3 axis;
  axis.x = xPrimary * sign(v.x);
  axis.y = yPrimary * sign(v.y);
  axis.z = zPrimary * sign(v.z);
  
  return axis;
}

float3 Tangent(float3 v)
{
  float3 tangentUp = cross(Up, v);
  float3 tangentForward = cross(Forward, v);

  if (LengthSquared(tangentForward) > LengthSquared(tangentUp))
  {
    return tangentForward;
  }
  else
  {
    return tangentUp;
  }
}

float PowerCurve(float x, float a, float b)
{
  float k = pow(a + b, a + b) / (pow(a, a) * pow(b, b));
  return k * pow(x, a) * pow(1.0 - x, b);
}

float4x4 GetTranslationMatrix(float3 translation)
{
  return float4x4(
    1.0, 0.0, 0.0, translation.x,
    0.0, 1.0, 0.0, translation.y,
    0.0, 0.0, 1.0, translation.z,
    0.0, 0.0, 0.0, 1.0);
}

float4x4 GetScaleMatrix(float3 scale)
{
  return float4x4(
    scale.x, 0.0, 0.0, 0.0,
    0.0, scale.y, 0.0, 0.0,
    0.0, 0.0, scale.z, 0.0,
    0.0, 0.0, 0.0, 1.0);
}

float4x4 GetRotationMatrixForwardToXZ(float x, float z)
{
  float angle = atan2(x, z);
  float sineAngle;
  float cosineAngle;
  sincos(angle, sineAngle, cosineAngle);

  return float4x4(
    cosineAngle, 0.0, sineAngle, 0.0,
    0.0, 1.0, 0.0, 0.0,
    -sineAngle, 0.0, cosineAngle, 0.0,
    0.0, 0.0, 0.0, 1.0);
}

float4 InvertQuaternion(float4 q)
{
  return float4(-q.xyz, q.w);
}

float3 RotateVector(float3 v, float4 q)
{
  return (cross((v * q.w) + cross(v, q.xyz), q.xyz) * 2.0) + v;
}

float4 SampleTexture(sampler2D s, float2 uv, float lod)
{
  float4 loduv = float4(uv.x, uv.y, 0.0, lod);
  return tex2Dlod(s, loduv);
}

float4 SampleTexture(sampler2D s, float2 uv)
{
  return SampleTexture(s, uv, 0.0);
}

/*
Following random and hash function modified from:
Hash without Sine
MIT License
Copyright (c) 2014 David Hoskins


Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
float Float3Rand(float3 seed)
{
  seed = frac((seed * 0.13591409) + 0.70710678);
  seed += dot(seed, seed.yzx + 31.415926);
  return frac((seed.x + seed.y) * seed.z);
}

float Float2Rand(float2 seed)
{
  return Float3Rand(seed.xyx);
}

float Float2Hash(float2 seed, float offset)
{
  return Float3Rand(float3(seed.xy, offset));
}

float Float2NoiseTexture(float2 seed)
{
  seed *= NoiseTextureScale;

  return NoiseTexture.SampleLevel(Noise_linear_repeat_sampler, seed, 0.0).r;
}

// Bilinear sampling done in software because hardware version is usually
// computed with lower precision
float Float2NoiseTextureHiRes(float2 seed)
{
  seed *= NoiseTextureScale;

  float2 f = frac(seed * NoiseTextureSize);
  float2 centering = (0.5 - f) * NoiseTexelSize;
  float2 uv = seed + centering;

  float4 s = NoiseTexture.Gather(Noise_point_repeat_sampler, uv);
  
  float top = lerp(s.w, s.z, f.x);
  float bottom = lerp(s.x, s.y, f.x);

  return lerp(top, bottom, f.y);
}

float Float3NoiseTexture(float3 seed)
{
  seed *= NoiseTextureScale;

  seed.z = frac(seed.z) * NoiseTextureSize;

  float p = floor(seed.z);
  float f = frac(seed.z);

  float2 aOffset = NoiseTextureOffset * p;
  float2 bOffset = aOffset + NoiseTextureOffset;

  float s1 = NoiseTexture.SampleLevel(Noise_linear_repeat_sampler, seed.xy + aOffset, 0.0).r;
  float s2 = NoiseTexture.SampleLevel(Noise_linear_repeat_sampler, seed.xy + bOffset, 0.0).r;

  return lerp(s1, s2, f);
}

float DistanceToHorizontalPlane(float3 rayStart, float3 rayDirection, float planeAltitude)
{
  return (planeAltitude - rayStart.y) / rayDirection.y;
}

float2 IntersectionOnHorizontalPlane(float3 rayStart, float3 rayDirection, float planeAltitude)
{
  float distance = DistanceToHorizontalPlane(rayStart, rayDirection, planeAltitude);
  float2 location = (rayDirection.xz * distance) + rayStart.xz;

  return location;
}

float DistanceToZOriginPlane(float3 rayStart, float3 rayDirection)
{
  return -rayStart.z / rayDirection.z;
}

float2 IntersectionOnZOriginPlane(float3 rayStart, float3 rayDirection, float distance)
{
  float2 location = (rayDirection.xy * distance) + rayStart.xy;
  return location;
}

float2 IntersectionOnZOriginPlane(float3 rayStart, float3 rayDirection)
{
  float distance = DistanceToZOriginPlane(rayStart, rayDirection);
  return IntersectionOnZOriginPlane(rayStart, rayDirection, distance);
}

bool IsOnStrip(float step, float stripWidth, float spaceWidth)
{
  return Modulus(step, (stripWidth + spaceWidth)) < spaceWidth;
}

#define DistanceSurfaceNormal(distanceFunction, position, offset) ( \
  normalize( \
    float3( \
      distanceFunction(position + float3(offset, 0.0, 0.0)) - \
        distanceFunction(position - float3(offset, 0.0, 0.0)), \
      distanceFunction(position + float3(0.0, offset, 0.0)) - \
        distanceFunction(position - float3(0.0, offset, 0.0)), \
      distanceFunction(position + float3(0.0, 0.0, offset)) - \
        distanceFunction(position - float3(0.0, 0.0, offset)) \
      ) \
    ) \
  )

#endif
