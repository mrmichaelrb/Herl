using UnityEngine;

[System.Serializable]
public struct Matrix2x2
{
  public float M11;
  public float M12;
  public float M21;
  public float M22;

  public Matrix2x2(float m11, float m12, float m21, float m22)
  {
    M11 = m11;
    M12 = m12;
    M21 = m21;
    M22 = m22;
  }

  public Vector2 MultiplyVector(Vector2 v)
  {
    return new Vector2((v.x * M11) + (v.y * M21), (v.x * M12) + (v.y * M22));
  }

  public Vector3 MultiplyVectorXZ(Vector3 v)
  {
    return new Vector3((v.x * M11) + (v.z * M21), v.y, (v.x * M12) + (v.z * M22));
  }

  public Vector4 ToVector()
  {
    return new Vector4(M11, M12, M21, M22);
  }
}
