using System.Collections.Generic;
using UnityEngine;

public class HudConsole : MonoBehaviour
{
    [SerializeField] bool _Enable = true;

    private static Queue<string> _Messages = new Queue<string>();

    private GUIStyle _BackgroundStyle;

    void Awake()
    {
        // Create a black background style with opacity
        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.3f)); // black with alpha
        bgTex.Apply();

        _BackgroundStyle = new GUIStyle();
        _BackgroundStyle.normal.background = bgTex;
        _BackgroundStyle.padding = new RectOffset(5, 5, 5, 5);
    }

    // Call this from anywhere to add a message
    public static void Log(string msg)
    {
        if (_Messages.Count >= 10)
            _Messages.Dequeue();

        _Messages.Enqueue(msg);
    }

    void OnGUI()
    {
        if (!_Enable)
        {
            return;
        }

        // Top left corner
        GUILayout.BeginArea(new Rect(10, 10, 500, 300), _BackgroundStyle);
        GUILayout.BeginVertical();

        foreach (string msg in _Messages)
        {
            GUILayout.Label(msg);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
