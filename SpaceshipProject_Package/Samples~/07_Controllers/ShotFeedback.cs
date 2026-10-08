using UnityEngine;

/// <summary>
/// Not for you to edit. What a shot looks like : a thin line from the weapon to where it ended,
/// and a small flash on the spot it hit. Both vanish by themselves.
///
///     ShotFeedback.Show(from, to, true);
/// </summary>
public static class ShotFeedback
{
    static Material _Material;

    public static void Show(Vector3 from, Vector3 to, bool hit)
    {
        if (_Material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            _Material = new Material(shader);
            _Material.color = new Color(1.0f, 0.9f, 0.3f);
        }

        GameObject tracer = new GameObject("Tracer");
        LineRenderer line = tracer.AddComponent<LineRenderer>();
        line.sharedMaterial = _Material;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startWidth = 0.04f;
        line.endWidth = 0.02f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Object.Destroy(tracer, 0.08f);

        if (hit)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(flash.GetComponent<Collider>());
            flash.transform.position = to;
            flash.transform.localScale = Vector3.one * 0.2f;
            flash.GetComponent<Renderer>().sharedMaterial = _Material;
            Object.Destroy(flash, 0.12f);
        }
    }
}
