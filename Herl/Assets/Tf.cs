using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Tf : MonoBehaviour
{
  public AudioSource transformAudio;

  SphereCollider _sphereCollider;

  public float Radius
  {
    get
    {
      return _sphereCollider.radius;
    }
  }

  void Awake()
  {
    _sphereCollider = GetComponent<SphereCollider>();
  }

  public void Goal()
  {
    transformAudio.Play();
  }
}
