using UnityEngine;

public struct Sun
{
  float _angle;
  Quaternion _rotation;

  public Color Color;
  public float Size;
  public float Hardness;
  public float Intensity;
  public float AngleVelocity;

  public float Angle
  {
    get
    {
      return _angle;
    }
    set
    {
      _angle = value;
      UpdateRotation();
    }
  }

  public Quaternion Rotation
  {
    get
    {
      return _rotation;
    }
  }

  public Vector3 Direction
  {
    get
    {
      return _rotation * Vector3.back;
    }
  }

  public Vector3 LightDirection
  {
    get
    {
      return _rotation * Vector3.forward;
    }
  }

  private void UpdateRotation()
  {
    _rotation = Quaternion.AngleAxis(_angle, Vector3.right);
  }

  public void Rotate(float seconds)
  {
    _angle += AngleVelocity * seconds;
    UpdateRotation();
  }
}
