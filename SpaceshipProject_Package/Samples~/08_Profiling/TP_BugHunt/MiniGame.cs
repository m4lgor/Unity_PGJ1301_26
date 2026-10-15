using UnityEngine;

// The score, the coins and a short message, drawn on the screen.
// The fields are static : anything in the scene can add to the score.
public class MiniGame : MonoBehaviour
{
    public static int Score;
    public static int Coins;

    static string _message = "";
    static float _messageUntil;

    public static void Say(string text)
    {
        _message = text;
        _messageUntil = Time.unscaledTime + 3.0f;
    }

    GUIStyle _style;
    Texture2D _background;

    void Awake()
    {
        Score = 0;
        Coins = 0;
        _message = "";
        _messageUntil = 0.0f;
    }

    void OnGUI()
    {
        if (_style == null)
        {
            _background = new Texture2D(1, 1);
            _background.SetPixel(0, 0, new Color(0.0f, 0.0f, 0.0f, 0.6f));
            _background.Apply();
            _style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            _style.normal.textColor = Color.white;
        }

        GUI.DrawTexture(new Rect(8, 8, 280, 88), _background);
        GUI.Label(new Rect(16, 12, 270, 24), "Score  " + Score, _style);
        GUI.Label(new Rect(16, 36, 270, 24), "Coins  " + Coins, _style);
        GUI.Label(new Rect(16, 60, 270, 24), Time.unscaledTime < _messageUntil ? _message : "", _style);

        float y = Screen.height - 30.0f;
        GUI.DrawTexture(new Rect(8, y - 4, Screen.width - 16, 26), _background);
        GUI.Label(new Rect(16, y - 2, Screen.width - 32, 24), "A / D (or the arrows) : run    Space : jump", _style);
    }
}
