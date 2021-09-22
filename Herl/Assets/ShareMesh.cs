using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ShareMesh : MonoBehaviour
{
  public GameObject SharedMeshObject;

  void Update()
  {
    GetComponent<MeshFilter>().sharedMesh = SharedMeshObject.GetComponent<MeshFilter>().sharedMesh;
  }
}
