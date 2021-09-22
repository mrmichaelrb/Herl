public class SphereWorldCollider : WorldCollider
{
  protected override void AddToWorld()
  {
    _world.AddSphereCollider(this);
  }

  protected override void RemoveFromWorld()
  {
    _world.RemoveSphereCollider(this);
  }
}
