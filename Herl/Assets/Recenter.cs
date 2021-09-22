using UnityEngine;

public class Recenter : MonoBehaviour
{
  public GameObject Eye;
  public float Limit;

  void Update()
  {
    Vector3 localPosition = transform.localPosition;
    Vector3 eyePosition = Eye.transform.localPosition;
    Vector3 relativePosition = localPosition + eyePosition;

    if (relativePosition.x < -Limit)
    {
      localPosition.x += Limit;
    }
    else if (relativePosition.x > Limit)
    {
      localPosition.x -= Limit;
    }

    if (relativePosition.y < -Limit)
    {
      localPosition.y += Limit;
    }
    else if (relativePosition.y > Limit)
    {
      localPosition.y -= Limit;
    }

    if (relativePosition.z < -Limit)
    {
      localPosition.z += Limit;
    }
    else if (relativePosition.z > Limit)
    {
      localPosition.z -= Limit;
    }

    transform.localPosition = localPosition;
  }
}
