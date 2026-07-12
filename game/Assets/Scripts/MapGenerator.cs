using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// MapGenerator builds a grey-box layout for the Abandoned Art School.
/// Use the component context menu in the Inspector to Generate or Clear in edit mode.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    private GameObject mapLayout;
    [SerializeField] private bool generateOnPlay;

    private void Start()
    {
        if (Application.isPlaying && generateOnPlay)
        {
            GenerateNow();
        }
    }

    [ContextMenu("Generate Map Layout")]
    public void GenerateNow()
    {
        ClearGenerated();
        GenerateMap();
        MarkSceneDirtyIfNeeded();
    }

    [ContextMenu("Clear Map Layout")]
    public void ClearGenerated()
    {
        GameObject existing = transform.Find("MapLayout")?.gameObject;
        if (existing != null)
        {
            DestroyObjectSafe(existing);
        }

        mapLayout = null;
        MarkSceneDirtyIfNeeded();
    }

    private void GenerateMap()
    {
        // Create parent container
        mapLayout = new GameObject("MapLayout");
        mapLayout.transform.SetParent(transform, false);
        mapLayout.transform.position = Vector3.zero;

        // Get or create default material (grey) — use HDRP/Lit for HDRP projects
        Material greyMaterial = new Material(Shader.Find("HDRP/Lit"));
        greyMaterial.color = new Color(0.7f, 0.7f, 0.7f, 1f);

        // ===== MAIN HALL =====
        CreateMainHall(greyMaterial);

        // ===== SIDE ROOMS =====
        CreateLeftRoom(greyMaterial);
        CreateRightRoom(greyMaterial);
        CreateBackRoom(greyMaterial);

        // ===== FLOOR LEVELS & RAMP =====
        CreateRamp(greyMaterial);

        // ===== GAMEPLAY PLACEHOLDERS =====
        CreateGalleryFramePlaceholders();
        CreateExitGatePlaceholders();
        CreateHoldingFramePlaceholders();
        CreateInkStationPlaceholders();
    }

    /// <summary>
    /// Main hall: 30×20 floor with 4 perimeter walls (height 3, thickness 1)
    /// </summary>
    private void CreateMainHall(Material mat)
    {
        // Floor: 30×1×20 at y=0
        CreateCube(
            parent: mapLayout,
            name: "MainHall_Floor",
            position: new Vector3(0, -0.5f, 0),
            scale: new Vector3(30, 1, 20),
            material: mat
        );

        // North wall (z = 10)
        CreateCube(
            parent: mapLayout,
            name: "MainHall_WallNorth",
            position: new Vector3(0, 1.5f, 10),
            scale: new Vector3(30, 3, 1),
            material: mat
        );

        // South wall (z = -10)
        CreateCube(
            parent: mapLayout,
            name: "MainHall_WallSouth",
            position: new Vector3(0, 1.5f, -10),
            scale: new Vector3(30, 3, 1),
            material: mat
        );

        // East wall (x = 15)
        CreateCube(
            parent: mapLayout,
            name: "MainHall_WallEast",
            position: new Vector3(15, 1.5f, 0),
            scale: new Vector3(1, 3, 20),
            material: mat
        );

        // West wall (x = -15)
        CreateCube(
            parent: mapLayout,
            name: "MainHall_WallWest",
            position: new Vector3(-15, 1.5f, 0),
            scale: new Vector3(1, 3, 20),
            material: mat
        );
    }

    /// <summary>
    /// Left side room: 10×8 units
    /// </summary>
    private void CreateLeftRoom(Material mat)
    {
        Vector3 roomCenter = new Vector3(-20, -0.5f, 0);

        // Floor
        CreateCube(
            parent: mapLayout,
            name: "LeftRoom_Floor",
            position: roomCenter,
            scale: new Vector3(10, 1, 8),
            material: mat
        );

        // North wall
        CreateCube(
            parent: mapLayout,
            name: "LeftRoom_WallNorth",
            position: new Vector3(-20, 1.5f, 4),
            scale: new Vector3(10, 3, 1),
            material: mat
        );

        // South wall
        CreateCube(
            parent: mapLayout,
            name: "LeftRoom_WallSouth",
            position: new Vector3(-20, 1.5f, -4),
            scale: new Vector3(10, 3, 1),
            material: mat
        );

        // West wall
        CreateCube(
            parent: mapLayout,
            name: "LeftRoom_WallWest",
            position: new Vector3(-25, 1.5f, 0),
            scale: new Vector3(1, 3, 8),
            material: mat
        );

        // East wall (connecting to main hall) - has opening at y=0
        CreateCube(
            parent: mapLayout,
            name: "LeftRoom_WallEast",
            position: new Vector3(-15, 1.5f, 0),
            scale: new Vector3(1, 3, 8),
            material: mat
        );

        // Corridor connecting to main hall (2 unit wide)
        CreateCube(
            parent: mapLayout,
            name: "LeftRoom_Corridor",
            position: new Vector3(-17.5f, -0.5f, 0),
            scale: new Vector3(5, 1, 2),
            material: mat
        );
    }

    /// <summary>
    /// Right side room: 10×8 units
    /// </summary>
    private void CreateRightRoom(Material mat)
    {
        Vector3 roomCenter = new Vector3(20, -0.5f, 0);

        // Floor
        CreateCube(
            parent: mapLayout,
            name: "RightRoom_Floor",
            position: roomCenter,
            scale: new Vector3(10, 1, 8),
            material: mat
        );

        // North wall
        CreateCube(
            parent: mapLayout,
            name: "RightRoom_WallNorth",
            position: new Vector3(20, 1.5f, 4),
            scale: new Vector3(10, 3, 1),
            material: mat
        );

        // South wall
        CreateCube(
            parent: mapLayout,
            name: "RightRoom_WallSouth",
            position: new Vector3(20, 1.5f, -4),
            scale: new Vector3(10, 3, 1),
            material: mat
        );

        // East wall
        CreateCube(
            parent: mapLayout,
            name: "RightRoom_WallEast",
            position: new Vector3(25, 1.5f, 0),
            scale: new Vector3(1, 3, 8),
            material: mat
        );

        // West wall (connecting to main hall)
        CreateCube(
            parent: mapLayout,
            name: "RightRoom_WallWest",
            position: new Vector3(15, 1.5f, 0),
            scale: new Vector3(1, 3, 8),
            material: mat
        );

        // Corridor connecting to main hall
        CreateCube(
            parent: mapLayout,
            name: "RightRoom_Corridor",
            position: new Vector3(17.5f, -0.5f, 0),
            scale: new Vector3(5, 1, 2),
            material: mat
        );
    }

    /// <summary>
    /// Back room: 15×8 units
    /// </summary>
    private void CreateBackRoom(Material mat)
    {
        Vector3 roomCenter = new Vector3(0, -0.5f, -16);

        // Floor
        CreateCube(
            parent: mapLayout,
            name: "BackRoom_Floor",
            position: roomCenter,
            scale: new Vector3(15, 1, 8),
            material: mat
        );

        // North wall (far back)
        CreateCube(
            parent: mapLayout,
            name: "BackRoom_WallNorth",
            position: new Vector3(0, 1.5f, -20),
            scale: new Vector3(15, 3, 1),
            material: mat
        );

        // South wall (connecting to main hall)
        CreateCube(
            parent: mapLayout,
            name: "BackRoom_WallSouth",
            position: new Vector3(0, 1.5f, -12),
            scale: new Vector3(15, 3, 1),
            material: mat
        );

        // East wall
        CreateCube(
            parent: mapLayout,
            name: "BackRoom_WallEast",
            position: new Vector3(7.5f, 1.5f, -16),
            scale: new Vector3(1, 3, 8),
            material: mat
        );

        // West wall
        CreateCube(
            parent: mapLayout,
            name: "BackRoom_WallWest",
            position: new Vector3(-7.5f, 1.5f, -16),
            scale: new Vector3(1, 3, 8),
            material: mat
        );

        // Corridor connecting to main hall
        CreateCube(
            parent: mapLayout,
            name: "BackRoom_Corridor",
            position: new Vector3(0, -0.5f, -13),
            scale: new Vector3(2, 1, 2),
            material: mat
        );
    }

    /// <summary>
    /// Ramp connecting floor level 0 (y=0) to upper level (y=4)
    /// </summary>
    private void CreateRamp(Material mat)
    {
        // Ramp: 15×1×1 angled from y=0 to y=4
        GameObject ramp = CreateCube(
            parent: mapLayout,
            name: "Ramp",
            position: new Vector3(0, 2f, 15),
            scale: new Vector3(3, 1, 15),
            material: mat
        );

        // Rotate to create incline (approximately 15 degrees)
        ramp.transform.Rotate(-15, 0, 0);

        // Upper level platform at y=4
        CreateCube(
            parent: mapLayout,
            name: "UpperLevel_Platform",
            position: new Vector3(0, 3.5f, 22),
            scale: new Vector3(15, 1, 12),
            material: mat
        );

        // Upper level walls
        CreateCube(
            parent: mapLayout,
            name: "UpperLevel_WallNorth",
            position: new Vector3(0, 5.5f, 28),
            scale: new Vector3(15, 3, 1),
            material: mat
        );

        CreateCube(
            parent: mapLayout,
            name: "UpperLevel_WallEast",
            position: new Vector3(7.5f, 5.5f, 22),
            scale: new Vector3(1, 3, 12),
            material: mat
        );

        CreateCube(
            parent: mapLayout,
            name: "UpperLevel_WallWest",
            position: new Vector3(-7.5f, 5.5f, 22),
            scale: new Vector3(1, 3, 12),
            material: mat
        );
    }

    /// <summary>
    /// Create 4 Gallery Frame placeholders (thin quads on walls)
    /// </summary>
    private void CreateGalleryFramePlaceholders()
    {
        // Gallery Frame 1: Left wall, main hall
        CreatePlaceholderQuad(
            parent: mapLayout,
            name: "GalleryFrame_Slot_1",
            position: new Vector3(-14.9f, 1.5f, 5),
            rotation: Quaternion.Euler(0, 90, 0),
            scale: new Vector3(3, 3, 1)
        );

        // Gallery Frame 2: Back wall, main hall
        CreatePlaceholderQuad(
            parent: mapLayout,
            name: "GalleryFrame_Slot_2",
            position: new Vector3(5, 1.5f, -9.9f),
            rotation: Quaternion.identity,
            scale: new Vector3(3, 3, 1)
        );

        // Gallery Frame 3: Right wall, back room
        CreatePlaceholderQuad(
            parent: mapLayout,
            name: "GalleryFrame_Slot_3",
            position: new Vector3(7.4f, 1.5f, -16),
            rotation: Quaternion.Euler(0, 90, 0),
            scale: new Vector3(3, 3, 1)
        );

        // Gallery Frame 4: Upper level
        CreatePlaceholderQuad(
            parent: mapLayout,
            name: "GalleryFrame_Slot_4",
            position: new Vector3(0, 5.4f, 28),
            rotation: Quaternion.identity,
            scale: new Vector3(3, 3, 1)
        );
    }

    /// <summary>
    /// Create 2 Exit Gate placeholders
    /// </summary>
    private void CreateExitGatePlaceholders()
    {
        // Exit Gate 1: Left room exit
        CreatePlaceholderQuad(
            parent: mapLayout,
            name: "ExitGate_Slot_1",
            position: new Vector3(-25, 1.5f, -2),
            rotation: Quaternion.Euler(0, 90, 0),
            scale: new Vector3(2, 3, 1)
        );

        // Exit Gate 2: Right room exit
        CreatePlaceholderQuad(
            parent: mapLayout,
            name: "ExitGate_Slot_2",
            position: new Vector3(25, 1.5f, 2),
            rotation: Quaternion.Euler(0, 90, 0),
            scale: new Vector3(2, 3, 1)
        );
    }

    /// <summary>
    /// Create 4 Holding Frame placeholders (thin quads on walls)
    /// </summary>
    private void CreateHoldingFramePlaceholders()
    {
        // Holding Frame 1: Right wall of main hall
        CreatePlaceholderQuad(
            parent: mapLayout,
            name: "HoldingFrame_Slot_1",
            position: new Vector3(14.9f, 1.5f, -5),
            rotation: Quaternion.Euler(0, 90, 0),
            scale: new Vector3(2.5f, 2.5f, 1)
        );

        // Holding Frame 2: Back wall of back room
        CreatePlaceholderQuad(
            parent: mapLayout,
            name: "HoldingFrame_Slot_2",
            position: new Vector3(-3, 1.5f, -19.9f),
            rotation: Quaternion.identity,
            scale: new Vector3(2.5f, 2.5f, 1)
        );

        // Holding Frame 3: Left room back wall
        CreatePlaceholderQuad(
            parent: mapLayout,
            name: "HoldingFrame_Slot_3",
            position: new Vector3(-25, 1.5f, 3),
            rotation: Quaternion.Euler(0, 90, 0),
            scale: new Vector3(2.5f, 2.5f, 1)
        );

        // Holding Frame 4: Upper level south wall (can be rescued by lower survivors)
        CreatePlaceholderQuad(
            parent: mapLayout,
            name: "HoldingFrame_Slot_4",
            position: new Vector3(3, 5.4f, 16),
            rotation: Quaternion.identity,
            scale: new Vector3(2.5f, 2.5f, 1)
        );
    }

    /// <summary>
    /// Create 3 Ink Station placeholders (small cubes)
    /// </summary>
    private void CreateInkStationPlaceholders()
    {
        Material inkMaterial = new Material(Shader.Find("HDRP/Lit"));
        inkMaterial.color = new Color(0.3f, 0.3f, 0.8f, 1f); // Blue

        // Ink Station 1: Main hall center-left
        CreateCube(
            parent: mapLayout,
            name: "InkStation_Slot_1",
            position: new Vector3(-10, 0.5f, 0),
            scale: new Vector3(1, 1, 1),
            material: inkMaterial
        );

        // Ink Station 2: Right room
        CreateCube(
            parent: mapLayout,
            name: "InkStation_Slot_2",
            position: new Vector3(20, 0.5f, -6),
            scale: new Vector3(1, 1, 1),
            material: inkMaterial
        );

        // Ink Station 3: Back room
        CreateCube(
            parent: mapLayout,
            name: "InkStation_Slot_3",
            position: new Vector3(0, 0.5f, -16),
            scale: new Vector3(1, 1, 1),
            material: inkMaterial
        );
    }

    /// <summary>
    /// Helper: Create a cube with position, scale, and material
    /// </summary>
    private GameObject CreateCube(GameObject parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.parent = parent.transform;
        cube.transform.position = position;
        cube.transform.localScale = scale;

        // Remove the collider for clean geometry (optional — remove this line if you need colliders)
        DestroyObjectSafe(cube.GetComponent<Collider>());

        // Apply material
        cube.GetComponent<Renderer>().material = material;

        return cube;
    }

    /// <summary>
    /// Helper: Create a thin quad placeholder with custom rotation
    /// </summary>
    private GameObject CreatePlaceholderQuad(GameObject parent, string name, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.parent = parent.transform;
        quad.transform.position = position;
        quad.transform.rotation = rotation;
        quad.transform.localScale = scale;

        // Remove collider
        DestroyObjectSafe(quad.GetComponent<Collider>());

        // Apply semi-transparent material for visibility
        Material placeholderMat = new Material(Shader.Find("HDRP/Unlit"));
        placeholderMat.color = new Color(1f, 0.84f, 0f, 0.6f); // Gold with transparency
        quad.GetComponent<Renderer>().material = placeholderMat;

        return quad;
    }

    private void DestroyObjectSafe(Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(obj);
            return;
        }

        DestroyImmediate(obj);
    }

    private void MarkSceneDirtyIfNeeded()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}
