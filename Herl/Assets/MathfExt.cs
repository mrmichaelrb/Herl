using UnityEngine;

static class MathfExt
{
  public static float Sign0(float f)
  {
    if (f == 0.0f)
    {
      return f;
    }
    else
    {
      return Mathf.Sign(f);
    }
  }

  public static float AcosFast(float f)
  {
    if ((f < -1.0f) || (f > 1.0f))
    {
      return float.NaN;
    }
    else
    {
      return (((-0.6981317f * f * f) - 0.8726646f) * f) + 1.5707963f;
    }
  }

  public static float AsinFast(float f)
  {
    if ((f < -1.0f) || (f > 1.0f))
    {
      return float.NaN;
    }
    else
    {
      return (((0.6981317f * f * f) + 0.8726646f) * f);
    }
  }

  public static float AngleBetweenFast(Vector3 fromNormalized, Vector3 toNormalized)
  {
    if ((fromNormalized == Vector3.zero) || (toNormalized == Vector3.zero))
    {
      return float.NaN;
    }
    else
    {
      float dotProduct = Vector3.Dot(fromNormalized, toNormalized);
      return AcosFast(dotProduct);
    }
  }
}
