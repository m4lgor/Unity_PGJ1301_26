using UnityEngine;

/// <summary>
/// Visual and audio feedback for a RocketEngine.
///
/// This component NEVER applies a force. It only reads RocketEngine.GetThrustRatio()
/// and shows it, so what you see on screen matches the force that is really being
/// applied to the spaceship. That makes it a debugging tool as much as an effect:
/// if an engine lights up when you did not expect it to, your force allocation is wrong.
/// </summary>
[RequireComponent(typeof(RocketEngine))]
public class RocketEngineFX : MonoBehaviour
{
    [Header("References (children of this engine)")]
    [SerializeField] private Transform _Flame;
    [SerializeField] private Light _Light;
    [SerializeField] private AudioSource _Audio;

    [Header("Flame")]
    [Tooltip("Flame length (in engine local units) at full thrust.")]
    [SerializeField] private float _MaxFlameLength = 2.0f;
    [Tooltip("Distance from the engine centre where the flame starts.")]
    [SerializeField] private float _NozzleOffset = 1.0f;
    [SerializeField] private Color _ColdColor = new Color(1.0f, 0.45f, 0.08f, 1.0f);
    [SerializeField] private Color _HotColor = new Color(0.55f, 0.85f, 1.0f, 1.0f);
    [Tooltip("Extra brightness pushed into emission at full thrust (HDR).")]
    [SerializeField] private float _EmissionBoost = 4.0f;

    [Header("Light")]
    [SerializeField] private float _MaxLightIntensity = 3.0f;

    [Header("Audio (optional - assign a looping clip)")]
    [SerializeField] private float _MaxVolume = 0.35f;
    [SerializeField] private float _MinPitch = 0.85f;
    [SerializeField] private float _MaxPitch = 1.30f;

    [Header("Response")]
    [Tooltip("How fast the visuals catch up with the thrust. Higher = snappier.")]
    [SerializeField] private float _Responsiveness = 18.0f;

    private RocketEngine _Engine;
    private Renderer _FlameRenderer;
    private MaterialPropertyBlock _Block;
    private Vector3 _FlameBaseScale;
    private float _DisplayedRatio;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        _Engine = GetComponent<RocketEngine>();

        if (_Flame)
        {
            _FlameBaseScale = _Flame.localScale;
            _FlameRenderer = _Flame.GetComponent<Renderer>();
            _Block = new MaterialPropertyBlock();
        }

        if (_Audio)
        {
            _Audio.loop = true;
            _Audio.playOnAwake = false;
            _Audio.volume = 0.0f;

            if (_Audio.clip)
            {
                _Audio.Play();
            }
        }
    }

    /// <summary>
    /// LateUpdate (not FixedUpdate): this is presentation, it should run once per
    /// rendered frame so the flame stays smooth whatever the physics rate is.
    /// </summary>
    private void LateUpdate()
    {
        float target = _Engine ? _Engine.GetThrustRatio() : 0.0f;

        // Frame-rate independent smoothing, so the flame does not pop on and off
        // between two physics steps.
        _DisplayedRatio = Mathf.Lerp(_DisplayedRatio, target, 1.0f - Mathf.Exp(-_Responsiveness * Time.deltaTime));

        ApplyFlame(_DisplayedRatio);
        ApplyLight(_DisplayedRatio);
        ApplyAudio(_DisplayedRatio);
    }

    private void ApplyFlame(float ratio)
    {
        if (!_Flame)
        {
            return;
        }

        bool visible = ratio > 0.01f;
        if (_Flame.gameObject.activeSelf != visible)
        {
            _Flame.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        // The engine pushes along -transform.right, so the exhaust comes out of +right.
        float length = _MaxFlameLength * ratio;

        Vector3 scale = _FlameBaseScale;
        scale.y = length; // the flame mesh is a unit sphere, so scale.y == total length
        _Flame.localScale = scale;
        _Flame.localPosition = Vector3.right * (_NozzleOffset + length * 0.5f);

        if (_FlameRenderer && _Block != null)
        {
            Color c = Color.Lerp(_ColdColor, _HotColor, ratio);

            _FlameRenderer.GetPropertyBlock(_Block);
            _Block.SetColor(BaseColorId, c);
            _Block.SetColor(EmissionColorId, c * (1.0f + _EmissionBoost * ratio));
            _FlameRenderer.SetPropertyBlock(_Block);
        }
    }

    private void ApplyLight(float ratio)
    {
        if (!_Light)
        {
            return;
        }

        _Light.enabled = ratio > 0.01f;
        _Light.intensity = _MaxLightIntensity * ratio;
        _Light.color = Color.Lerp(_ColdColor, _HotColor, ratio);
    }

    private void ApplyAudio(float ratio)
    {
        if (!_Audio || !_Audio.clip)
        {
            return;
        }

        _Audio.volume = _MaxVolume * ratio;
        _Audio.pitch = Mathf.Lerp(_MinPitch, _MaxPitch, ratio);
    }
}
