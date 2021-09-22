using UnityEngine;

public class TerrainHeightSampler
{
  public const int ElementSize = 1;

  public Vector2 RequestedLocation;
  public Vector2 SampledLocation;
  public float Height;

  World _world;
  bool _sampled = false;

  public static TerrainHeightSampler GetSampler(World world, TerrainHeightSampler sampler, Vector2 location)
  {
    if (sampler == null)
    {
      sampler = new TerrainHeightSampler(world, location);
    }
    else
    {
      sampler.RequestedLocation = location;
    }

    return sampler;
  }

  public static TerrainHeightSampler GetSampler(World world, TerrainHeightSampler sampler, Vector3 position)
  {
    return GetSampler(world, sampler, position.xz());
  }

  public TerrainHeightSampler(World world, Vector2 location)
  {
    _world = world;
    RequestedLocation = location;
    AddToWorld();
  }

  void AddToWorld()
  {
    _world.AddTerrainHeightSampler(this);
  }

  public void RemoveFromWorld()
  {
    _world.RemoveTerrainHeightSampler(this);
  }

  public void RecordSample(Vector2 location, float height)
  {
    SampledLocation = location;
    Height = height;
    _sampled = true;
  }

  public float HeightAboveTerrain
  {
    get
    {
      return Height - _world.transform.position.y;
    }
  }

  public bool IsPredictive(Vector2 location, float distanceMax, float accuracyMin)
  {
    if (_sampled)
    {
      if (SampledLocation.Equals(RequestedLocation))
      {
        return true;
      }

      float distanceMaxSquared = distanceMax * distanceMax;
      float sampleDistanceSquared = (SampledLocation - RequestedLocation).sqrMagnitude;

      if (sampleDistanceSquared < distanceMaxSquared)
      {
        Vector2 requestedDirection = RequestedLocation - location;
        Vector2 sampledDirection = SampledLocation - location;
        requestedDirection.Normalize();
        sampledDirection.Normalize();

        if (Vector2.Dot(requestedDirection, sampledDirection) > accuracyMin)
        {
          return true;
        }
      }
    }

    return false;
  }

  public bool IsPredictive(Vector3 position, float distanceMax, float accuracyMin)
  {
    return IsPredictive(position.xz(), distanceMax, accuracyMin);
  }
}
