using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Missile : PointWorldCollider
{
  float _disableTime;

  public void Shoot(Vector3 position, Vector3 velocity, Quaternion rotation, float length, float speed, float lifetime)
  {
    // Must be done first to ensure that Awake has been called
    gameObject.SetActive(true);

    Vector3 missileDirection = rotation * Vector3.forward;
    Vector3 missileVelocity = missileDirection * speed;

    position = position + (missileDirection * length);

    transform.SetPositionAndRotation(position, rotation);

    _rigidBody.velocity = velocity + missileVelocity;

    _disableTime = Time.time + lifetime;
  }

  void OnCollisionEnter(Collision collision)
  {
    gameObject.SetActive(false);
  }

  public override void OnWorldCollision(WorldCollision collision)
  {
    gameObject.SetActive(false);
  }

  void FixedUpdate()
  {
    if (Time.time > _disableTime)
    {
      gameObject.SetActive(false);
    }
  }
}
