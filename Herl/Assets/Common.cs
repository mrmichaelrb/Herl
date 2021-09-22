using UnityEngine;
using float2 = UnityEngine.Vector2;
using float3 = UnityEngine.Vector3;
using float2x2 = Matrix2x2;
using Unity.Collections;

static class Common
{
  // TODO: Try 64
  // Must match GPU_THREAD_GROUP_SIZE in compute shaders
  public const int GpuThreadGroupSize = 32;

  public const float CelestialDistance = 262144.0f;

  public const int TeamCount = 2;
  public const int TeamNone = -1;

  public const int LayerUI = 5;
  public const int LayerUIMask = (1 << LayerUI);
  public const int LayerEverythingExceptUIMask = ~LayerUIMask;

  public const int LayerFixedOrigin = 12;

  public const int LayerTerrainDistanceHint = 8;
  public const int LayerTerrainDistanceHintMask = (1 << LayerTerrainDistanceHint);
  public const int LayerEverythingExceptTerrainDistanceHintMask = ~LayerTerrainDistanceHintMask;

  public const int LayerTeam1 = 9;
  public const int LayerTeam1Mask = (1 << LayerTeam1);

  public const int LayerTeam2 = 10;
  public const int LayerTeam2Mask = (1 << LayerTeam2);

  public const string TagPlayer = "Player";
  public const string TagMissile = "M";
  public const string TagRl = "R";

  public static readonly Vector3 HiddenPosition = new Vector3(CelestialDistance, -CelestialDistance, CelestialDistance);

  public static readonly Bounds NeverCullBounds = new Bounds(Vector3.zero, new Vector3(CelestialDistance, CelestialDistance, CelestialDistance));

  public static readonly Color InfinityColor = new Color(float.PositiveInfinity, 0.0f, 0.0f);

  static readonly Color s_tf1PrimaryColor = new Color(0.0f, 2.0f, 2.0f);
  static readonly Color s_tf2PrimaryColor = new Color(1.8f, 0.2f, 1.8f);

  // Functions have been created for compatibility with HLSL version
  #region HLSL Functions

#pragma warning disable IDE1006

  static float2 float2(float x, float y)
  {
    return new float2(x, y);
  }

  static float3 float3(float x, float y, float z)
  {
    return new float3(x, y, z);
  }

  static float frac(float v)
  {
    return v - Mathf.Floor(v);
  }

  static float2 frac(float2 v)
  {
    return float2(frac(v.x), frac(v.y));
  }

  static float round(float v)
  {
    return Mathf.Round(v);
  }

  static float3 round(float3 v)
  {
    return float3(round(v.x), round(v.y), round(v.z));
  }

  static float min(float a, float b)
  {
    return Mathf.Min(a, b);
  }

  static float saturate(float v)
  {
    return Mathf.Clamp01(v);
  }

  static float2 floor(float2 v)
  {
    return float2(Mathf.Floor(v.x), Mathf.Floor(v.y));
  }

  static float3 normalize(float3 v)
  {
    return Vector3.Normalize(v);
  }

  static float dot(float2 lhs, float2 rhs)
  {
    return Vector2.Dot(lhs, rhs);
  }

  static float dot(float3 lhs, float3 rhs)
  {
    return Vector3.Dot(lhs, rhs);
  }

  static float3 cross(float3 lhs, float3 rhs)
  {
    return Vector3.Cross(lhs, rhs);
  }

  static float2 add(float2 a, float b)
  {
    return new float2(a.x + b, a.y + b);
  }

  static float2 mul(float2 a, float b)
  {
    return new float2(a.x * b, a.y * b);
  }

  static float2 mul(float2 a, float2 b)
  {
    return new float2(a.x * b.x, a.y * b.y);
  }

  static float2 mul(float2x2 m, float2 v)
  {
    return m.MultiplyVector(v);
  }

  static float lerp(float a, float b, float t)
  {
    return Mathf.LerpUnclamped(a, b, t);
  }

  static float smoothstep(float from, float to, float t)
  {
    return Mathf.SmoothStep(from, to, t);
  }

#pragma warning restore IDE100

  #endregion

  public static int GetGpuDispatchSize(int count)
  {
    return (count + (GpuThreadGroupSize - 1)) / GpuThreadGroupSize;
  }

  public static int GetRandomSeed()
  {
    return unchecked((int)System.DateTime.Now.Ticks);
  }

  public static Matrix4x4 GetReflectionMatrix(Vector4 plane)
  {
    Matrix4x4 result;

    result.m00 = (1.0f - (2.0f * plane.x * plane.x));
    result.m01 = (-2.0f * plane.x * plane.y);
    result.m02 = (-2.0f * plane.x * plane.z);
    result.m03 = (-2.0f * plane.w * plane.x);

    result.m10 = (-2.0f * plane.y * plane.x);
    result.m11 = (1.0f - (2.0f * plane.y * plane.y));
    result.m12 = (-2.0f * plane.y * plane.z);
    result.m13 = (-2.0f * plane.w * plane.y);

    result.m20 = (-2.0f * plane.z * plane.x);
    result.m21 = (-2.0f * plane.z * plane.y);
    result.m22 = (1.0f - (2.0f * plane.z * plane.z));
    result.m23 = (-2.0f * plane.w * plane.z);

    result.m30 = 0.0f;
    result.m31 = 0.0f;
    result.m32 = 0.0f;
    result.m33 = 1.0f;

    return result;
  }

  public static int ApplyTextureSizeBias(int size, int bias)
  {
    while ((bias > 0) && (size > 0))
    {
      size /= 2;
      bias--;
    }

    while (bias < 0)
    {
      size *= 2;
      bias++;
    }

    return size;
  }

  public static int GetSquareTextureSize(int width, int height)
  {
    int size = System.Math.Max(width, height);
    int result = Mathf.NextPowerOfTwo(size);

    return result;
  }

  public static int GetSquareTextureSize(int width, int height, int bias)
  {
    int result = GetSquareTextureSize(width, height);

    return ApplyTextureSizeBias(result, bias);
  }

  // How the size of a shadow map is calculated, from:
  // https://docs.unity3d.com/Manual/LightPerformance.html
  public static int GetShadowMapSize(Camera camera, Light light)
  {
    int size = System.Math.Max(camera.pixelWidth, camera.pixelHeight);
    int result = Mathf.NextPowerOfTwo((int)(1.9f * size));

    // Assume a GPU with more than 512 MiB of memory
    int sizeLimit;

    switch (light.GetShadowResolution())
    {
      case ShadowResolution.Low:
        sizeLimit = 1024;
        break;
      case ShadowResolution.Medium:
        sizeLimit = 2048;
        break;
      case ShadowResolution.High:
        sizeLimit = 4096;
        break;
      default:
        sizeLimit = 8192;
        break;
    }

    return System.Math.Min(result, sizeLimit);
  }

  public static int GetShadowMapSize(Camera camera, Light light, int bias)
  {
    int result = GetShadowMapSize(camera, light);

    return ApplyTextureSizeBias(result, bias);
  }

  public static void SetRenderTextureProperties(RenderTexture r)
  {
    r.antiAliasing = 1;
    r.autoGenerateMips = false;
    r.useDynamicScale = false;
    r.useMipMap = false;
    r.wrapMode = TextureWrapMode.Clamp;
  }

  public static byte ConvertToColor32Byte(float colorComponent)
  {
    return (byte)(colorComponent * 255.0f);
  }

  public static Color32 GetShieldColor(float health, float opacity)
  {
    Color32 result = Color32.LerpUnclamped(Color.red, Color.green, health);
    result.a = ConvertToColor32Byte(opacity);
    return result;
  }

  public static Color RandomColorExcludeHue(Color c, float minHue, float maxHue)
  {
    float hue;
    float saturation;
    float value;

    Color.RGBToHSV(c, out hue, out saturation, out value);

    if ((hue > minHue) && (hue < maxHue))
    {
      if ((hue - minHue) > (maxHue - hue))
      {
        hue = Random.Range(0.0f, minHue);
      }
      else
      {
        hue = Random.Range(maxHue, 1.0f);
      }

      c = Color.HSVToRGB(hue, saturation, value);
    }

    return c;
  }

  public static void Randomize8BitTexture(Texture2D texture)
  {
    NativeArray<byte> data = texture.GetRawTextureData<byte>();

    for (int i = 0; i < data.Length; i++)
    {
      data[i] = (byte)Random.Range(0.0f, 255.0f);
    }

    texture.Apply();
  }

  public static int GetTeamLayer(int team)
  {
    if (team == 0)
    {
      return LayerTeam1;
    }
    else
    {
      return LayerTeam2;
    }
  }

  public static int GetTeamLayerMask(int team)
  {
    if (team == 0)
    {
      return LayerTeam1Mask;
    }
    else
    {
      return LayerTeam2Mask;
    }
  }

  public static Color GetTeamPrimaryColor(int team)
  {
    if (team == 0)
    {
      return s_tf1PrimaryColor;
    }
    else
    {
      return s_tf2PrimaryColor;
    }
  }

  public static bool InputGetAxisButtonDown(string axisButtonName, bool invert, ref bool pressed)
  {
    float axis = Input.GetAxis(axisButtonName);

    if (invert)
    {
      axis *= -1.0f;
    }

    if (axis > 0.5f)
    {
      if (!pressed)
      {
        pressed = true;
        return true;
      }
    }
    else
    {
      pressed = false;
    }

    return false;
  }
}
