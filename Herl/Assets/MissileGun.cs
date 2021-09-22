using UnityEngine;

public class MissileGun : MonoBehaviour
{
  public Rigidbody Rigidbody;
  public bool Dual;
  public Vector3 MissileOffset;
  public float MissileLength;
  public float MissileSpeed;
  public float MissileLifetime;
  public float MissileReloadTime;
  public AudioSource MissileAudio;
  public AudioSource MissileAudioDual;

  World _world;
  float _nextMissileTime;
  float _dualOffset = 1.0f;

  void Init()
  {
    if (_world == null)
    {
      _world = FindObjectOfType<World>();
    }
  }

  void Awake()
  {
    Init();
  }

  void OnEnable()
  {
    Init();
  }

  public Vector3 MissileOrigin
  {
    get
    {
      Vector3 offset = MissileOffset;
      offset.x *= _dualOffset;
      return transform.position + (transform.rotation * offset);
    }
  }

  public Vector3 MissileDirection
  {
    get
    {
      return transform.rotation * Vector3.forward;
    }
  }

  public void Shoot(int team)
  {
    float time = Time.time;

    if (time > _nextMissileTime)
    {
      Missile newMissile = _world.GetMissilePool(team).GetAvailableComponent();

      if (newMissile != null)
      {
        if (Dual)
        {
          _dualOffset *= -1.0f;
        }

        if (_dualOffset == 1.0f)
        {
          MissileAudio.Play();
        }
        else
        {
          MissileAudioDual.Play();
        }


        newMissile.Shoot(MissileOrigin, Rigidbody.velocity, transform.rotation, MissileLength, MissileSpeed, MissileLifetime);
        _nextMissileTime = time + MissileReloadTime;
      }
    }
  }
}
