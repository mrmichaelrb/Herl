using UnityEngine;

public class CircularOrbit : MonoBehaviour
{
  public GameObject BarycenterObject;
  public Vector3 Barycenter;
  public Vector3 Axis;
  public float Angle;
  public float Distance;
  public float AngularVelocity;

  Vector3 _previousAxis;
  Vector3 _tangent;

  void LateUpdate()
  {
    if (BarycenterObject != null)
    {
      Barycenter = BarycenterObject.transform.position;
    }

    float angleDelta = AngularVelocity * Time.deltaTime;
    Angle += angleDelta;
    Angle = Angle % 360.0f;

    if (Axis != _previousAxis)
    {
      _tangent = Axis.GetTangent().normalized;
      _previousAxis = Axis;
    }

    transform.position = Barycenter + (Quaternion.AngleAxis(Angle, Axis) * (_tangent * Distance));
  }
}
