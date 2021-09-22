using UnityEngine;
using UnityEngine.Rendering;
using float2 = UnityEngine.Vector2;
using float3 = UnityEngine.Vector3;
using float4 = UnityEngine.Vector4;

static class Extensions
{
#pragma warning disable IDE1006

  // Vector2 to Vector2
  public static float2 xx(this float2 v)
  {
    return new float2(v.x, v.x);
  }

  public static float2 yx(this float2 v)
  {
    return new float2(v.y, v.x);
  }

  // Vector2 to Vector3
  public static float3 xyx(this float2 v)
  {
    return new float3(v.x, v.y, v.x);
  }

  // Vector3 to Vector2
  public static float2 xy(this float3 v)
  {
    return new float2(v.x, v.y);
  }

  public static float2 xz(this float3 v)
  {
    return new float2(v.x, v.z);
  }

  // Vector3 to Vector3
  public static float3 zxy(this float3 v)
  {
    return new float3(v.z, v.x, v.y);
  }

  public static float3 yxz(this float3 v)
  {
    return new float3(v.y, v.x, v.z);
  }

  public static float3 xyz(this Quaternion q)
  {
    return new float3(q.x, q.y, q.z);
  }

  public static float4 xyzw(this Quaternion q)
  {
    return new float4(q.x, q.y, q.z, q.w);
  }

#pragma warning restore IDE100

  public static float3 GetReciprical(this float3 v)
  {
    return new float3(1.0f / v.x, 1.0f / v.y, 1.0f / v.z);
  }

  public static float3 Clamp(this float3 v, float min, float max)
  {
    return new float3(Mathf.Clamp(v.x, min, max), Mathf.Clamp(v.y, min, max), Mathf.Clamp(v.z, min, max));
  }

  public static float3 GetTangent(this float3 v)
  {
    float3 tangentUp = float3.Cross(float3.up, v);
    float3 tangentForward = float3.Cross(float3.forward, v);

    if (tangentForward.sqrMagnitude > tangentUp.sqrMagnitude)
    {
      return tangentForward;
    }
    else
    {
      return tangentUp;
    }
  }

  public static Vector3 GetPosition(this Matrix4x4 m)
  {
    return m.GetColumn(3);
  }

  public static Vector3 GetScale(this Matrix4x4 m)
  {
    return new Vector3(
      m.GetColumn(0).magnitude,
      m.GetColumn(1).magnitude,
      m.GetColumn(2).magnitude);
  }

  public static Quaternion GetRotation(this Matrix4x4 m)
  {
    return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
  }

  public static Vector4 WorldToCameraPlane(this Camera c, Vector4 plane)
  {
    return Matrix4x4.Transpose(Matrix4x4.Inverse(c.worldToCameraMatrix)) * plane;
  }

  public static ShadowResolution GetShadowResolution(this Light l)
  {
    switch (l.shadowResolution)
    {
      case LightShadowResolution.FromQualitySettings:
        return QualitySettings.shadowResolution;
      case LightShadowResolution.VeryHigh:
        return ShadowResolution.VeryHigh;
      case LightShadowResolution.High:
        return ShadowResolution.High;
      case LightShadowResolution.Medium:
        return ShadowResolution.Medium;
      default:
        return ShadowResolution.Low;
    }
  }
}
