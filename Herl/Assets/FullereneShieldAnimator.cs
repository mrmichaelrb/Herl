using System.Collections.Generic;
using UnityEngine;

class FullereneShieldAnimator : MonoBehaviour
{
  const int AnimationLimit = 32;

  static List<FullereneShield> s_animatedShields = new List<FullereneShield>();

  int _lastAnimationIndex;

  public static void AddShield(FullereneShield shield)
  {
    s_animatedShields.Add(shield);
  }

  public static void RemoveShield(FullereneShield shield)
  {
    s_animatedShields.Remove(shield);
  }

  void Update()
  {
    int count = s_animatedShields.Count;

    if (count > 0)
    {
      if (_lastAnimationIndex >= count)
      {
        _lastAnimationIndex = 0;
      }

      int animationIndex = _lastAnimationIndex;
      int animationCount = 0;

      do
      {
        s_animatedShields[animationIndex].AnimateShield();

        animationIndex = (animationIndex + 1) % count;
        animationCount++;

      } while ((animationCount < AnimationLimit) && (animationIndex != _lastAnimationIndex));

      _lastAnimationIndex = animationIndex;
    }
  }
}
