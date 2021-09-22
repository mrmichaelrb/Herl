using UnityEngine;

public class Compass : MonoBehaviour
{
  public GameObject FollowObject;

  void Update()
  {
    float zAngle = transform.parent.rotation.eulerAngles.y;
    Quaternion compassRotation;

    if (FollowObject == null)
    {
      compassRotation = Quaternion.Euler(0.0f, 0.0f, zAngle);
    }
    else
    {
      Vector3 targetDirection = FollowObject.transform.position - transform.parent.position;
      targetDirection.y = 0.0f;
      targetDirection.Normalize();

      compassRotation = new Quaternion();
      compassRotation.SetLookRotation(targetDirection);

      float targetZAngle = compassRotation.eulerAngles.y;

      compassRotation = Quaternion.Euler(0.0f, 0.0f, zAngle - targetZAngle);
    }

    transform.localRotation = compassRotation;
  }
}
