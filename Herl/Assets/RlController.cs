using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RlController : SphereWorldCollider
{
  const int ExecutionPoolSize = 64;
  const float DamageForceMin = 1000.0f;
  const float OrbitScale = 3.5f;
  const float AimingLeadTime = 0.1f;
  const float EnemySearchProductMin = 0.125f;
  const float EnemySearchDistanceMax = 2048.0f;
  const float EnemySearchDistanceMaxSquared = EnemySearchDistanceMax * EnemySearchDistanceMax;
  const float BaseEnemyDistanceMax = EnemySearchDistanceMax * 2.0f;
  const float BaseEnemyDistanceMaxSquared = BaseEnemyDistanceMax * BaseEnemyDistanceMax;
  const float AimProductMin = 0.99f;
  const float FollowProductMin = 0.75f;
  const float ShootDistanceMax = 1000.0f;
  const float VelocityForce = 50000.0f;
  const float FlockingAvoidanceDistanceRatio = 8.0f;
  const float FlockingAvoidanceVelocityScale = 0.125f;
  const float FlockingAvoidanceVelocityMax = 10.0f;
  const float TerrainAvoidancePredictionTime = 0.5f;
  const float TerrainAvoidanceDistanceRatio = 1.5f;
  const float TerrainSampleDistanceMax = 100.0f;
  const float TerrainSampleAccuracyMin = 0.97f;

  static ExecutionPool s_executionPool = new ExecutionPool(ExecutionPoolSize);
  static readonly Vector3 s_shieldRotation = new Vector3(0.1f, 0.1f, 0.1f);

  ExecutionPool.Registration _executionPoolRegistration;
  GameObject _enemy;
  Vector3 _turnTorque;
  float _forwardForce;
  Vector3 _flockingAvoidanceForce;
  MissileGun _gun;
  FullereneShield _shield;
  int _team;
  int _teamLayerMask;
  Tf _base;
  float _baseOrbitRatio;
  TerrainHeightSampler _terrainSampler;

  float _terrainAvoidanceDistance;
  float _flockingAvoidanceDistance;

  public void Reset(int team)
  {
    // Must be done first to ensure that Awake has been called
    gameObject.SetActive(true);

    _team = team;
    _teamLayerMask = Common.GetTeamLayerMask(team);

    _base = _world.GetTeamTf(_team);
    _baseOrbitRatio = Random.Range(-0.5f, 0.5f);

    _rigidBody.useGravity = false;
    _rigidBody.velocity = Vector3.zero;

    _shield.Reset();
  }

  void Die()
  {
    _rigidBody.useGravity = true;
  }
 
  void Damage(Vector3 damagePosition, float force)
  {
    if (force > DamageForceMin)
    {
      _shield.Health = _shield.Health - 0.005f;

      // TODO: Remove Recharge
      if (_shield.Health <= 0.0f)
      {
        _shield.Health = 1.0f;
      }

      if (_shield.Health > 0.0f)
      {
        _shield.Damage(damagePosition);
      }
      else
      {
        Die();
      }
    }
  }

  void OnCollisionEnter(Collision collision)
  {
    if (collision.gameObject.layer != gameObject.layer)
    {
      Vector3 collisionPosition = collision.contacts[0].point;
      float force;

      if (collision.gameObject.CompareTag(Common.TagMissile))
      {
        // TODO: Set to valid amount
        force = DamageForceMin * 10.0f;
      }
      else
      {
        force = collision.impulse.magnitude / Time.fixedDeltaTime;
      }

      Damage(collisionPosition, force);
    }
  }

  public override void OnWorldCollision(WorldCollision collision)
  {
    base.OnWorldCollision(collision);

    // Double check because world collisions are done somewhat asynchronously
    if (enabled)
    {
      Vector3 collisionPosition = collision.GetContactPosition();
      float force = _worldCollisionVelocityChange.magnitude / Time.fixedDeltaTime;

      Damage(collisionPosition, force);
    }
  }

  protected override void Awake()
  {
    base.Awake();

    _executionPoolRegistration = s_executionPool.Register();

    _gun = GetComponent<MissileGun>();
    _shield = GetComponentInChildren<FullereneShield>();

    float sphereRadius = transform.localScale.x * _sphereCollider.radius;

    _terrainAvoidanceDistance = sphereRadius * TerrainAvoidanceDistanceRatio;
    _flockingAvoidanceDistance = sphereRadius * FlockingAvoidanceDistanceRatio;
  }

  protected override void OnDisable()
  {
    base.OnDisable();

    if (_terrainSampler != null)
    {
      _terrainSampler.RemoveFromWorld();
      _terrainSampler = null;
    }
  }

  void Update()
  {
    if (_shield.Opacity > 0)
    {
      _shield.transform.Rotate(s_shieldRotation * Time.deltaTime);
    }
  }

  void FixedUpdate()
  {
    if (_executionPoolRegistration.IsScheduled)
    {
      Vector3 basePosition = _base.transform.position;
      Vector3 position = transform.position;
      Vector3 targetDirection;
      _turnTorque = Vector3.zero;
      float turnScale;

      // Search for an enemy
      if (_enemy == null)
      {
        Vector3 enemyDisplacement;
        float enemyDistanceSquared;
        GameObject possibleEnemy = _world.GetNearestActivePlayer(position, _team, true, transform.forward, EnemySearchProductMin, out enemyDisplacement, out enemyDistanceSquared);

        if (possibleEnemy != null )
        {
          if (enemyDistanceSquared < EnemySearchDistanceMaxSquared)
          {
            _enemy = possibleEnemy;
          }
        }
      }

      // Abandon an enemy that is too far away from the base
      if (_enemy != null)
      {
        float baseEnemyDistanceSquared = (basePosition - position).sqrMagnitude;

        if (baseEnemyDistanceSquared > BaseEnemyDistanceMaxSquared)
        {
          _enemy = null;
        }
      }

      // Orbit the base
      if (_enemy == null)
      {
        Vector3 baseOrbitDirection = basePosition - position;
        baseOrbitDirection = Vector3.Cross(baseOrbitDirection, Vector3.up);
        baseOrbitDirection.Normalize();

        Vector3 baseOrbitPosition = (baseOrbitDirection * _base.Radius * OrbitScale) + basePosition;
        baseOrbitPosition.y += _baseOrbitRatio * _base.Radius;

        targetDirection = baseOrbitPosition - position;
        targetDirection.Normalize();

        turnScale = 600.0f;
        _forwardForce = VelocityForce * Time.fixedDeltaTime;
      }
      // Fly toward and shoot enemy
      else
      {
        Vector3 targetPosition = _enemy.transform.position + (_enemy.GetComponent<Rigidbody>().velocity * AimingLeadTime);
        Vector3 targetDisplacement = targetPosition - position;
        targetDirection = targetDisplacement.normalized;

        turnScale = 600.0f;
        _forwardForce = VelocityForce * Time.fixedDeltaTime;

        float aimProduct = Vector3.Dot(transform.forward, targetDirection);

        if (aimProduct > AimProductMin)
        {
          if (targetDisplacement.magnitude < ShootDistanceMax)
          {
            bool targetObstructed = Physics.Raycast(_gun.MissileOrigin, _gun.MissileDirection, ShootDistanceMax, _teamLayerMask);

            if (!targetObstructed)
            {
              _gun.Shoot(_team);
            }
          }
        }
        else if (aimProduct > FollowProductMin)
        {
          turnScale *= 4.0f;
          _forwardForce = 0.75f * VelocityForce * Time.fixedDeltaTime;
        }
      }

      // Terrain avoidance
      if (position.y < (_world.TerrainCeiling + _world.transform.position.y))
      {
        Vector3 predictedPosition = (_rigidBody.velocity * TerrainAvoidancePredictionTime) + position;

        _terrainSampler = TerrainHeightSampler.GetSampler(_world, _terrainSampler, predictedPosition);

        if (_terrainSampler.IsPredictive(position, TerrainSampleDistanceMax, TerrainSampleAccuracyMin))
        {
          float predictedHeightMin = predictedPosition.y - _terrainAvoidanceDistance;

          if (predictedHeightMin < _terrainSampler.HeightAboveTerrain)
          {
            targetDirection.y += 0.5f;
            targetDirection.Normalize();
          }
        }
        else
        {
          _forwardForce = 0.5f * VelocityForce * Time.fixedDeltaTime;
        }
      }

      _flockingAvoidanceForce = Vector3.zero;

      Collider[] nearbyflockingColliders = Physics.OverlapSphere(position, _flockingAvoidanceDistance, _teamLayerMask);

      foreach (Collider flockingCollider in nearbyflockingColliders)
      {
        Vector3 colliderDisplacement = flockingCollider.transform.position - position;
        colliderDisplacement = colliderDisplacement.GetReciprical();
        colliderDisplacement = colliderDisplacement.Clamp(-FlockingAvoidanceVelocityMax, FlockingAvoidanceVelocityMax);

        _flockingAvoidanceForce -= colliderDisplacement;
      }

      float productAvoidanceTargetProduct = Vector3.Dot(_flockingAvoidanceForce, targetDirection);
      _flockingAvoidanceForce -= targetDirection * productAvoidanceTargetProduct;
      _flockingAvoidanceForce *= FlockingAvoidanceVelocityScale;

      Vector3 rotationAxis = Vector3.Cross(transform.forward, targetDirection);

      float theta = MathfExt.AsinFast(rotationAxis.magnitude);

      if (!float.IsNaN(theta))
      {
        _turnTorque = rotationAxis.normalized * theta;
        _turnTorque *= turnScale * Time.fixedDeltaTime;
        _turnTorque -= _rigidBody.angularVelocity;
      }
    }

    _rigidBody.drag = _world.GetAtmosphericDrag(transform.position.y);

    _rigidBody.AddTorque(_turnTorque, ForceMode.Acceleration);
    _rigidBody.AddRelativeForce(0.0f, 0.0f, _forwardForce, ForceMode.Acceleration);
    _rigidBody.AddRelativeForce(_flockingAvoidanceForce, ForceMode.VelocityChange);
  }
}
