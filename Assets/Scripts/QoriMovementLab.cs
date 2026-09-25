using UnityEngine;
using UnityEngine.InputSystem;

// Only the dedicated lab scene contains this bootstrap. Geometry is deliberately plain.
public sealed class QoriMovementLab : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject anchorPrefab;
    [SerializeField] private bool showControls = true;
    private PlayerMovement player;
    private Camera labCamera;
    private Sprite tile;
    private Material tileMaterial;

    private void Awake()
    {
        if (playerPrefab == null || anchorPrefab == null)
        {
            Debug.LogError("Qori movement lab is missing its approved prefab references.", this);
            enabled = false;
            return;
        }
        int ground = LayerMask.NameToLayer("Ground");
        if (ground < 0)
        {
            Debug.LogError("Qori movement lab requires the Ground layer.", this);
            enabled = false;
            return;
        }
        Texture2D white = Texture2D.whiteTexture;
        tile = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(.5f, .5f), white.width);
        tile.name = "Lab runtime white tile";
        tileMaterial = new Material(Shader.Find("Sprites/Default"));
        Color main = new Color(.29f, .40f, .36f);
        Color elevated = new Color(.37f, .48f, .42f);
        Surface("Flat start and braking lane", -16, -2, 0, 2, main, ground);
        Surface("Short swing landing", 2, 8, 1, 1, main, ground);
        Surface("Approach step", 8, 10, 2, 1, elevated, ground);
        Surface("High ledge for long swing", 10, 13, 3, 1, elevated, ground);
        Surface("Long swing landing", 20, 25, 2, 1, main, ground);
        Surface("Return step", 25, 27, 3, 1, elevated, ground);
        Surface("Upper landing and reversal lane", 27, 33, 4, 1, elevated, ground);
        Surface("Lower recovery floor", -2, 33, -4, 2, new Color(.22f, .31f, .29f), ground);
        Surface("Left wall", -16.5f, -16, 5, 7, main, ground);
        Surface("Right wall", 33, 33.5f, 9, 15, main, ground);
        Anchor("Short swing anchor", -.5f, 3);
        Anchor("Low platform anchor", 6, 4);
        Anchor("High ledge anchor", 11.8f, 6);
        Anchor("Long swing first anchor", 15.2f, 5.2f);
        Anchor("Long swing second anchor", 19, 4.5f);
        Anchor("Landing anchor", 24, 5);
        Anchor("Upper anchor", 29, 7);
        GameObject character = Instantiate(playerPrefab, new Vector3(-10, 1, 0), Quaternion.identity);
        character.name = "Qori - movement lab";
        player = character.GetComponent<PlayerMovement>();
        GameObject cameraObject = new GameObject("Movement lab camera");
        cameraObject.tag = "MainCamera";
        labCamera = cameraObject.AddComponent<Camera>();
        labCamera.orthographic = true;
        labCamera.orthographicSize = 5.2f;
        labCamera.clearFlags = CameraClearFlags.SolidColor;
        labCamera.backgroundColor = new Color(.105f, .15f, .14f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(-10, 2, -10);
        cameraObject.AddComponent<CameraFollow>().Configure(character.transform);
    }

    private void Surface(string label, float left, float right, float top, float depth, Color color, int layer)
    {
        GameObject surface = new GameObject(label);
        surface.transform.SetParent(transform, false);
        surface.layer = layer;
        surface.transform.position = new Vector3((left + right) * .5f, top - depth * .5f, 0);
        surface.transform.localScale = new Vector3(right - left, depth, 1);
        SpriteRenderer renderer = surface.AddComponent<SpriteRenderer>();
        renderer.sprite = tile;
        renderer.sharedMaterial = tileMaterial;
        renderer.color = color;
        renderer.sortingOrder = -5;
        surface.AddComponent<BoxCollider2D>().size = Vector2.one;
    }

    private void Anchor(string label, float x, float y)
    {
        GameObject anchor = Instantiate(anchorPrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
        anchor.name = label;
    }

    private void Update()
    {
        if (player != null && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            player.Respawn();
    }

    private void OnGUI()
    {
        if (!showControls || player == null) return;
        float width = Mathf.Min(650f, Screen.width - 24f);
        GUIStyle text = new GUIStyle(GUI.skin.label) { wordWrap = true };
        string controls = "A/D or arrows: move · Hold Shift: run · Space: jump · F / right mouse: thread · W/S: reel/extend · R: reset\n"
            + "Xbox: stick: move · Hold L3: run · A: jump · RT: thread · LT: reel · D-pad down: extend\n"
            + "Wall: auto-slide; jump to climb; hold away + jump to push off; steer away to drop.\n"
            + "Ledge: catch automatically; Jump / Up climbs; Down / away drops.\n"
            + "Flat lane → short gap → high ledge → long swing → upper landing";
        float height = text.CalcHeight(new GUIContent(controls), width - 24f) + 38f;
        float y = Mathf.Max(110f, Screen.height - height - 12f);
        GUI.Box(new Rect(12, y, width, height), "Qori movement lab");
        GUI.Label(new Rect(24, y + 26f, width - 24f, height - 30f), controls, text);
    }

    private void OnDestroy()
    {
        if (tile != null) Destroy(tile);
        if (tileMaterial != null) Destroy(tileMaterial);
    }
}
