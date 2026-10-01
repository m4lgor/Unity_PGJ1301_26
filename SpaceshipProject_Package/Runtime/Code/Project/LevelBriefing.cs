using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows a short briefing when a level starts: what you have to do, and above all
/// WHAT IS NEW compared to the previous level.
///
/// This is deliberately separate from the Rules panel in GlobalSettings:
///   - the Rules panel answers "what is true right now" (a live state readout)
///   - this answers "what changed and what am I supposed to do" (a one-off briefing)
/// The Rules panel cannot do this job: Level02 and Level03 have identical rules but
/// are completely different exercises.
/// </summary>
public class LevelBriefing : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private string _Title = "";

    [Tooltip("One sentence: what the student has to achieve.")]
    [TextArea(2, 5)]
    [SerializeField] private string _Objective = "";

    [Tooltip("What changed since the previous level. One item per line, or separated by ';'.")]
    [TextArea(2, 8)]
    [SerializeField] private string _NewThisLevel = "";

    [Header("Layout")]
    [Tooltip("Panel width in pixels. Narrow keeps it a tall column against the right edge.")]
    [SerializeField] private float _PanelWidth = 300.0f;
    [SerializeField] private float _ScreenMargin = 12.0f;

    [Header("Behaviour")]
    [SerializeField] private bool _ShowOnStart = true;

    [Tooltip("Shows/hides the briefing again during play.")]
    [SerializeField] private Key _ToggleKey = Key.F1;

    private bool _Visible;
    private string[] _Bullets;
    private GUIStyle _Panel, _TitleStyle, _HeadStyle, _ItemStyle;

    private void Awake()
    {
        // Split on newlines (what you get typing in the Inspector) or on ';'
        // (which survives a one-line edit in the scene file).
        _Bullets = _NewThisLevel.Split(new[] { '\n', '\r', ';' }, System.StringSplitOptions.RemoveEmptyEntries);

        _Visible = _ShowOnStart && HasContent();
    }

    private bool HasContent()
    {
        return !string.IsNullOrWhiteSpace(_Title)
            || !string.IsNullOrWhiteSpace(_Objective)
            || _Bullets.Length > 0;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !HasContent())
        {
            return;
        }

        // Note: no mouse dismissal on purpose. A click is already "fly there" for the
        // example controller, and we do not want dismissing the panel to move the ship.
        if (keyboard[_ToggleKey].wasPressedThisFrame)
        {
            _Visible = !_Visible;
        }
    }

    private void EnsureStyles()
    {
        if (_Panel != null)
        {
            return;
        }

        _Panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 12, 12) };
        _TitleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 18 };
        _HeadStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };
        _ItemStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
    }

    private void OnGUI()
    {
        if (!_Visible || !HasContent())
        {
            return;
        }

        EnsureStyles();

        // Anchored to the right edge, out of the way: the Rules panel owns the top-left
        // and the middle belongs to the ship. Narrow on purpose -- the text wraps into a
        // tall thin column instead of a wide banner across the play area.
        float width = Mathf.Max(160.0f, _PanelWidth);
        float x = Screen.width - width - _ScreenMargin;

        GUILayout.BeginArea(new Rect(x, _ScreenMargin, width, Screen.height - _ScreenMargin * 2.0f));
        GUILayout.BeginVertical(_Panel);

        if (!string.IsNullOrWhiteSpace(_Title))
        {
            GUILayout.Label(_Title, _TitleStyle);
            GUILayout.Space(6.0f);
        }

        if (!string.IsNullOrWhiteSpace(_Objective))
        {
            GUILayout.Label("Objectif", _HeadStyle);
            GUILayout.Label(_Objective.Trim(), _ItemStyle);
        }

        if (_Bullets.Length > 0)
        {
            GUILayout.Space(8.0f);
            GUILayout.Label("Nouveau dans ce niveau", _HeadStyle);

            for (int i = 0; i < _Bullets.Length; i++)
            {
                GUILayout.Label("- " + _Bullets[i].Trim(), _ItemStyle);
            }
        }

        GUILayout.Space(10.0f);
        GUILayout.Label("<i>[" + _ToggleKey + "] pour afficher / masquer</i>", _ItemStyle);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
