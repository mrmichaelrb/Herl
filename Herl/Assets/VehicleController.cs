using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;
using System.IO;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : SphereWorldCollider
{
  const int Team = 0;

  const float VelocityForce = 50000.0f;
  const float VelocityBoostFactor = 4.0f;

  const float GlitchFadeSpeed = 1.0f;

  const float MissileDelay = 0.05f;
  const float MissileLength = 6.0f;
  const float MissileSpeed = 4000.0f;
  const float MissileTime = 2.0f;
  const float MissileSideOffset = 4.5f;

  const float VelocityAudioPitchFactor = 2.0e-7f;
  const float VelocityAudioPitchOffset = 1.0f;
  const float VelocityAudioVolumeRatio = 0.8f;

  const float RotationAudioPitchFactor = 64.0f;
  const float RotationAudioPitchOffset = 0.5f;
  const float RotationAudioPitchMax = 4.0f;
  const float RotationAudioVolumeRatio = 0.0625f;

  const float TestDistanceFromTerrain = 30.0f;
  const float TestTravelDistance = 50000.0f;
  const float TestSampleSize = 1.0f;
  const float TestLookAhead = 10.0f;

  const int NumTestSamples = (int)(TestTravelDistance / TestSampleSize);
  const int NumLookAheads = (int)(TestLookAhead / TestSampleSize);
  const int NumTestSampleHeights = NumTestSamples + NumLookAheads;

  public GameObject Player;
  public GameObject AimingAnchor;
  public GameObject Cockpit;
  public MissileGun Blaster;
  public PostProcessVolume PostProcessingVolume;
  public AudioSource VelocityAudio;
  public AudioSource RotationAudio;

  FullereneShield _shield;
  Glitch _glitch;

  bool _invert = false;
  bool _mouse = false;
  bool _steer = true;
  bool _move = true;
  bool _boost = false;
  bool _boostPressed = false;
  bool _test = false;
  bool _log = false;

  Vector3 _lastVelocityNormalized;

  Vector3[] _testSamples;
  int _testSampleIndex;

  StreamWriter _logFile;

  protected override void Awake()
  {
    base.Awake();

    _shield = GetComponentInChildren<FullereneShield>();
    PostProcessingVolume.profile.TryGetSettings(out _glitch);

    _shield.Reset();

    _world.AddActivePlayer(gameObject, Team);
  }

  void Start()
  {
    VirtualReality.CenterHmd();
  }

  float GetTestSampleHeight(float[] heights, int indexBegin)
  {
    int stepEnd = indexBegin + NumLookAheads;

    float height = 0.0f;
    for (int i = indexBegin; i < stepEnd; i++)
    {
      height = Mathf.Max(height, heights[i]);
    }

    return height;
  }

  void GenerateTestSamples()
  {
    if (_testSamples == null)
    {
      Vector3 position = Vector3.zero;
      Vector2 location = position.xz();
      Vector2[] locations = new Vector2[NumTestSampleHeights];

      locations[0] = location;

      for (int i = 1; i < NumTestSampleHeights; i++)
      {
        location.y += TestSampleSize;
        locations[i] = location;
      }

#pragma warning disable 618
      float[] heights = _world.GetTerrainHeights(locations);
#pragma warning restore 618

      _testSamples = new Vector3[NumTestSamples];

      for (int i = 0; i < NumTestSamples; i++)
      {
        _testSamples[i].Set(locations[i].x, GetTestSampleHeight(heights, i), locations[i].y);
      }
    }
  }

  void FadeGlitch()
  {
    float intensity = _glitch.Intensity.value;

    if (intensity > 0.0f)
    {
      intensity -= (GlitchFadeSpeed * Time.deltaTime);

      if (intensity < 0.0f)
      {
        intensity = 0.0f;
      }

      _glitch.Intensity.value = intensity;
    }
  }

  void HandleMove()
  {
    if (Application.isEditor)
    {
      if (_move)
      {
        if (Input.GetKeyUp(KeyCode.RightShift))
        {
          _move = false;
        }
      }
      else
      {
        _move = Input.GetKey(KeyCode.RightShift);
      }
    }
  }

  void HandleSteer()
  {
    if (Input.GetButtonDown("Free Look"))
    {
      _steer = !_steer;

      Cockpit.transform.rotation = transform.rotation;

      if ((_steer) && (XRSettings.enabled))
      {
        InputTracking.Recenter();
      }
    }

    if (!XRSettings.enabled)
    {
      if (Input.GetButtonDown("Mouse"))
      {
        _mouse = !_mouse;
      }

      if (_mouse)
      {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
      }
      else
      {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
      }

      const float stickSpeed = 50.0f;

      float stickHorizontal = Input.GetAxis("Horizontal") * Time.deltaTime * stickSpeed;
      float stickVertical = Input.GetAxis("Vertical") * Time.deltaTime * stickSpeed;

      float x = stickVertical;
      float y = stickHorizontal;
      float z = stickHorizontal;

      if (_mouse)
      {
        const float mouseSpeed = 1.0f;

        float mouseX = Input.GetAxis("Mouse X") * mouseSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSpeed;

        x += mouseY;
        y += mouseX;
        z += mouseX;
      }

      if (Input.GetButtonDown("Invert"))
      {
        _invert = !_invert;
      }

      if (_invert)
      {
        x *= -1.0f;
      }

      float roll = transform.rotation.eulerAngles.z;
      if (roll > 180.0f)
      {
        roll = roll - 360.0f;
      }

      transform.Rotate(0.0f, 0.0f, roll * Time.deltaTime * -0.4f);

      if (_steer)
      {
        AimingAnchor.transform.Rotate(-x, y, -z * 0.5f);
        AimingAnchor.transform.rotation = Quaternion.Slerp(AimingAnchor.transform.rotation, transform.rotation, Time.deltaTime * 2.0f);
      }
      else
      {
        Cockpit.transform.Rotate(-x, y, 0.0f);
      }
    }

    if (_steer)
    {
      transform.rotation = Quaternion.Slerp(transform.rotation, AimingAnchor.transform.rotation, Time.deltaTime * 2.0f);
    }
    else if (!XRSettings.enabled)
    {
      Cockpit.transform.rotation = Quaternion.Slerp(Cockpit.transform.rotation, AimingAnchor.transform.rotation, Time.deltaTime * 2.0f);
    }
  }

  void HandleBoost()
  {
    if (Common.InputGetAxisButtonDown("Boost", false, ref _boostPressed))
    {
      _boost = !_boost;
    }
  }

  void HandleRecenter()
  {
    if (Input.GetButtonDown("Center"))
    {
      VirtualReality.CenterHmd();
    }
  }

  void HandleBlaster()
  {
    if ((Input.GetButton("Blaster")) || (Input.GetAxis("Blaster") > 0.5f))
    {
      Blaster.Shoot(Team);
    }
  }

  void HandleLog()
  {
    if (Input.GetButtonDown("Log"))
    {
      _log = !_log;
    }

    if (_log)
    {
      if (_logFile == null)
      {
        _logFile = new StreamWriter("Statistics.csv");
      }

      _logFile.Write(Time.deltaTime * 1000.0f);
      _logFile.Write(",");

      float gpuTimeLastFrame;
      if (XRStats.TryGetGPUTimeLastFrame(out gpuTimeLastFrame))
      {
        _logFile.Write(gpuTimeLastFrame);
      }
      _logFile.Write(",");

      _logFile.Write(VirtualReality.NumFramePresents());
      _logFile.Write(",");

      int droppedFrameCount = 0;
      if (XRStats.TryGetDroppedFrameCount(out droppedFrameCount))
      {
        _logFile.Write(droppedFrameCount);
      }
      _logFile.WriteLine();
    }
    else
    {
      if (_logFile != null)
      {
        _logFile.Close();
        _logFile = null;
      }
    }
  }

  void LookForward()
  {
    transform.rotation = Quaternion.identity;
    Player.transform.localPosition = -AimingAnchor.transform.localPosition;
    Player.transform.localRotation = Quaternion.Inverse(AimingAnchor.transform.localRotation);
  }

  void HandleTest()
  {
    if (Input.GetButtonDown("Test"))
    {
      _log = true;
      _test = true;
      _testSampleIndex = 0;
    }

    if (_test)
    {
      GenerateTestSamples();

      LookForward();

      if (_testSampleIndex < NumTestSamples)
      {
        gameObject.transform.position = _testSamples[_testSampleIndex];
        _testSampleIndex++;
      }
      else
      {
        _log = false;
        _test = false;
        _testSamples = null;
      }
    }
  }

  void HandleQuit()
  {
    if (Input.GetButtonDown("Quit"))
    {
      Application.Quit();
    }
  }

  void HandleEngineAudio()
  {
    float audioVolume = Mathf.Clamp01(1.0f - ((transform.position.y - _world.transform.position.y) / _world.AtmosphereCeiling));

    VelocityAudio.pitch = (_rigidBody.velocity.sqrMagnitude * VelocityAudioPitchFactor) + VelocityAudioPitchOffset;
    VelocityAudio.volume = audioVolume * VelocityAudioVolumeRatio;

    Vector3 velocityNormalized = _rigidBody.velocity.normalized;
    float angleSpeed = MathfExt.AngleBetweenFast(velocityNormalized, _lastVelocityNormalized);

    if (float.IsNaN(angleSpeed))
    {
      angleSpeed = 0.0f;
    }

    angleSpeed /= Time.deltaTime;

    RotationAudio.pitch = Mathf.Min(RotationAudioPitchMax, (angleSpeed * RotationAudioPitchFactor) + RotationAudioPitchOffset);
    RotationAudio.volume = audioVolume * RotationAudioVolumeRatio;

    _lastVelocityNormalized = velocityNormalized;
  }

  void Die()
  {
  }

  void Damage(Vector3 damagePosition, float healthLoss, bool glitch)
  {
    _shield.Health = _shield.Health - healthLoss;

    if (glitch)
    {
      _glitch.Intensity.value = 1.0f;
    }

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

  void OnCollisionEnter(Collision collision)
  {
    if (collision.gameObject.layer != gameObject.layer)
    {
      Vector3 collisionPosition = collision.contacts[0].point;
      float healthLoss;

      if (collision.gameObject.CompareTag(Common.TagMissile))
      {
        // TODO: Set to valid amount
        healthLoss = 0.005f;
      }
      else
      {
        healthLoss = collision.impulse.magnitude / Time.fixedDeltaTime;
      }

      Damage(collisionPosition, healthLoss, false);
    }
  }

  public override void OnWorldCollision(WorldCollision collision)
  {
    base.OnWorldCollision(collision);

    // Double check because world collisions are done somewhat asynchronously
    if (enabled)
    {
      Vector3 collisionPosition = collision.GetContactPosition();
      float healthLoss = _worldCollisionVelocityChange.magnitude / Time.fixedDeltaTime;

      Damage(collisionPosition, healthLoss, true);
    }
  }

  void Update()
  {
    FadeGlitch();
    HandleSteer();
    HandleMove();
    HandleBoost();
    HandleRecenter();
    HandleBlaster();
    // HandleLog();
    // HandleTest();
    HandleQuit();
  }

  void FixedUpdate()
  {
    if ((_move) && (!_test))
    {
      float forwardForce = VelocityForce * Time.fixedDeltaTime;

      if (_boost)
      {
        forwardForce *= VelocityBoostFactor;
      }

      _rigidBody.AddRelativeForce(0f, 0f, forwardForce, ForceMode.Acceleration);
    }

    float aboveAltitudeMax = transform.position.y - _world.AltitudeMax - _world.transform.position.y;

    if (aboveAltitudeMax > 0)
    {
      _rigidBody.AddForce(0.0f, -aboveAltitudeMax, 0.0f, ForceMode.Acceleration);
    }

    _rigidBody.drag = _world.GetAtmosphericDrag(transform.position.y);

    HandleEngineAudio();
  }
}
