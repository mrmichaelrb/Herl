public class PointWorldCollider : WorldCollider
{
  protected override void AddToWorld()
  {
    _world.AddPointCollider(this);
  }

  protected override void RemoveFromWorld()
  {
    _world.RemovePointCollider(this);
  }
}
