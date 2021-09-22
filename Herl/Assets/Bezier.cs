// Derived from https://catlikecoding.com/unity/tutorials/curves-and-splines/

using UnityEngine;

public static class Bezier
{

  public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
  {
    t = Mathf.Clamp01(t);
    float oneMinusT = 1.0f - t;
    return
      oneMinusT * oneMinusT * p0 +
      2.0f * oneMinusT * t * p1 +
      t * t * p2;
  }

  public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
  {
    return
      2.0f * (1.0f - t) * (p1 - p0) +
      2.0f * t * (p2 - p1);
  }

  public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
  {
    t = Mathf.Clamp01(t);
    float oneMinusT = 1.0f - t;
    return
      oneMinusT * oneMinusT * oneMinusT * p0 +
      3.0f * oneMinusT * oneMinusT * t * p1 +
      3.0f * oneMinusT * t * t * p2 +
      t * t * t * p3;
  }

  public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
  {
    t = Mathf.Clamp01(t);
    float oneMinusT = 1.0f - t;
    return
      3.0f * oneMinusT * oneMinusT * (p1 - p0) +
      6.0f * oneMinusT * t * (p2 - p1) +
      3.0f * t * t * (p3 - p2);
  }

  public static Vector3 GetSecondDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
  {
    t = Mathf.Clamp01(t);
    float oneMinusT = 1.0f - t;
    return
      6.0f * oneMinusT * (p2 - (2.0f * p1) + p0) +
      6.0f * t * (p3 - (2.0f * p2) + p1);
  }
}
