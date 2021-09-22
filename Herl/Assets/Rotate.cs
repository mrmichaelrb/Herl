using UnityEngine;

public class Rotate : MonoBehaviour
{
  public Vector3 Axis;
  public float AngularVelocity;

  void LateUpdate()
  {
    float angleDelta = AngularVelocity * Time.deltaTime;
    transform.Rotate(Axis, angleDelta);
  }
}
