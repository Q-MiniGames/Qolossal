using UnityEngine;

// Temporary motion study: one painted sprite with restrained regional deformation.
// It does not replace a layered character rig or authored animation frames.
public sealed class QoriRopeMotion : MonoBehaviour
{
    private readonly QoriCloakMotion ropeCloak = new QoriCloakMotion();
    private Mesh mesh;
    private Material material;
    private MeshRenderer drawing;
    private Vector3[] rest, vertices;
    private Vector2[] pixels;
    private Color[] colors;
    private Vector2 neckPoint;
    private float drift, driftVelocity, lift, liftVelocity;
    public bool Ready => drawing != null;

    public void Initialize(Sprite sprite, SpriteRenderer source)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (sprite == null || shader == null || sprite.packed) return;
        const int cells = 40;
        int count = (cells + 1) * (cells + 1);
        rest = new Vector3[count]; vertices = new Vector3[count];
        pixels = new Vector2[count]; colors = new Color[count];
        Vector2[] uv = new Vector2[count];
        int[] triangles = new int[cells * cells * 6];
        Rect rect = sprite.rect;

        neckPoint = new Vector2((rect.width * .58f - sprite.pivot.x) / sprite.pixelsPerUnit,
            (rect.height * .63f - sprite.pivot.y) / sprite.pixelsPerUnit);
        for (int y = 0; y <= cells; y++)
        for (int x = 0; x <= cells; x++)
        {
            int i = y * (cells + 1) + x;
            float px = rect.width * x / cells, py = rect.height * y / cells;
            rest[i] = new Vector3((px - sprite.pivot.x) / sprite.pixelsPerUnit,
                (py - sprite.pivot.y) / sprite.pixelsPerUnit);
            pixels[i] = new Vector2(px / rect.width, 1f - py / rect.height);
            uv[i] = new Vector2((rect.x + px) / sprite.texture.width,
                (rect.y + py) / sprite.texture.height);
            colors[i] = Color.white;
            if (x == cells || y == cells) continue;
            int k = (y * cells + x) * 6;
            triangles[k] = i; triangles[k+1] = i+cells+1; triangles[k+2] = i+1;
            triangles[k+3] = i+1; triangles[k+4] = i+cells+1; triangles[k+5] = i+cells+2;
        }
        mesh = new Mesh { name = "Qori continuous rope study" };
        mesh.vertices = rest; mesh.uv = uv; mesh.triangles = triangles; mesh.colors = colors;
        mesh.MarkDynamic();
        GameObject display = new GameObject("Continuous Rope Artwork");
        display.transform.SetParent(transform, false);
        display.AddComponent<MeshFilter>().sharedMesh = mesh;
        drawing = display.AddComponent<MeshRenderer>();
        material = new Material(shader) { mainTexture = sprite.texture };
        drawing.sharedMaterial = material;
        drawing.sortingLayerID = source.sortingLayerID;
        drawing.sortingOrder = source.sortingOrder;
        drawing.enabled = false;
    }

    private static float Ramp(float a, float b, float v) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a,b,v));

    public void Show(bool active, Vector2 velocity, bool flipped, Color tint, bool grounded, bool bodyWithoutCloak = false)
    {
        if (!Ready) return;
        drawing.enabled = active;
        if (!active) { drift = driftVelocity = lift = liftVelocity = 0f; ropeCloak.Reset(); return; }
        ropeCloak.Step(grounded ? Vector2.zero : velocity, Time.deltaTime);
        float direction = flipped ? -1f : 1f;
        float target = grounded ? 0f : Mathf.Clamp(velocity.x * direction / 9f, -1f, 1f);
        drift = Mathf.SmoothDamp(drift, target, ref driftVelocity, .22f, Mathf.Infinity, Time.deltaTime);
        lift = Mathf.SmoothDamp(lift, grounded ? 0f : Mathf.Clamp(velocity.y / 12f,-1f,1f),
            ref liftVelocity, .16f, Mathf.Infinity, Time.deltaTime);
        for (int i = 0; i < rest.Length; i++)
        {
            Vector2 p = pixels[i];
            // Masks authored for the existing Rope Rest artwork. Keep face and raised arm rigid.
            float left = 1f - Ramp(.47f,.61f,p.x);
            float ears = left * Ramp(.13f,.22f,p.y) * (1f-Ramp(.37f,.43f,p.y));
            float cloak = left * Ramp(.40f,.53f,p.y) * (1f-Ramp(.78f,.86f,p.y));
            if (bodyWithoutCloak) cloak = 0f;
            float legs = Ramp(.69f,.93f,p.y) * Ramp(.48f,.61f,p.x) * (1f-Ramp(.77f,.87f,p.x));
            Vector3 offset = new Vector3(-drift*legs*.55f,
                ears*drift*.32f + legs*Mathf.Max(0f,lift)*.28f, 0f);
            vertices[i] = rest[i] + offset + (bodyWithoutCloak ? Vector3.zero : ropeCloak.Offset(p, flipped, true));
            vertices[i].x *= direction;
            colors[i] = tint;
        }
        mesh.vertices = vertices; mesh.colors = colors; mesh.RecalculateBounds();
    }

    public void ShowWalking(bool active, float trailing, float stepWave, bool flipped, Color tint, float verticalTrail = 0f, float airAmount = 0f, QoriCloakMotion cloakMotion = null, bool bodyWithoutCloak = false)
    {
        if (!Ready) return;
        drawing.enabled = active;
        if (!active) return;
        for (int i = 0; i < rest.Length; i++)
        {
            Vector2 p = pixels[i];
            float left = 1f - Ramp(.40f, .51f, p.x);
            float ears = left * (1f - Ramp(.30f, .39f, p.y));
            float cloak = left * Ramp(.40f, .54f, p.y) * (1f - Ramp(.76f, .84f, p.y));
            if (bodyWithoutCloak) cloak = 0f;
            // Preserve the hands, weapon and planted feet; rotate the head around the neck.
            Vector3 offset = new Vector3(-cloak * trailing * .22f,
                ears * (trailing * .26f + stepWave * .07f - verticalTrail * .25f)
                + cloak * (stepWave * .08f - verticalTrail * .40f), 0f);
            if (cloakMotion != null)
            {
                offset.x = 0f;
                offset.y -= cloak * (stepWave * .08f - verticalTrail * .40f);
                offset += cloakMotion.Offset(p, flipped, false);
            }
            Vector3 point = rest[i] + offset;
            float headWeight = 1f - Ramp(.32f, .40f, p.y);
            float headAngle = (-verticalTrail * .055f - trailing * .025f + stepWave * .01f) * headWeight;
            Vector2 relative = (Vector2)point - neckPoint;
            float c = Mathf.Cos(headAngle), sn = Mathf.Sin(headAngle);
            point.x = neckPoint.x + relative.x * c - relative.y * sn;
            point.y = neckPoint.y + relative.x * sn + relative.y * c;
            // Extra ear-tip follow-through, independent of the rigid face rotation.
            point.y += ears * (-verticalTrail * .18f + trailing * .10f);
            // Bend the lower legs progressively during ascent; never displace grounded feet.
            float lowerLeg = Ramp(.69f, .90f, p.y) * Ramp(.37f, .46f, p.x)
                * (1f - Ramp(.65f, .74f, p.x));
            float tuck = Mathf.Max(0f, verticalTrail) * airAmount * lowerLeg;
            point.x -= tuck * .36f;
            point.y += tuck * .52f;
            vertices[i] = point;
            vertices[i].x *= flipped ? -1f : 1f;
            colors[i] = tint;
        }
        mesh.vertices = vertices; mesh.colors = colors; mesh.RecalculateBounds();
    }

    private void OnDisable() { if (drawing != null) drawing.enabled = false; }
    private void OnDestroy()
    {
        if (drawing != null) Destroy(drawing.gameObject);
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}

