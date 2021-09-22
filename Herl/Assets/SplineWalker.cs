// Derived from https://catlikecoding.com/unity/tutorials/curves-and-splines/

using UnityEngine;

public class SplineWalker : MonoBehaviour
{
  public BezierSpline Spline;
  public float Duration;
  public float Progress;
  public Vector3 Offset;
  public Vector3 Scale;
  public bool LookForward;
  public float PitchRatio;
  public SplineWalkerMode Mode;

  bool goingForward = true;

  void Update()
  {
    if (goingForward)
    {
      Progress += Time.deltaTime / Duration;

      if (Progress > 1.0f)
      {
        if (Mode == SplineWalkerMode.Once)
        {
          Progress = 1.0f;
        }
        else if (Mode == SplineWalkerMode.Loop)
        {
          Progress -= 1.0f;
        }
        else
        {
          Progress = 2.0f - Progress;
          goingForward = false;
        }
      }
    }
    else
    {
      Progress -= Time.deltaTime / Duration;

      if (Progress < 0.0f)
      {
        Progress = -Progress;
        goingForward = true;
      }
    }

    Vector3 oldSplinePosition = Spline.transform.localPosition;
    Vector3 oldSplineScale = Spline.transform.localScale;

    Spline.transform.localPosition = oldSplinePosition + Offset;
    Spline.transform.localScale = Vector3.Scale(oldSplineScale, Scale);

    Vector3 position = Spline.GetPoint(Progress);
    transform.position = position;

    if (LookForward)
    {
      Vector3 direction = Spline.GetDirection(Progress);
      direction.y *= PitchRatio;

      transform.LookAt(position + direction.normalized);
    }

    Spline.transform.localScale = oldSplineScale;
    Spline.transform.localPosition = oldSplinePosition;
  }
}
