using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class FullereneShield : MonoBehaviour
{
  const int VertexCount = 180;

  const float DamageProductOffset = 0.2f;
  const float MaxOpacity = 0.6f;
  const float FadeTime = 0.8f;
  const float FadeStep = MaxOpacity / FadeTime;
  const float TransitionStep = FadeStep * 2.0f;

  static Vector3[] s_inverseNormals;

  public float Health;
  public bool AlwaysOpaque;

  Mesh _mesh;
  MeshRenderer _meshRenderer;
  readonly Color32[] _colors = new Color32[VertexCount];
  float _opacity;
  float _transition;
  bool _animating;
  bool _animationDone;
  bool _colorsChanged;

  public float Opacity
  {
    get
    {
      return _opacity;
    }
  }

  void CalculateShieldNormals()
  {
    if (s_inverseNormals == null)
    {
      s_inverseNormals = _mesh.vertices;

      for (int i = 0; i < VertexCount; i++)
      {
        s_inverseNormals[i].Normalize();
        s_inverseNormals[i] = s_inverseNormals[i] * -1.0f;
      }
    }
  }

  void Init()
  {
    if (_mesh == null)
    {
      _mesh = GetComponent<MeshFilter>().mesh;
    }

    if (_meshRenderer == null)
    {
      _meshRenderer = GetComponent<MeshRenderer>();
    }

    CalculateShieldNormals();

    Reset();
  }

  void Awake()
  {
    Init();
  }

  void OnEnable()
  {
    Init();
  }

  void StartAnimation()
  {
    if (!_animating)
    {
      FullereneShieldAnimator.AddShield(this);
      _animating = true;
    }

    _animationDone = false;
  }

  void StopAnimation()
  {
    if (_animating)
    {
      FullereneShieldAnimator.RemoveShield(this);
      _animating = false;
    }
  }

  public void Reset()
  {
    _meshRenderer.enabled = AlwaysOpaque;
    _opacity = AlwaysOpaque ? 1.0f : 0.0f;

    Health = 1.0f;
    _transition = 1.0f;
    AnimateShield();
  }

  public void Damage(Vector3 damagePosition)
  {
    Vector3 damageDirection = transform.position - damagePosition;
    damageDirection.Normalize();

    Matrix4x4 rotationMatrix = Matrix4x4.Rotate(transform.rotation);

    for (int i = 0; i < VertexCount; i++)
    {
      Vector3 inverseNormal = rotationMatrix.MultiplyVector(s_inverseNormals[i]);

      float damageProduct = Vector3.Dot(inverseNormal, damageDirection);
      damageProduct += DamageProductOffset;
      damageProduct = Mathf.Clamp01(damageProduct);

      _colors[i] = Color32.LerpUnclamped(_colors[i], Color.red, damageProduct);
      _colors[i].a = Common.ConvertToColor32Byte(MaxOpacity);
    }

    if (!AlwaysOpaque)
    {
      _opacity = MaxOpacity;
    }

    _transition = 0.0f;
    _colorsChanged = true;

    _meshRenderer.enabled = true;
    StartAnimation();
  }

  public void AnimateShield()
  {
    _animationDone = true;

    Color32 shieldColor = Common.GetShieldColor(Health, _opacity);

    for (int i = 0; i < VertexCount; i++)
    {
      Color32 vertexColor = _colors[i];

      if (!vertexColor.Equals(shieldColor))
      {
        _colors[i] = Color32.LerpUnclamped(vertexColor, shieldColor, _transition);
        _animationDone = false;
      }
    }

    _colorsChanged |= !_animationDone;
  }

  void Update()
  {
    float deltaTime = Time.deltaTime;

    if (!AlwaysOpaque)
    {
      _opacity -= FadeStep * deltaTime;

      if (_opacity < 0.0f)
      {
        _opacity = 0.0f;
        _meshRenderer.enabled = false;
      }
    }

    _transition += TransitionStep * deltaTime;
    _transition = Mathf.Clamp01(_transition);

    if (_animationDone)
    {
      StopAnimation();
    }

    if (_colorsChanged)
    {
      _mesh.colors32 = _colors;
      _colorsChanged = false;
    }
  }
}
