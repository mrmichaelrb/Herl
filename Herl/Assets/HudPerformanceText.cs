using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

[RequireComponent(typeof(Text))]
public class HudPerformanceText : MonoBehaviour
{
  const int UnknownValue = -1;

  const float displayUpdateTime = 0.25f;

  const string UnknownText = "?";

  Text _text;
  World _world;
  int _frameCount;
  float _sampleTime;
  bool _redrawsKnown;
  int _redraws;
  bool _dropsKnown;
  int _drops;

  void Awake()
  {
    _text = GetComponent<Text>();
    _world = FindObjectOfType<World>();
  }

  void Update()
  {
    _frameCount++;
    _sampleTime = _sampleTime + Time.unscaledDeltaTime;

    if (XRSettings.enabled)
    {
      if (XRStats.TryGetFramePresentCount(out int presentCount))
      {
        _redrawsKnown = true;
        _redraws += presentCount;
      }

      if (XRStats.TryGetDroppedFrameCount(out int droppedCount))
      {
        _dropsKnown = true;
        _drops += droppedCount;
      }

      if (_sampleTime > displayUpdateTime)
      {
        string fpsText;
        string redrawsText = UnknownText;
        string dropsText = UnknownText;

        float fps = _frameCount / _sampleTime;
        fpsText = fps.ToString("0.0");

        if (_redrawsKnown)
        {
          redrawsText = _redraws.ToString();
        }

        if (_dropsKnown)
        {
          dropsText = _drops.ToString();
        }

        _text.text = "World: " + _world.Seed + "\nFPS: " + fpsText + "\nRedraws: " + redrawsText + "\nDrops: " + dropsText;

        _frameCount = 0;
        _sampleTime = 0.0f;
        _redrawsKnown = false;
        _redraws = 0;
        _dropsKnown = false;
        _drops = 0;
      }
    }
    else
    {
      if (_sampleTime > displayUpdateTime)
      {
        float fps = _frameCount / _sampleTime;
        string fpsText = fps.ToString("0.0");

        _text.text = "World: " + _world.Seed + "\nFPS: " + fpsText;

        _frameCount = 0;
        _sampleTime = 0.0f;
      }
    }
  }
}
