using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PowerBall : SphereWorldCollider
{
  const float NormalLevelAltitudeOffset = 150.0f;
  const float NormalLevelingAcceleration = 200.0f;
  const float NormalDrag = 0.1f;
  const float SpawningTime = 7.0f;
  const float SpawnedRadius = 50.0f;
  const float SolidRadiusRatio = 0.8f;
  const float TfActionDistance = 4000.0f;
  const float TfGoalDistance = 10.0f;
  const float TfGoalDistanceSquared = TfGoalDistance * TfGoalDistance;
  const float TfActionDistanceSquared = TfActionDistance * TfActionDistance;
  const float TfLevelingAcceleration = 200.0f;
  const float TfPullAcceleration = 1000.0f;
  const float TfPullDrag = 2.0f;
  const float PlayerActionDistance = 1000.0f;
  const float PlayerActionDistanceSquared = PlayerActionDistance * PlayerActionDistance;
  const float PlayerPushAcceleration = 10000000.0f;
  const float PlayerActivateDistance = 500.0f;
  const float PlayerActivateDistanceSquared = PlayerActivateDistance * PlayerActivateDistance;

  Vector3 _spawnPosition;
  float _spawningTimeLeft;
  bool _activated;

  public bool Spawning
  {
    get
    {
      return (_spawningTimeLeft > 0);
    }
  }

  public float ProgressInverse
  {
    get
    {
      return _spawningTimeLeft / SpawningTime;
    }
  }

  public float SolidRadius
  {
    get
    {
      return Radius * SolidRadiusRatio;
    }
  }

  private float NormalLevelAltitude
  {
    get
    {
      return NormalLevelAltitudeOffset + _world.transform.position.y;
    }
  }

  private float TfLevelAltitude
  {
    get
    {
      return _world.Tf1.transform.position.y - (_world.Tf1.Radius + (Radius * 2.0f));
    }
  }


  void Start()
  {
    _spawnPosition = _world.GetActualPosition(transform.position);
    _spawningTimeLeft = SpawningTime;
    _activated = false;
  }

  protected override void OnEnable()
  {
    base.OnEnable();

    CapsuleCollider tf1Cylinder = _world.Tf1Object.GetComponent<CapsuleCollider>();
    CapsuleCollider tf2Cylinder = _world.Tf2Object.GetComponent<CapsuleCollider>();
    Collider powerBallCollider = GetComponent<Collider>();
    Physics.IgnoreCollision(tf1Cylinder, powerBallCollider);
    Physics.IgnoreCollision(tf2Cylinder, powerBallCollider);
  }

  void Respawn()
  {
    transform.position = _world.GetFloatingPosition(_spawnPosition);
    _rigidBody.velocity = Vector3.zero;
    _spawningTimeLeft = SpawningTime;
    _activated = false;
  }

  void Goal(int team)
  {
    _world.Goal(team);
    Respawn();
  }

  void Level(float altitude, float acceleration)
  {
    if (transform.position.y > altitude)
    {
      _rigidBody.AddForce(0.0f, -acceleration, 0.0f, ForceMode.Acceleration);
    }
    else
    {
      _rigidBody.AddForce(0.0f, acceleration, 0.0f, ForceMode.Acceleration);
    }
  }

  bool AttractToTf(int team)
  {
    Tf tf = _world.GetTeamTf(team);

    Vector2 displacement = tf.transform.position.xz() - transform.position.xz();
    float distanceSquared = displacement.sqrMagnitude;

    if (distanceSquared < TfActionDistanceSquared)
    {
      if (distanceSquared < TfGoalDistanceSquared)
      {
        Goal(team);
      }
      else
      {
        _rigidBody.drag = Mathf.Lerp(TfPullDrag, NormalDrag, distanceSquared / TfActionDistanceSquared);

        Vector3 pullAcceleration = new Vector3(displacement.x, 0.0f, displacement.y);
        pullAcceleration.Normalize();
        pullAcceleration *= TfPullAcceleration;

        _rigidBody.AddForce(pullAcceleration, ForceMode.Acceleration);

        Level(TfLevelAltitude, TfLevelingAcceleration);
      }

      return true;
    }

    return false;
  }

  private void Update()
  {
    if (Spawning)
    {
      _spawningTimeLeft -= Time.deltaTime;
      _spawningTimeLeft = Mathf.Max(0.0f, _spawningTimeLeft);

      float scale = (SpawningTime - _spawningTimeLeft) / SpawningTime;
      scale *= SpawnedRadius * 2.0f;
      transform.localScale = new Vector3(scale, scale, scale);
    }
  }

  void FixedUpdate()
  {
    if (!Spawning)
    {
      bool tfAttraction = false;

      for (int team = 0; team < Common.TeamCount; team++)
      {
        tfAttraction |= AttractToTf(team);
      }

      if (!tfAttraction)
      {
        _rigidBody.drag = NormalDrag;

        Vector3 nearestPlayerDisplacement;
        float nearestPlayerDistanceSquared;
        GameObject nearestPlayer = _world.GetNearestActivePlayer(transform.position, Common.TeamNone, false, out nearestPlayerDisplacement, out nearestPlayerDistanceSquared);

        if (nearestPlayer != null)
        {
          if (nearestPlayerDistanceSquared < PlayerActionDistanceSquared)
          {
            Vector3 pushAcceleration = nearestPlayerDisplacement * -0.25f;
            pushAcceleration.y = 0.0f;

            Vector3 playerForward = nearestPlayer.transform.rotation * Vector3.forward;
            pushAcceleration.x += playerForward.x;
            pushAcceleration.z += playerForward.z;

            pushAcceleration.Normalize();

            pushAcceleration *= PlayerPushAcceleration / nearestPlayerDistanceSquared;

            _rigidBody.AddForce(pushAcceleration, ForceMode.Acceleration);
          }

          if (nearestPlayerDistanceSquared < PlayerActivateDistanceSquared)
          {
            _activated = true;
          }
        }

        if (_activated)
        {
          Level(NormalLevelAltitude, NormalLevelingAcceleration);
        }
      }
    }
  }
}
