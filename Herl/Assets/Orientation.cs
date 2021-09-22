using UnityEngine;

public class Orientation : MonoBehaviour
{
  public GameObject FollowObject;

  void Update()
  {
    transform.rotation = FollowObject.transform.rotation;
  }
}
