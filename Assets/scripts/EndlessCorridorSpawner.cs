using System.Collections.Generic;
using UnityEngine;

public class EndlessCorridorSpawner : MonoBehaviour
{
    [Header("Tracking Target")]
    [Tooltip("Drag Jetpack_Vehicle here.")]
    public Transform playerTransform;

    [Header("Alignment Anchors")]
    [Tooltip("Drag 'LEFT BUILDINGS' from Hierarchy here.")]
    public Transform leftBuildingAnchor;

    [Tooltip("Drag 'RIGHT BUILDINGS' from Hierarchy here.")]
    public Transform rightBuildingAnchor;

    [Header("Rooftop Runway Offset")]
    [Tooltip("Starting Z distance where procedural chunks begin.")]
    public float startOffsetZ = 20f;

    [Header("Road & Sidewalk Geometry")]
    public float chunkLength = 100f;
    public float roadWidth = 5.81f;
    public float sidewalkWidth = 4.2f;

    [Tooltip("Width of foundation slab behind buildings.")]
    public float underBuildingSlabWidth = 8.0f;
    public float footpathHeight = 0.15f;

    [Header("Middle White Dashes")]
    public float dashWidth = 0.15f;
    public float dashLength = 2.4f;
    public float dashGap = 3.5f;

    [Header("Materials")]
    public Material roadMaterial;
    public Material footpathMaterial;
    public Material whiteLineMaterial;

    [Header("Building Configuration")]
    public GameObject[] buildingPrefabs;

    [Tooltip("Distance along Z between consecutive buildings to keep them side-by-side without gaps.")]
    public float buildingSpacingStepZ = 4.5f;

    [Tooltip("Global scale multiplier for spawned buildings.")]
    public float buildingScaleMultiplier = 1.3f;

    public float leftSideYRotation = 0f;
    public float rightSideYRotation = 180f;

    [Header("Target Frames (Obstacles)")]
    [Tooltip("Drag your Target_Frame prefab here.")]
    public GameObject[] obstaclePrefabs;

    [Tooltip("Fixed flight height of all frames above the road.")]
    public float fixedFrameHeightY = 3.8f;

    [Tooltip("Z distance step between consecutive frames.")]
    public float distanceBetweenFrames = 30f;

    [Tooltip("Safe starting distance before the first frame appears.")]
    public float firstFrameStartDistance = 40f;

    [Header("View & Distance Buffer")]
    public int initialChunksOnScreen = 8;
    public float spawnDistanceAhead = 600f;
    public float destroyDistanceBehind = 150f;

    private float spawnZ = 0f;
    private float leftBuildingZ = 0f;
    private float rightBuildingZ = 0f;
    private float nextFrameZ = 0f;
    private int groundLayerIndex;

    private List<GameObject> activeChunks = new List<GameObject>();

    private readonly float[] gridLanesX = new float[] { -2.1f, 0.0f, 2.1f };

    private void Start()
    {
        if (playerTransform == null)
        {
            JetpackFlightController jet = FindAnyObjectByType<JetpackFlightController>();
            if (jet != null) playerTransform = jet.transform;
            else if (Camera.main != null) playerTransform = Camera.main.transform;
        }

        groundLayerIndex = LayerMask.NameToLayer("Ground");
        if (groundLayerIndex == -1) groundLayerIndex = 0;

        spawnZ = startOffsetZ;
        leftBuildingZ = startOffsetZ;
        rightBuildingZ = startOffsetZ;
        nextFrameZ = startOffsetZ + firstFrameStartDistance;

        for (int i = 0; i < initialChunksOnScreen; i++)
        {
            SpawnChunk();
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        while (spawnZ - playerTransform.position.z < spawnDistanceAhead)
        {
            SpawnChunk();
        }

        CleanupBehindPlayer();
    }

    private void SpawnChunk()
    {
        GameObject chunk = new GameObject("Chunk_" + Mathf.RoundToInt(spawnZ));
        chunk.transform.SetParent(transform);
        chunk.transform.position = new Vector3(0f, 0f, spawnZ);

        float halfRoad = roadWidth * 0.5f;
        float totalFoundationWidth = sidewalkWidth + underBuildingSlabWidth;

        // 1. Center Asphalt Road
        CreateSlab(chunk.transform,
            new Vector3(0f, -0.08f, chunkLength * 0.5f),
            new Vector3(roadWidth, 0.05f, chunkLength),
            roadMaterial, "Road");

        // 2. Left Sidewalk & Foundation Floor
        float leftCenter = -halfRoad - (totalFoundationWidth * 0.5f);
        CreateSlab(chunk.transform,
            new Vector3(leftCenter, footpathHeight * 0.5f, chunkLength * 0.5f),
            new Vector3(totalFoundationWidth, footpathHeight, chunkLength),
            footpathMaterial, "Left_Footpath_Foundation");

        // 3. Right Sidewalk & Foundation Floor
        float rightCenter = halfRoad + (totalFoundationWidth * 0.5f);
        CreateSlab(chunk.transform,
            new Vector3(rightCenter, footpathHeight * 0.5f, chunkLength * 0.5f),
            new Vector3(totalFoundationWidth, footpathHeight, chunkLength),
            footpathMaterial, "Right_Footpath_Foundation");

        // 4. Center Dashed Line
        float currentDashZ = 0.5f;
        while (currentDashZ + dashLength <= chunkLength)
        {
            Vector3 dashPos = new Vector3(0.075f, -0.07f, currentDashZ + (dashLength * 0.5f));
            CreateSlab(chunk.transform, dashPos, new Vector3(dashWidth, 0.06f, dashLength), whiteLineMaterial, "White_Dash");
            currentDashZ += dashLength + dashGap;
        }

        // 5. Buildings - Strictly Single Row, Ground Aligned, Zero Offset Deviation
        if (buildingPrefabs != null && buildingPrefabs.Length > 0 && buildingSpacingStepZ > 0.5f)
        {
            float chunkTargetZ = spawnZ + chunkLength;

            float fixedLeftX = (leftBuildingAnchor != null) ? leftBuildingAnchor.position.x : (-halfRoad - sidewalkWidth);
            float fixedRightX = (rightBuildingAnchor != null) ? rightBuildingAnchor.position.x : (halfRoad + sidewalkWidth);

            // Left Row
            while (leftBuildingZ < chunkTargetZ)
            {
                GameObject prefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
                if (prefab != null)
                {
                    GameObject b = Instantiate(prefab, chunk.transform);
                    b.transform.localScale = b.transform.localScale * buildingScaleMultiplier;
                    b.transform.localRotation = Quaternion.Euler(0f, leftSideYRotation, 0f);

                    // Compensate for any child root offset
                    AlignBuilding(b, fixedLeftX, leftBuildingZ);

                    leftBuildingZ += (buildingSpacingStepZ * buildingScaleMultiplier);
                }
                else
                {
                    leftBuildingZ += 4.5f;
                }
            }

            // Right Row
            while (rightBuildingZ < chunkTargetZ)
            {
                GameObject prefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
                if (prefab != null)
                {
                    GameObject b = Instantiate(prefab, chunk.transform);
                    b.transform.localScale = b.transform.localScale * buildingScaleMultiplier;
                    b.transform.localRotation = Quaternion.Euler(0f, rightSideYRotation, 0f);

                    // Compensate for any child root offset
                    AlignBuilding(b, fixedRightX, rightBuildingZ);

                    rightBuildingZ += (buildingSpacingStepZ * buildingScaleMultiplier);
                }
                else
                {
                    rightBuildingZ += 4.5f;
                }
            }
        }

        // 6. Target Frames
        SpawnTargetFramesInChunk(chunk.transform);

        activeChunks.Add(chunk);
        spawnZ += chunkLength;
    }

    private void AlignBuilding(GameObject obj, float targetX, float targetZ)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            obj.transform.position = new Vector3(targetX, footpathHeight, targetZ);
            return;
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        // Calculate offset between bounding center/min and root transform
        Vector3 delta = obj.transform.position - b.center;
        float bottomY = b.min.y;
        float yAdjustment = footpathHeight - bottomY;

        // Force the bounding center to sit exactly at targetX and targetZ, with base on footpath
        obj.transform.position = new Vector3(targetX + delta.x, obj.transform.position.y + yAdjustment, targetZ + delta.z);
    }

    private void SpawnTargetFramesInChunk(Transform chunkParent)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        float chunkEnd = spawnZ + chunkLength;

        while (nextFrameZ < chunkEnd)
        {
            float posX = gridLanesX[Random.Range(0, gridLanesX.Length)];
            float posY = fixedFrameHeightY;

            GameObject framePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            if (framePrefab != null)
            {
                GameObject spawnedFrame = Instantiate(framePrefab, new Vector3(posX, posY, nextFrameZ), Quaternion.identity);
                spawnedFrame.transform.SetParent(chunkParent);
            }

            nextFrameZ += distanceBetweenFrames;
        }
    }

    private void CreateSlab(Transform parent, Vector3 localPos, Vector3 scale, Material mat, string name)
    {
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(tempCube);

        GameObject slab = new GameObject(name);
        slab.transform.SetParent(parent, false);
        slab.transform.localPosition = localPos;
        slab.transform.localScale = scale;
        slab.layer = groundLayerIndex;

        MeshFilter mf = slab.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = slab.AddComponent<MeshRenderer>();
        if (mat != null) mr.sharedMaterial = mat;

        BoxCollider col = slab.AddComponent<BoxCollider>();
        col.size = Vector3.one;
    }

    private void CleanupBehindPlayer()
    {
        while (activeChunks.Count > 0 && (activeChunks[0].transform.position.z + chunkLength) < (playerTransform.position.z - destroyDistanceBehind))
        {
            Destroy(activeChunks[0]);
            activeChunks.RemoveAt(0);
        }
    }
}