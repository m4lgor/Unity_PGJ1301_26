using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Procedural parallax starfield.
///
/// Three layers of stars are generated as meshes at start-up (no texture, no prefab,
/// nothing to import) and each layer follows the camera by a different fraction:
/// a layer that follows the camera exactly reads as infinitely far away, a layer that
/// stays put reads as close. That difference is the whole effect -- with an orthographic
/// camera there is no perspective, so parallax is the only depth cue available.
///
/// The play area is bounded (the arena is 20x20), so one static field is enough and
/// there is no wrapping to do.
/// </summary>
public class SpaceBackground : MonoBehaviour
{
    [Header("Material (UQAT/Starfield)")]
    [SerializeField] private Material _StarMaterial;

    [Header("Field")]
    [Tooltip("Same field for every run when the seed is unchanged.")]
    [SerializeField] private int _Seed = 20263;
    [Tooltip("Width/height covered by the stars, in world units. Must comfortably exceed the arena.")]
    [SerializeField] private float _FieldSize = 70.0f;

    [Header("Star counts (far to near)")]
    [SerializeField] private int _StarsFar = 520;
    [SerializeField] private int _StarsMid = 260;
    [SerializeField] private int _StarsNear = 90;

    [Header("Parallax: 1 = follows the camera (infinitely far), 0 = static (close)")]
    [Range(0.0f, 1.0f)] [SerializeField] private float _ParallaxFar = 0.93f;
    [Range(0.0f, 1.0f)] [SerializeField] private float _ParallaxMid = 0.82f;
    [Range(0.0f, 1.0f)] [SerializeField] private float _ParallaxNear = 0.64f;

    [Header("Star size, in world units")]
    [SerializeField] private float _SizeFar = 0.030f;
    [SerializeField] private float _SizeMid = 0.052f;
    [SerializeField] private float _SizeNear = 0.085f;

    [Header("Nebula (UQAT/Nebula material)")]
    [SerializeField] private Material _NebulaMaterial;
    [SerializeField] private int _NebulaCount = 3;
    [Tooltip("Resolution of the generated noise texture. 128 is plenty for something this blurry.")]
    [SerializeField] private int _NebulaTextureSize = 128;
    [Tooltip("How far from the centre clouds are scattered. The far layer follows the camera " +
             "at ~0.93, so this is very nearly a screen-space radius: keep it near the visible " +
             "half-height (ortho size 7) or the clouds sit off-screen.")]
    [SerializeField] private float _NebulaSpread = 7.0f;
    [SerializeField] private float _NebulaMinSize = 16.0f;
    [SerializeField] private float _NebulaMaxSize = 30.0f;
    [Tooltip("Keep this low. Nebulae are atmosphere, they must never fight the ship for attention.")]
    [Range(0.0f, 1.0f)] [SerializeField] private float _NebulaIntensity = 0.18f;

    // Lists, not arrays: the three star layers are fixed, but every planet adds its own
    // layer so it can sit at its own depth.
    private readonly System.Collections.Generic.List<Transform> _Layers = new System.Collections.Generic.List<Transform>();
    private readonly System.Collections.Generic.List<float> _ParallaxPerLayer = new System.Collections.Generic.List<float>();
    private readonly System.Collections.Generic.List<float> _DepthPerLayer = new System.Collections.Generic.List<float>();
    private Mesh[] _Meshes;
    private Camera _Camera;

    [Header("Planets (URP Lit material)")]
    [SerializeField] private Material _PlanetMaterial;
    [SerializeField] private int _PlanetCount = 2;
    [SerializeField] private float _PlanetMinDiameter = 1.8f;
    [SerializeField] private float _PlanetMaxDiameter = 4.0f;
    [Tooltip("Scales the planet and ring tints. Low values read as 'lit by a very distant sun' " +
             "and let the planets sit behind the action instead of competing with it.")]
    [Range(0.0f, 1.0f)] [SerializeField] private float _PlanetBrightness = 0.45f;
    [Tooltip("Horizontal spread radius. Same screen-space logic as the nebula spread.")]
    [SerializeField] private float _PlanetSpread = 11.0f;
    [Tooltip("Vertical spread radius. Smaller than the horizontal one because the view is " +
             "much wider than it is tall (ortho size 7 vs ~12.4 half-width).")]
    [SerializeField] private float _PlanetSpreadY = 5.0f;
    [Tooltip("Parallax of the NEAREST planet (moves most).")]
    [Range(0.0f, 1.0f)] [SerializeField] private float _PlanetParallaxNear = 0.72f;
    [Tooltip("Parallax of the FURTHEST planet (moves least, so reads as distant).")]
    [Range(0.0f, 1.0f)] [SerializeField] private float _PlanetParallaxFar = 0.91f;
    [Range(0.0f, 1.0f)] [SerializeField] private float _PlanetRingChance = 0.5f;

    private Mesh _NebulaMesh;
    private Texture2D _NebulaTexture;
    private Material _NebulaInstance;

    private readonly System.Collections.Generic.List<Material> _RuntimeMaterials = new System.Collections.Generic.List<Material>();
    private readonly System.Collections.Generic.List<Mesh> _RuntimeMeshes = new System.Collections.Generic.List<Mesh>();

    private void Start()
    {
        _Camera = Camera.main;
        Build();
    }

    private void Build()
    {
        if (!_StarMaterial)
        {
            Debug.LogWarning("SpaceBackground - no star material assigned, no background will be drawn.", this);
            return;
        }

        // Deterministic: the same seed always gives the same sky.
        System.Random rng = new System.Random(_Seed);

        int[] counts = { _StarsFar, _StarsMid, _StarsNear };
        float[] sizes = { _SizeFar, _SizeMid, _SizeNear };
        float[] dimming = { 0.55f, 0.80f, 1.00f }; // far stars are fainter
        float[] starParallax = { _ParallaxFar, _ParallaxMid, _ParallaxNear };
        float[] starDepth = { 30.0f, 22.0f, 16.0f }; // world Z, all behind the play plane at Z = 0

        _Meshes = new Mesh[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject go = new GameObject("StarLayer_" + i);
            go.transform.SetParent(transform, false);

            _Meshes[i] = BuildStarMesh(rng, Mathf.Max(0, counts[i]), sizes[i], dimming[i]);

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = _Meshes[i];

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _StarMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            _Layers.Add(go.transform);
            _ParallaxPerLayer.Add(starParallax[i]);
            _DepthPerLayer.Add(starDepth[i]);
        }

        // Parented to the far layer, so the clouds inherit its parallax for free.
        BuildNebula(rng, _Layers[0]);

        BuildPlanets(rng);
    }

    /// <summary>
    /// Every planet gets its OWN layer, so each one sits at its own depth.
    ///
    /// Size, parallax and brightness are all derived from a single "farness" value t.
    /// That link is the point: two planets of different sizes drifting at the same rate
    /// just read as two different planets, whereas a smaller/dimmer one that also moves
    /// less reads as genuinely further away.
    /// </summary>
    private void BuildPlanets(System.Random rng)
    {
        if (!_PlanetMaterial || _PlanetCount <= 0)
        {
            return;
        }

        for (int i = 0; i < _PlanetCount; i++)
        {
            // Stratified: one planet per depth band, jittered inside it. With only two
            // planets pure randomness would often put both at the same distance.
            float t = (i + (float)rng.NextDouble()) / Mathf.Max(1, _PlanetCount); // 0 = near, 1 = far

            float diameter = Mathf.Lerp(_PlanetMaxDiameter, _PlanetMinDiameter, t);
            float parallax = Mathf.Lerp(_PlanetParallaxNear, _PlanetParallaxFar, t);
            float depth = Mathf.Lerp(18.0f, 28.0f, t);
            float brightness = Mathf.Lerp(1.0f, 0.65f, t); // distance dims it too

            // Stratified by ANGLE too, not just depth: each planet owns one sector of the
            // circle and is jittered inside it. Uniform random placement puts two planets
            // on the same side of the screen far too often.
            float sector = (i + 0.2f + (float)rng.NextDouble() * 0.6f) / _PlanetCount;
            float angle = sector * Mathf.PI * 2.0f;

            // Elliptical, not circular: the view is about twice as wide as it is tall, so a
            // circular spread wastes the horizontal room and throws planets off the top.
            float radius = Mathf.Lerp(0.55f, 1.0f, (float)rng.NextDouble());
            float x = Mathf.Cos(angle) * radius * _PlanetSpread;
            float y = Mathf.Sin(angle) * radius * _PlanetSpreadY;

            GameObject holder = new GameObject("PlanetLayer_" + i);
            holder.transform.SetParent(transform, false);
            _Layers.Add(holder.transform);
            _ParallaxPerLayer.Add(parallax);
            _DepthPerLayer.Add(depth);

            Transform parent = holder.transform;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Planet_" + i;

            // CreatePrimitive also gives us a SphereCollider. Background decoration must
            // never take part in physics: students raycast for the Goal in Level03, and a
            // stray collider out here would silently break their queries.
            Collider collider = go.GetComponent<Collider>();
            if (collider)
            {
                Destroy(collider);
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, 0.0f);
            go.transform.localScale = Vector3.one * diameter;

            Material material = new Material(_PlanetMaterial);
            material.hideFlags = HideFlags.DontSave;
            material.SetColor("_BaseColor", Dim(PickPlanetTint(rng), brightness));
            _RuntimeMaterials.Add(material);

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (rng.NextDouble() < _PlanetRingChance)
            {
                BuildRing(go.transform, rng);
            }
        }
    }

    /// <summary>
    /// A flat annulus, tilted so the orthographic camera sees it as an ellipse rather
    /// than a halo. It is opaque and shares the planet's queue, so normal depth sorting
    /// makes the near half of the ring pass in front of the planet by itself.
    /// </summary>
    private void BuildRing(Transform planet, System.Random rng)
    {
        const int segments = 64;
        const float inner = 0.72f;   // local units: the sphere's own radius is 0.5
        const float outer = 1.10f;

        Vector3[] vertices = new Vector3[segments * 2];
        Vector3[] normals = new Vector3[segments * 2];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2.0f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            vertices[i * 2 + 0] = new Vector3(cos * inner, sin * inner, 0.0f);
            vertices[i * 2 + 1] = new Vector3(cos * outer, sin * outer, 0.0f);

            // Facing the camera, which looks along +Z.
            normals[i * 2 + 0] = new Vector3(0.0f, 0.0f, -1.0f);
            normals[i * 2 + 1] = new Vector3(0.0f, 0.0f, -1.0f);

            int a = i * 2;
            int b = i * 2 + 1;
            int c = (i * 2 + 2) % (segments * 2);
            int d = (i * 2 + 3) % (segments * 2);

            int t = i * 6;
            triangles[t + 0] = a;
            triangles[t + 1] = c;
            triangles[t + 2] = b;
            triangles[t + 3] = b;
            triangles[t + 4] = c;
            triangles[t + 5] = d;
        }

        Mesh mesh = new Mesh();
        mesh.name = "PlanetRing";
        mesh.hideFlags = HideFlags.DontSave;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        _RuntimeMeshes.Add(mesh);

        GameObject go = new GameObject("Ring");
        go.transform.SetParent(planet, false);
        go.transform.localPosition = Vector3.zero;

        // Tilt so it reads as an ellipse; a little Z spin so rings are not all aligned.
        float tilt = Mathf.Lerp(62.0f, 78.0f, (float)rng.NextDouble());
        float spin = (float)rng.NextDouble() * 360.0f;
        go.transform.localRotation = Quaternion.Euler(tilt, 0.0f, spin);

        Material material = new Material(_PlanetMaterial);
        material.hideFlags = HideFlags.DontSave;
        material.SetColor("_BaseColor", Dim(PickRingTint(rng)));
        material.SetFloat("_Cull", 0.0f); // visible from both sides
        _RuntimeMaterials.Add(material);

        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    /// <summary>
    /// Scales rgb only. Alpha is left alone so the material stays opaque -- the planets
    /// recede by being dark, not by being see-through.
    /// </summary>
    private Color Dim(Color c, float extra = 1.0f)
    {
        float k = _PlanetBrightness * extra;
        return new Color(c.r * k, c.g * k, c.b * k, c.a);
    }

    private Color PickPlanetTint(System.Random rng)
    {
        double roll = rng.NextDouble();

        if (roll < 0.30)
        {
            return new Color(0.62f, 0.42f, 0.30f); // rust
        }

        if (roll < 0.55)
        {
            return new Color(0.30f, 0.44f, 0.62f); // ice blue
        }

        if (roll < 0.80)
        {
            return new Color(0.58f, 0.54f, 0.42f); // sand
        }

        return new Color(0.42f, 0.34f, 0.52f);     // violet grey
    }

    private Color PickRingTint(System.Random rng)
    {
        float g = Mathf.Lerp(0.45f, 0.70f, (float)rng.NextDouble());
        return new Color(g, g * 0.94f, g * 0.82f);
    }

    private void BuildNebula(System.Random rng, Transform parent)
    {
        if (!_NebulaMaterial || _NebulaCount <= 0)
        {
            return;
        }

        _NebulaTexture = BuildNebulaTexture(rng, Mathf.Max(16, _NebulaTextureSize));

        // The texture only exists at runtime, so the material has to as well.
        _NebulaInstance = new Material(_NebulaMaterial);
        _NebulaInstance.hideFlags = HideFlags.DontSave;
        _NebulaInstance.mainTexture = _NebulaTexture;
        _NebulaInstance.SetFloat("_Intensity", _NebulaIntensity);

        _NebulaMesh = BuildNebulaMesh(rng, _NebulaCount);

        GameObject go = new GameObject("Nebula");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0.0f, 0.0f, 2.0f); // just behind the far stars

        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = _NebulaMesh;

        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _NebulaInstance;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    /// <summary>
    /// Fractal Perlin noise multiplied by a radial falloff. The falloff is the important
    /// half: without it the cloud stops abruptly at the quad border and reads as a square.
    /// </summary>
    private Texture2D BuildNebulaTexture(System.Random rng, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.DontSave;

        float offsetX = (float)rng.NextDouble() * 1000.0f;
        float offsetY = (float)rng.NextDouble() * 1000.0f;

        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);

                // fBm: a few octaves of Perlin, each finer and fainter than the last.
                float noise = 0.0f;
                float amplitude = 0.5f;
                float frequency = 3.0f;

                for (int octave = 0; octave < 5; octave++)
                {
                    noise += amplitude * Mathf.PerlinNoise(offsetX + u * frequency, offsetY + v * frequency);
                    amplitude *= 0.5f;
                    frequency *= 2.0f;
                }

                // Radial falloff, squared for a softer edge.
                float dx = u - 0.5f;
                float dy = v - 0.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) * 2.0f;
                float falloff = Mathf.Clamp01(1.0f - distance);
                falloff *= falloff;

                float value = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(noise * falloff));
                byte b = (byte)(value * 255.0f);

                pixels[y * size + x] = new Color32(b, b, b, b);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(true);

        return texture;
    }

    private Mesh BuildNebulaMesh(System.Random rng, int count)
    {
        Vector3[] vertices = new Vector3[count * 4];
        Color[] colors = new Color[count * 4];
        Vector2[] uvs = new Vector2[count * 4];
        int[] triangles = new int[count * 6];

        // Deliberately NOT derived from _FieldSize: that one is sized for the star field,
        // which has to cover the whole arena plus camera travel. Clouds need to stay near
        // the middle of the view instead.
        float half = _NebulaSpread;

        for (int n = 0; n < count; n++)
        {
            float cx = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
            float cy = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
            float size = Mathf.Lerp(_NebulaMinSize, _NebulaMaxSize, (float)rng.NextDouble()) * 0.5f;
            float angle = (float)rng.NextDouble() * Mathf.PI * 2.0f;

            // Rotation is baked into the vertices so every cloud looks different
            // even though they all share one noise texture.
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            Color tint = PickNebulaTint(rng);
            float strength = Mathf.Lerp(0.55f, 1.0f, (float)rng.NextDouble());
            Color c = new Color(tint.r, tint.g, tint.b, strength);

            int v = n * 4;
            Vector2[] corners =
            {
                new Vector2(-size, -size),
                new Vector2(+size, -size),
                new Vector2(+size, +size),
                new Vector2(-size, +size),
            };

            Vector2[] cornerUVs =
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
            };

            for (int k = 0; k < 4; k++)
            {
                float rx = corners[k].x * cos - corners[k].y * sin;
                float ry = corners[k].x * sin + corners[k].y * cos;

                vertices[v + k] = new Vector3(cx + rx, cy + ry, 0.0f);
                colors[v + k] = c;
                uvs[v + k] = cornerUVs[k];
            }

            int t = n * 6;
            triangles[t + 0] = v + 0;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 0;
            triangles[t + 4] = v + 3;
            triangles[t + 5] = v + 2;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Nebula";
        mesh.hideFlags = HideFlags.DontSave;
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }

    private Color PickNebulaTint(System.Random rng)
    {
        double roll = rng.NextDouble();

        if (roll < 0.40)
        {
            return new Color(0.45f, 0.24f, 0.72f); // violet
        }

        if (roll < 0.75)
        {
            return new Color(0.18f, 0.36f, 0.78f); // deep blue
        }

        return new Color(0.14f, 0.52f, 0.50f);     // teal
    }

    private Mesh BuildStarMesh(System.Random rng, int count, float size, float dimming)
    {
        Vector3[] vertices = new Vector3[count * 4];
        Color[] colors = new Color[count * 4];
        Vector2[] uvs = new Vector2[count * 4];
        int[] triangles = new int[count * 6];

        float half = _FieldSize * 0.5f;

        for (int s = 0; s < count; s++)
        {
            float x = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
            float y = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
            float r = size * (0.5f + (float)rng.NextDouble());

            // Brightness goes in alpha; the shader multiplies rgb by it.
            float brightness = dimming * Mathf.Lerp(0.22f, 1.0f, (float)rng.NextDouble());
            Color tint = PickStarTint(rng);
            Color c = new Color(tint.r, tint.g, tint.b, brightness);

            // Per-star twinkle phase and speed, so the sky never pulses in unison.
            Vector2 twinkle = new Vector2(
                (float)rng.NextDouble() * Mathf.PI * 2.0f,
                Mathf.Lerp(0.4f, 2.2f, (float)rng.NextDouble()));

            int v = s * 4;

            // The camera never rotates (LockedCameraOrtho locks it to +Z), so plain
            // axis-aligned quads are enough -- no billboarding needed.
            vertices[v + 0] = new Vector3(x - r, y - r, 0.0f);
            vertices[v + 1] = new Vector3(x + r, y - r, 0.0f);
            vertices[v + 2] = new Vector3(x + r, y + r, 0.0f);
            vertices[v + 3] = new Vector3(x - r, y + r, 0.0f);

            for (int k = 0; k < 4; k++)
            {
                colors[v + k] = c;
                uvs[v + k] = twinkle;
            }

            int t = s * 6;
            triangles[t + 0] = v + 0;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 0;
            triangles[t + 4] = v + 3;
            triangles[t + 5] = v + 2;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Starfield";
        mesh.hideFlags = HideFlags.DontSave; // generated at runtime, never an asset

        if (vertices.Length > 65000)
        {
            mesh.indexFormat = IndexFormat.UInt32;
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }

    private Color PickStarTint(System.Random rng)
    {
        double roll = rng.NextDouble();

        if (roll < 0.70)
        {
            return new Color(1.00f, 1.00f, 1.00f); // white
        }

        if (roll < 0.90)
        {
            return new Color(0.72f, 0.83f, 1.00f); // blue-white
        }

        return new Color(1.00f, 0.86f, 0.70f);     // warm
    }

    private void LateUpdate()
    {
        if (_Layers.Count == 0)
        {
            return;
        }

        if (!_Camera)
        {
            _Camera = Camera.main;
            if (!_Camera)
            {
                return;
            }
        }

        Vector3 cam = _Camera.transform.position;

        for (int i = 0; i < _Layers.Count; i++)
        {
            // Follow a fraction of the camera's travel: the closer that fraction is to 1,
            // the less the layer appears to move, so the further away it reads.
            _Layers[i].position = new Vector3(cam.x * _ParallaxPerLayer[i], cam.y * _ParallaxPerLayer[i], _DepthPerLayer[i]);
        }
    }

    private void OnDestroy()
    {
        if (_Meshes == null)
        {
            return;
        }

        for (int i = 0; i < _Meshes.Length; i++)
        {
            if (_Meshes[i])
            {
                Destroy(_Meshes[i]);
            }
        }

        // The nebula texture and material are created at runtime, so we own them.
        if (_NebulaMesh) Destroy(_NebulaMesh);
        if (_NebulaTexture) Destroy(_NebulaTexture);
        if (_NebulaInstance) Destroy(_NebulaInstance);

        // Planet + ring material instances and ring meshes.
        for (int i = 0; i < _RuntimeMaterials.Count; i++)
        {
            if (_RuntimeMaterials[i]) Destroy(_RuntimeMaterials[i]);
        }

        for (int i = 0; i < _RuntimeMeshes.Count; i++)
        {
            if (_RuntimeMeshes[i]) Destroy(_RuntimeMeshes[i]);
        }
    }
}
