using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public abstract class WorldCollider : MonoBehaviour
{
  const float GroundLevelRatio = 0.5f;
  const float VelocityRatio = 1 - GroundLevelRatio;

  protected World _world;
  bool _addedToWorld = false;
  protected Rigidbody _rigidBody;
  protected SphereCollider _sphereCollider;
  protected Vector3 _worldCollisionVelocityChange;

  public Vector3 Position
  {
    get
    {
      return transform.position + _sphereCollider.center;
    }
  }

  public Vector3 Velocity
  {
    get
    {
      return _rigidBody.velocity;
    }
  }

  public float Radius
  {
    get
    {
      return _sphereCollider.radius * transform.localScale.x;
    }
  }

  private float Circumference
  {
    get
    {
      return Radius * 2.0f * Mathf.PI;
    }
  }

  public virtual void OnWorldCollision(WorldCollision collision)
  {
    if (enabled)
    {
      Vector3 startVelocity = _rigidBody.velocity;
      Vector3 collisionVelocity = Vector3.zero;
      Vector3 collisionNormal = Vector3.up;
      bool staticCollision = collision.StaticCollision;
      bool dynamicCollision = collision.DynamicCollision;

      if (dynamicCollision)
      {
        collisionVelocity = Vector3.Reflect(startVelocity, collision.DynamicNormal) * _sphereCollider.material.bounciness;
        collisionNormal = collision.DynamicNormal;
      }

      if (staticCollision)
      {
        Vector3 groundLevelPosition = transform.position;
        float distanceToGroundLevel = -collision.StaticHeight;
        groundLevelPosition.y += distanceToGroundLevel * GroundLevelRatio;

        _rigidBody.MovePosition(groundLevelPosition);

        float velocityToGroundLevel = Mathf.Sqrt(Physics.gravity.y * collision.StaticHeight);

        if (dynamicCollision)
        {
          collisionVelocity.y = Mathf.Max(collisionVelocity.y, velocityToGroundLevel);
          collisionNormal = Vector3.Slerp(collision.DynamicNormal, collision.StaticNormal, 0.5f);
        }
        else
        {
          collisionVelocity.x = startVelocity.x + (collision.StaticNormal.x * velocityToGroundLevel);
          collisionVelocity.z = startVelocity.z + (collision.StaticNormal.z * velocityToGroundLevel);
          collisionVelocity.y = Mathf.Max(startVelocity.y, velocityToGroundLevel * VelocityRatio);
          collisionNormal = collision.StaticNormal;
        }
      }

      if (staticCollision || dynamicCollision)
      {
        _rigidBody.velocity = collisionVelocity;

        _worldCollisionVelocityChange = collisionVelocity - startVelocity;

        Vector3 torqueAxis = Vector3.Cross(startVelocity, collisionNormal);
        torqueAxis.Normalize();

        float torqueAmount = _sphereCollider.material.dynamicFriction * _worldCollisionVelocityChange.sqrMagnitude;

        _rigidBody.AddTorque(torqueAxis * -torqueAmount, ForceMode.Impulse);
      }
    }
    else
    {
      OnDisable();
    }
  }

  private void Init()
  {
    if (_world == null)
    {
      _world = FindObjectOfType<World>();
    }

    if (_rigidBody == null)
    {
      _rigidBody = GetComponent<Rigidbody>();
    }

    if (_sphereCollider == null)
    {
      _sphereCollider = GetComponent<SphereCollider>();
    }
  }

  protected virtual void Awake()
  {
    Init();
  }

  protected virtual void OnEnable()
  {
    Init();

    if (!_addedToWorld)
    {
      AddToWorld();
      _addedToWorld = true;
    }
  }

  protected virtual void OnDisable()
  {
    if (_addedToWorld)
    {
      RemoveFromWorld();
      _addedToWorld = false;
    }
  }

  protected abstract void AddToWorld();

  protected abstract void RemoveFromWorld();
}
