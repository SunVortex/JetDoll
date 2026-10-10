using System.Collections.Generic;
using UnityEngine;

public class EndlessCorridorSpawner : MonoBehaviour
{
    [Header("Tracking Target")]
    [Tooltip("Drag Jetpack_Vehicle or Main Camera here.")]
    public Transform playerTransform;

    [Header("Rooftop Runway Offset")]
    public float startOffsetZ = 25f;

    [Header("Road & Sidewalk Geometry")]
    public float chunkLength = 100f;
    public float roadWidth = 5.81f;
    public float sidewalkWidth = 4.2f;
    public float underBuildingSlabWidth = 35f;
    public float footpathHeight = 0.15f;

    [Header("Invisible Boundary Walls")]
    public float wallColliderHeight = 25f;
    public float wallColliderThickness = 1.0f;

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
    public float buildingSpacingZ = 6.0f;
    [Tooltip("Distance away from the road curb. Higher value pushes buildings away from the road.")]
    public float buildingFrontOffsetX = 4.5f;
    [Tooltip("Scale multiplier to make all buildings taller and larger.")]
    public float buildingScaleMultiplier = 1.6f;
    public float leftSideYRotation = 0f;
    public float rightSideYRotation = 180f;

    [Header("Target Frames (Obstacles)")]
    [Tooltip("Drag your Target_Frame prefab here.")]
    public GameObject[] obstaclePrefabs;

    [Tooltip("Z distance step between consecutive frames.")]
    public float distanceBetweenFrames = 30f;

    [Tooltip("Safe starting distance before the first frame appears.")]
    public float firstFrameStartDistance = 65f;

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

    // ONLY inside the center lane: narrow Left, Middle, Right offsets right along the road markings
    private readonly float[] centerTrackLanesX = new float[] { -0.75f, 0.0f, 0.75f };

    // 3 Distinct vertical heights to fly through
    private readonly float[] gridHeightsY = new float[] { 2.4f, 4.0f, 5.8f };

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
        leftBuildingZ = startOffsetZ + 10f;
        rightBuildingZ = startOffsetZ + 10f;
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
        chunk.transform.position = new Vector3(0, 0, spawnZ);

        float halfRoad = roadWidth * 0.5f;
        float totalFoundationWidth = sidewalkWidth + underBuildingSlabWidth;

        // 1. Center Asphalt Road
        CreateSlab(chunk.transform,
            new Vector3(0, -0.08f, chunkLength * 0.5f),
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
        float currentDashZ = dashGap * 0.5f;
        while (currentDashZ + dashLength <= chunkLength)
        {
            Vector3 dashPos = new Vector3(0.075f, -0.07f, currentDashZ + (dashLength * 0.5f));
            CreateSlab(chunk.transform, dashPos, new Vector3(dashWidth, 0.06f, dashLength), whiteLineMaterial, "White_Dash");
            currentDashZ += dashLength + dashGap;
        }

        // 5. Invisible Boundary Colliders
        float wallBoundaryX = halfRoad + sidewalkWidth;
        CreateInvisibleWall(chunk.transform, new Vector3(-wallBoundaryX, wallColliderHeight * 0.5f, chunkLength * 0.5f), "Left_Boundary_Collider");
        CreateInvisibleWall(chunk.transform, new Vector3(wallBoundaryX, wallColliderHeight * 0.5f, chunkLength * 0.5f), "Right_Boundary_Collider");

        // 6. Buildings (Larger size & consistent setback from the road)
        if (buildingPrefabs != null && buildingPrefabs.Length > 0 && buildingSpacingZ > 0.5f)
        {
            float chunkTargetZ = spawnZ + chunkLength;
            float leftPlacementX = -halfRoad - sidewalkWidth - buildingFrontOffsetX;
            float rightPlacementX = halfRoad + sidewalkWidth + buildingFrontOffsetX;
            float buildingBaseY = footpathHeight;

            while (leftBuildingZ < chunkTargetZ)
            {
                float placeZ = leftBuildingZ + (buildingSpacingZ * 0.5f);
                GameObject prefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];

                GameObject b = Instantiate(prefab, new Vector3(leftPlacementX, buildingBaseY, placeZ), Quaternion.Euler(0, leftSideYRotation, 0));
                b.transform.localScale *= buildingScaleMultiplier;
                b.transform.SetParent(chunk.transform);

                leftBuildingZ += buildingSpacingZ;
            }

            while (rightBuildingZ < chunkTargetZ)
            {
                float placeZ = rightBuildingZ + (buildingSpacingZ * 0.5f);
                GameObject prefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];

                GameObject b = Instantiate(prefab, new Vector3(rightPlacementX, buildingBaseY, placeZ), Quaternion.Euler(0, rightSideYRotation, 0));
                b.transform.localScale *= buildingScaleMultiplier;
                b.transform.SetParent(chunk.transform);

                rightBuildingZ += buildingSpacingZ;
            }
        }

        // 7. Spawn Target Frames ONLY in Middle corridor track
        SpawnTargetFramesInChunk(chunk.transform);

        activeChunks.Add(chunk);
        spawnZ += chunkLength;
    }

    private void SpawnTargetFramesInChunk(Transform chunkParent)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        float chunkEnd = spawnZ + chunkLength;

        while (nextFrameZ < chunkEnd)
        {
            // Pick narrow center-track lane (-0.75, 0, +0.75)
            float posX = centerTrackLanesX[Random.Range(0, centerTrackLanesX.Length)];

            // Pick flight altitude (Low, Mid, High)
            float posY = gridHeightsY[Random.Range(0, gridHeightsY.Length)];

            GameObject framePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            if (framePrefab != null)
            {
                GameObject spawnedFrame = Instantiate(framePrefab, new Vector3(posX, posY, nextFrameZ), Quaternion.identity);
                spawnedFrame.transform.SetParent(chunkParent);
            }

            nextFrameZ += distanceBetweenFrames;
        }
    }

    private void CreateInvisibleWall(Transform parent, Vector3 localPos, string wallName)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = localPos;

        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = new Vector3(wallColliderThickness, wallColliderHeight, chunkLength);
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