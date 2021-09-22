using UnityEngine;

// Only floats X and Z coordinates
public class FloatingOrigin : MonoBehaviour
{
  const float Threshold = 1024.0f;

  ParticleSystem.Particle[] _particleBuffer = null;

  void FixedUpdate()
  {
    Vector3 position = gameObject.transform.position;
    Vector3 centeredOffset = Vector3.zero;

    if (position.x < -Threshold)
    {
      centeredOffset.x += Threshold;
    }
    else if (position.x > Threshold)
    {
      centeredOffset.x -= Threshold;
    }

    if (position.y < -Threshold)
    {
      centeredOffset.y += Threshold;
    }
    else if (position.y > Threshold)
    {
      centeredOffset.y -= Threshold;
    }

    if (position.z < -Threshold)
    {
      centeredOffset.z += Threshold;
    }
    else if (position.z > Threshold)
    {
      centeredOffset.z -= Threshold;
    }

    if (centeredOffset != Vector3.zero)
    {
      Object[] transformObjects = FindObjectsOfType(typeof(Transform));
      foreach (Object transformObject in transformObjects)
      {
        Transform t = (Transform)transformObject;

        if (t.gameObject.layer != Common.LayerFixedOrigin)
        {
          if (t.parent == null)
          {
            t.position += centeredOffset;
          }
        }
      }

      Object[] particleSytemObjects = FindObjectsOfType(typeof(ParticleSystem));
      foreach (Object particleSytemObject in particleSytemObjects)
      {
        ParticleSystem ps = (ParticleSystem)particleSytemObject;
        ParticleSystem.MainModule main = ps.main;

        if (main.simulationSpace == ParticleSystemSimulationSpace.World)
        {
          if ((_particleBuffer == null) || (_particleBuffer.Length < main.maxParticles))
          {
            _particleBuffer = new ParticleSystem.Particle[main.maxParticles];
          }

          int particleCount = ps.GetParticles(_particleBuffer);

          for (int i = 0; i < particleCount; i++)
          {
            _particleBuffer[i].position += centeredOffset;
          }

          ps.SetParticles(_particleBuffer, particleCount);
        }
      }
    }
  }
}
