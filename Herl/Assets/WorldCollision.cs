using Unity.Collections;
using UnityEngine;
using float3 = UnityEngine.Vector3;

public struct WorldCollision
{
  public const int ElementSize = 14;

  public float3 StaticPosition;
  public float StaticHeight;
  public float3 StaticNormal;
  public float3 DynamicPosition;
  public float DynamicDistance;
  public float3 DynamicNormal;

  readonly bool _staticCollision;
  readonly bool _dynamicCollision;

  public WorldCollision(NativeArray<float> source, int index, Vector3 velocity)
  {
    int i = index * ElementSize;
    StaticPosition = new float3(source[i], source[i + 1], source[i + 2]);
    StaticHeight = source[i + 3];
    StaticNormal = new float3(source[i + 4], source[i + 5], source[i + 6]);
    DynamicPosition = new float3(source[i + 7], source[i + 8], source[i + 9]);
    DynamicDistance = source[i + 10];
    DynamicNormal = new float3(source[i + 11], source[i + 12], source[i + 13]);

    _staticCollision = (StaticHeight < 0);
    _dynamicCollision = ((DynamicDistance < float.PositiveInfinity) && (Vector3.Dot(velocity, DynamicNormal) < 0.0f));
  }

  public bool StaticCollision
  {
    get
    {
      return _staticCollision;
    }
  }

  public bool DynamicCollision
  {
    get
    {
      return _dynamicCollision;
    }
  }

  public Vector3 GetContactPosition()
  {
    if (_staticCollision && _dynamicCollision)
    {
      return Vector3.LerpUnclamped(StaticPosition, DynamicPosition, 0.5f);
    }
    else if (_staticCollision)
    {
      return StaticPosition;
    }
    else
    {
      return DynamicPosition;
    }
  }
}
