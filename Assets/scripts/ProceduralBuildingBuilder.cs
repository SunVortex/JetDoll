using UnityEngine;

[ExecuteInEditMode]
public class ProceduralBuildingBuilder : MonoBehaviour
{
    public enum BuildingStyle
    {
        // --- Core Geometric Styles ---
        OverhangingSlabs,
        SteppedBlocks,
        ZigguratPyramid,
        ModularTetris,
        SpireNeedle,
        StackedContainer,
        RibbedFin,
        BrutalistFortress,
        OffsetTower,
        LShapedCorner,
        CylinderPedestal,
        FloatingBalconies,
        PixelMatrix,
        DoubleSlabSandwich,
        PodCluster,
        DiagonalStaircase,
        MonolithPillars,

        // --- 9 Styles ---
        CheckerboardSlabs,
        CentralSpineWings,
        NestedFrames,
        ChevronArrow,
        TotemSegments,
        SlottedTower,
        CornerNotchedCube,
        HorizontalLouvers,
        AsymmetricalPylon
    }

    public enum RoofTopperType
    {
        Cone,
        CylinderSpire,
        ConeAndCylinder,
        TwinCones,
        SphereAndSpire,
        None
    }

    [Header("Building Setup")]
    public BuildingStyle style = BuildingStyle.OverhangingSlabs;
    public RoofTopperType roofTopper = RoofTopperType.Cone;
    public float baseWidth = 5f;
    public float baseDepth = 5f;
    public float totalHeight = 9f;

    [Header("Materials (Assign from your colours folder)")]
    public Material[] materials;

    [Header("Fallback Colors (Used if materials array is empty)")]
    public Material fallbackBaseMaterial;
    public Color[] palette = new Color[]
    {
        new Color(0.24f, 0.82f, 0.65f), // Mint Green
        new Color(0.85f, 0.65f, 0.78f), // Soft Lilac / Pink
        new Color(0.18f, 0.52f, 0.62f), // Teal Blue
        new Color(0.42f, 0.38f, 0.75f), // Indigo Purple
        new Color(0.92f, 0.76f, 0.62f), // Warm Sand / Beige
        new Color(0.88f, 0.45f, 0.58f), // Coral Pink
        new Color(0.20f, 0.28f, 0.30f), // Dark Slate
        new Color(0.96f, 0.96f, 0.96f)  // Off White
    };

    [ContextMenu("Generate Building")]
    public void Build()
    {
        Clear();

        switch (style)
        {
            case BuildingStyle.OverhangingSlabs: BuildOverhangingSlabs(); break;
            case BuildingStyle.SteppedBlocks: BuildSteppedBlocks(); break;
            case BuildingStyle.ZigguratPyramid: BuildZigguratPyramid(); break;
            case BuildingStyle.ModularTetris: BuildModularTetris(); break;
            case BuildingStyle.SpireNeedle: BuildSpireNeedle(); break;
            case BuildingStyle.StackedContainer: BuildStackedContainer(); break;
            case BuildingStyle.RibbedFin: BuildRibbedFin(); break;
            case BuildingStyle.BrutalistFortress: BuildBrutalistFortress(); break;
            case BuildingStyle.OffsetTower: BuildOffsetTower(); break;
            case BuildingStyle.LShapedCorner: BuildLShapedCorner(); break;
            case BuildingStyle.CylinderPedestal: BuildCylinderPedestal(); break;
            case BuildingStyle.FloatingBalconies: BuildFloatingBalconies(); break;
            case BuildingStyle.PixelMatrix: BuildPixelMatrix(); break;
            case BuildingStyle.DoubleSlabSandwich: BuildDoubleSlabSandwich(); break;
            case BuildingStyle.PodCluster: BuildPodCluster(); break;
            case BuildingStyle.DiagonalStaircase: BuildDiagonalStaircase(); break;
            case BuildingStyle.MonolithPillars: BuildMonolithPillars(); break;
            case BuildingStyle.CheckerboardSlabs: BuildCheckerboardSlabs(); break;
            case BuildingStyle.CentralSpineWings: BuildCentralSpineWings(); break;
            case BuildingStyle.NestedFrames: BuildNestedFrames(); break;
            case BuildingStyle.ChevronArrow: BuildChevronArrow(); break;
            case BuildingStyle.TotemSegments: BuildTotemSegments(); break;
            case BuildingStyle.SlottedTower: BuildSlottedTower(); break;
            case BuildingStyle.CornerNotchedCube: BuildCornerNotchedCube(); break;
            case BuildingStyle.HorizontalLouvers: BuildHorizontalLouvers(); break;
            case BuildingStyle.AsymmetricalPylon: BuildAsymmetricalPylon(); break;
        }

        BuildRoofTopper();
    }

    [ContextMenu("Clear Building")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    void BuildRoofTopper()
    {
        if (roofTopper == RoofTopperType.None) return;

        float topY = totalHeight;
        Material accentMat = GetSelectedMaterial();

        switch (roofTopper)
        {
            case RoofTopperType.Cone:
                CreateCone(new Vector3(0, topY, 0), baseWidth * 0.45f, 2.5f, accentMat);
                break;

            case RoofTopperType.CylinderSpire:
                CreateMeshPart(PrimitiveType.Cylinder, new Vector3(0, topY + 1.25f, 0), new Vector3(0.8f, 1.25f, 0.8f), accentMat);
                break;

            case RoofTopperType.ConeAndCylinder:
                CreateMeshPart(PrimitiveType.Cylinder, new Vector3(0, topY + 0.6f, 0), new Vector3(1.2f, 0.6f, 1.2f), accentMat);
                CreateCone(new Vector3(0, topY + 1.2f, 0), baseWidth * 0.35f, 2.2f, GetSelectedMaterial());
                break;

            case RoofTopperType.TwinCones:
                float offset = baseWidth * 0.25f;
                CreateCone(new Vector3(-offset, topY, 0), baseWidth * 0.3f, 2.0f, accentMat);
                CreateCone(new Vector3(offset, topY, 0), baseWidth * 0.3f, 2.0f, accentMat);
                break;

            case RoofTopperType.SphereAndSpire:
                CreateMeshPart(PrimitiveType.Cylinder, new Vector3(0, topY + 0.8f, 0), new Vector3(0.6f, 0.8f, 0.6f), accentMat);
                CreateMeshPart(PrimitiveType.Sphere, new Vector3(0, topY + 2.0f, 0), Vector3.one * 1.5f, GetSelectedMaterial());
                break;
        }
    }

    void BuildOverhangingSlabs()
    {
        float currentY = 0f;
        int layers = Mathf.Max(3, Mathf.FloorToInt(totalHeight / 2.2f));
        for (int i = 0; i < layers; i++)
        {
            float slabH = totalHeight / layers;
            float slabW = baseWidth * Random.Range(1.0f, 1.4f);
            float slabD = baseDepth * Random.Range(0.9f, 1.2f);
            float shift = (i % 2 == 0) ? -1.0f : 1.0f;
            CreateMeshPart(PrimitiveType.Cube, new Vector3(shift, currentY + slabH * 0.5f, 0), new Vector3(slabW, slabH, slabD), GetSelectedMaterial());
            currentY += slabH;
        }
    }

    void BuildSteppedBlocks()
    {
        for (int i = 0; i < 6; i++)
        {
            float w = Random.Range(2.5f, baseWidth);
            float d = Random.Range(2.5f, baseDepth);
            float h = Random.Range(totalHeight * 0.45f, totalHeight);
            float posX = Random.Range(-baseWidth * 0.25f, baseWidth * 0.25f);
            float posZ = Random.Range(-baseDepth * 0.25f, baseDepth * 0.25f);
            CreateMeshPart(PrimitiveType.Cube, new Vector3(posX, h * 0.5f, posZ), new Vector3(w, h, d), GetSelectedMaterial());
        }
    }

    void BuildZigguratPyramid()
    {
        int steps = 4;
        float stepH = totalHeight / steps;
        float currentY = 0f;
        for (int i = 0; i < steps; i++)
        {
            float factor = 1.3f - (i * 0.25f);
            CreateMeshPart(PrimitiveType.Cube, new Vector3(0, currentY + stepH * 0.5f, 0), new Vector3(baseWidth * factor, stepH, baseDepth * factor), GetSelectedMaterial());
            currentY += stepH;
        }
    }

    void BuildModularTetris()
    {
        for (int i = 0; i < 7; i++)
        {
            bool vertical = (i % 2 == 0);
            Vector3 size = vertical
                ? new Vector3(baseWidth * 0.4f, Random.Range(4f, totalHeight * 0.8f), baseDepth * 0.45f)
                : new Vector3(baseWidth * 1.2f, Random.Range(1.5f, 2.8f), baseDepth * 0.9f);
            float posX = Random.Range(-baseWidth * 0.3f, baseWidth * 0.3f);
            float posY = Random.Range(size.y * 0.5f, totalHeight - size.y * 0.5f);
            float posZ = Random.Range(-baseDepth * 0.2f, baseDepth * 0.2f);
            CreateMeshPart(PrimitiveType.Cube, new Vector3(posX, posY, posZ), size, GetSelectedMaterial());
        }
    }

    void BuildSpireNeedle()
    {
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, 0), new Vector3(baseWidth * 0.55f, totalHeight, baseDepth * 0.55f), GetSelectedMaterial());
        float finH = totalHeight * 0.6f;
        CreateMeshPart(PrimitiveType.Cube, new Vector3(-baseWidth * 0.45f, finH * 0.5f, 0), new Vector3(baseWidth * 0.25f, finH, baseDepth * 0.45f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(baseWidth * 0.45f, finH * 0.5f, 0), new Vector3(baseWidth * 0.25f, finH, baseDepth * 0.45f), GetSelectedMaterial());
    }

    void BuildStackedContainer()
    {
        int tiers = 4;
        float h = totalHeight / tiers;
        float currentY = 0f;
        for (int i = 0; i < tiers; i++)
        {
            Vector3 size = (i % 2 == 0)
                ? new Vector3(baseWidth * 1.3f, h, baseDepth * 0.7f)
                : new Vector3(baseWidth * 0.7f, h, baseDepth * 1.3f);
            CreateMeshPart(PrimitiveType.Cube, new Vector3(Random.Range(-0.5f, 0.5f), currentY + h * 0.5f, Random.Range(-0.5f, 0.5f)), size, GetSelectedMaterial());
            currentY += h;
        }
    }

    void BuildRibbedFin()
    {
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, 0), new Vector3(baseWidth, totalHeight, baseDepth * 0.6f), GetSelectedMaterial());
        int finCount = 4;
        float stepZ = baseDepth / (finCount + 1);
        for (int i = 0; i < finCount; i++)
        {
            float posZ = -baseDepth * 0.5f + stepZ * (i + 1);
            CreateMeshPart(PrimitiveType.Cube, new Vector3(baseWidth * 0.4f, totalHeight * 0.45f, posZ), new Vector3(baseWidth * 0.35f, totalHeight * 0.9f, 0.4f), GetSelectedMaterial());
        }
    }

    void BuildBrutalistFortress()
    {
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.35f, 0), new Vector3(baseWidth * 1.1f, totalHeight * 0.7f, baseDepth * 1.1f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0.6f, totalHeight * 0.8f, 0), new Vector3(baseWidth * 0.7f, totalHeight * 0.4f, baseDepth * 0.7f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Sphere, new Vector3(-baseWidth * 0.35f, totalHeight * 0.75f, baseDepth * 0.35f), Vector3.one * 1.8f, GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cylinder, new Vector3(baseWidth * 0.3f, totalHeight * 0.4f, -baseDepth * 0.55f), new Vector3(0.8f, totalHeight * 0.3f, 0.8f), GetSelectedMaterial());
    }

    void BuildOffsetTower()
    {
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, 0), new Vector3(baseWidth * 0.5f, totalHeight, baseDepth * 0.5f), GetSelectedMaterial());
        for (int i = 0; i < 4; i++)
        {
            float y = (totalHeight / 5f) * (i + 1);
            float side = (i % 2 == 0) ? 1f : -1f;
            CreateMeshPart(PrimitiveType.Cube, new Vector3(side * baseWidth * 0.4f, y, 0), new Vector3(baseWidth * 0.55f, 1.8f, baseDepth * 0.7f), GetSelectedMaterial());
        }
    }

    void BuildLShapedCorner()
    {
        float currentY = 0f;
        int tiers = 3;
        float h = totalHeight / tiers;
        for (int i = 0; i < tiers; i++)
        {
            float flip = (i % 2 == 0) ? 1f : -1f;
            CreateMeshPart(PrimitiveType.Cube, new Vector3(0, currentY + h * 0.5f, -baseDepth * 0.25f), new Vector3(baseWidth, h * 0.95f, baseDepth * 0.5f), GetSelectedMaterial());
            CreateMeshPart(PrimitiveType.Cube, new Vector3(flip * baseWidth * 0.25f, currentY + h * 0.5f, baseDepth * 0.25f), new Vector3(baseWidth * 0.5f, h * 0.95f, baseDepth * 0.5f), GetSelectedMaterial());
            currentY += h;
        }
    }

    void BuildCylinderPedestal()
    {
        float pillarH = totalHeight * 0.45f;
        float pOffX = baseWidth * 0.3f;
        float pOffZ = baseDepth * 0.3f;

        CreateMeshPart(PrimitiveType.Cylinder, new Vector3(-pOffX, pillarH * 0.5f, -pOffZ), new Vector3(1.2f, pillarH * 0.5f, 1.2f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cylinder, new Vector3(pOffX, pillarH * 0.5f, -pOffZ), new Vector3(1.2f, pillarH * 0.5f, 1.2f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cylinder, new Vector3(-pOffX, pillarH * 0.5f, pOffZ), new Vector3(1.2f, pillarH * 0.5f, 1.2f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cylinder, new Vector3(pOffX, pillarH * 0.5f, pOffZ), new Vector3(1.2f, pillarH * 0.5f, 1.2f), GetSelectedMaterial());

        float slabH = totalHeight - pillarH;
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, pillarH + slabH * 0.5f, 0), new Vector3(baseWidth * 1.2f, slabH, baseDepth * 1.2f), GetSelectedMaterial());
    }

    void BuildFloatingBalconies()
    {
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, -baseDepth * 0.2f), new Vector3(baseWidth, totalHeight, baseDepth * 0.6f), GetSelectedMaterial());
        for (int i = 0; i < 3; i++)
        {
            float y = (totalHeight / 4f) * (i + 1);
            float x = (i % 2 == 0) ? -baseWidth * 0.22f : baseWidth * 0.22f;
            CreateMeshPart(PrimitiveType.Cube, new Vector3(x, y, baseDepth * 0.35f), new Vector3(baseWidth * 0.45f, 1.4f, baseDepth * 0.6f), GetSelectedMaterial());
        }
    }

    void BuildPixelMatrix()
    {
        int rows = 3;
        int cols = 3;
        float cellW = baseWidth / cols;
        float cellH = totalHeight / rows;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float x = -baseWidth * 0.5f + cellW * (c + 0.5f);
                float y = cellH * (r + 0.5f);
                float depthMod = Random.Range(0.6f, 1.3f);
                CreateMeshPart(PrimitiveType.Cube, new Vector3(x, y, 0), new Vector3(cellW * 0.92f, cellH * 0.92f, baseDepth * depthMod), GetSelectedMaterial());
            }
        }
    }

    void BuildDoubleSlabSandwich()
    {
        float slabThick = 0.5f;
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, -baseDepth * 0.45f), new Vector3(baseWidth * 1.1f, totalHeight, slabThick), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, baseDepth * 0.45f), new Vector3(baseWidth * 1.1f, totalHeight, slabThick), GetSelectedMaterial());

        for (int i = 0; i < 3; i++)
        {
            float y = (totalHeight / 4f) * (i + 1);
            CreateMeshPart(PrimitiveType.Cube, new Vector3(Random.Range(-0.6f, 0.6f), y, 0), new Vector3(baseWidth * 0.6f, 1.6f, baseDepth * 0.85f), GetSelectedMaterial());
        }
    }

    void BuildPodCluster()
    {
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, 0), new Vector3(baseWidth * 0.6f, totalHeight, baseDepth * 0.6f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Sphere, new Vector3(baseWidth * 0.35f, totalHeight * 0.35f, baseDepth * 0.35f), Vector3.one * 2.2f, GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Sphere, new Vector3(-baseWidth * 0.35f, totalHeight * 0.7f, -baseDepth * 0.2f), Vector3.one * 2.5f, GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cylinder, new Vector3(0, totalHeight * 0.5f, baseDepth * 0.35f), new Vector3(1.2f, 1.8f, 1.2f), GetSelectedMaterial());
    }

    void BuildDiagonalStaircase()
    {
        int steps = 4;
        float stepW = baseWidth / steps;
        float stepH = totalHeight / steps;
        for (int i = 0; i < steps; i++)
        {
            float x = -baseWidth * 0.5f + stepW * (i + 0.5f);
            float h = stepH * (i + 1);
            CreateMeshPart(PrimitiveType.Cube, new Vector3(x, h * 0.5f, 0), new Vector3(stepW * 0.95f, h, baseDepth * 0.95f), GetSelectedMaterial());
        }
    }

    void BuildMonolithPillars()
    {
        float pWidth = baseWidth * 0.4f;
        float spacing = baseWidth * 0.28f;
        CreateMeshPart(PrimitiveType.Cube, new Vector3(-spacing, totalHeight * 0.5f, 0), new Vector3(pWidth, totalHeight, baseDepth), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(spacing, totalHeight * 0.5f, 0), new Vector3(pWidth, totalHeight, baseDepth), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.6f, 0), new Vector3(baseWidth * 1.2f, 1.5f, baseDepth * 1.15f), GetSelectedMaterial());
    }

    void BuildCheckerboardSlabs()
    {
        int tiers = 4;
        float tierH = totalHeight / tiers;
        float currentY = 0f;
        for (int i = 0; i < tiers; i++)
        {
            float offsetA = (i % 2 == 0) ? -baseWidth * 0.25f : baseWidth * 0.25f;
            float offsetB = -offsetA;
            CreateMeshPart(PrimitiveType.Cube, new Vector3(offsetA, currentY + tierH * 0.5f, -baseDepth * 0.22f), new Vector3(baseWidth * 0.48f, tierH * 0.95f, baseDepth * 0.48f), GetSelectedMaterial());
            CreateMeshPart(PrimitiveType.Cube, new Vector3(offsetB, currentY + tierH * 0.5f, baseDepth * 0.22f), new Vector3(baseWidth * 0.48f, tierH * 0.95f, baseDepth * 0.48f), GetSelectedMaterial());
            currentY += tierH;
        }
    }

    void BuildCentralSpineWings()
    {
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, 0), new Vector3(baseWidth * 0.3f, totalHeight, baseDepth * 0.4f), GetSelectedMaterial());
        int wingTiers = 3;
        for (int i = 0; i < wingTiers; i++)
        {
            float y = (totalHeight / 4f) * (i + 1);
            float wingW = baseWidth * Random.Range(0.45f, 0.6f);
            CreateMeshPart(PrimitiveType.Cube, new Vector3(-baseWidth * 0.35f, y, 0), new Vector3(wingW, 1.2f, baseDepth * 0.85f), GetSelectedMaterial());
            CreateMeshPart(PrimitiveType.Cube, new Vector3(baseWidth * 0.35f, y + 0.4f, 0), new Vector3(wingW, 1.2f, baseDepth * 0.85f), GetSelectedMaterial());
        }
    }

    void BuildNestedFrames()
    {
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, 0), new Vector3(baseWidth, totalHeight, baseDepth * 0.6f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, baseDepth * 0.35f), new Vector3(baseWidth * 0.65f, totalHeight * 0.7f, baseDepth * 0.4f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, baseDepth * 0.5f), new Vector3(baseWidth * 0.35f, totalHeight * 0.4f, baseDepth * 0.2f), GetSelectedMaterial());
    }

    void BuildChevronArrow()
    {
        int tiers = 4;
        float h = totalHeight / tiers;
        float currentY = 0f;
        for (int i = 0; i < tiers; i++)
        {
            float wingW = baseWidth * 0.55f;
            GameObject leftWing = CreateMeshPartReturn(PrimitiveType.Cube, new Vector3(-baseWidth * 0.22f, currentY + h * 0.5f, 0), new Vector3(wingW, h * 0.9f, baseDepth * 0.6f), GetSelectedMaterial());
            leftWing.transform.localRotation = Quaternion.Euler(0, 20f, 0);

            GameObject rightWing = CreateMeshPartReturn(PrimitiveType.Cube, new Vector3(baseWidth * 0.22f, currentY + h * 0.5f, 0), new Vector3(wingW, h * 0.9f, baseDepth * 0.6f), GetSelectedMaterial());
            rightWing.transform.localRotation = Quaternion.Euler(0, -20f, 0);

            currentY += h;
        }
    }

    void BuildTotemSegments()
    {
        int tiers = 5;
        float h = totalHeight / tiers;
        float currentY = 0f;
        for (int i = 0; i < tiers; i++)
        {
            if (i % 2 == 0)
            {
                CreateMeshPart(PrimitiveType.Cube, new Vector3(0, currentY + h * 0.5f, 0), new Vector3(baseWidth * 0.9f, h, baseDepth * 0.9f), GetSelectedMaterial());
            }
            else
            {
                CreateMeshPart(PrimitiveType.Cylinder, new Vector3(0, currentY + h * 0.5f, 0), new Vector3(baseWidth * 0.7f, h * 0.5f, baseDepth * 0.7f), GetSelectedMaterial());
            }
            currentY += h;
        }
    }

    void BuildSlottedTower()
    {
        float pierW = baseWidth * 0.35f;
        CreateMeshPart(PrimitiveType.Cube, new Vector3(-baseWidth * 0.3f, totalHeight * 0.5f, 0), new Vector3(pierW, totalHeight, baseDepth), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(baseWidth * 0.3f, totalHeight * 0.5f, 0), new Vector3(pierW, totalHeight, baseDepth), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.3f, 0), new Vector3(baseWidth * 0.35f, 1.4f, baseDepth * 0.8f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.7f, 0), new Vector3(baseWidth * 0.35f, 1.4f, baseDepth * 0.8f), GetSelectedMaterial());
    }

    void BuildCornerNotchedCube()
    {
        float currentY = 0f;
        int tiers = 3;
        float h = totalHeight / tiers;
        for (int i = 0; i < tiers; i++)
        {
            CreateMeshPart(PrimitiveType.Cube, new Vector3(-baseWidth * 0.12f, currentY + h * 0.5f, 0), new Vector3(baseWidth * 0.75f, h * 0.95f, baseDepth), GetSelectedMaterial());
            CreateMeshPart(PrimitiveType.Cube, new Vector3(baseWidth * 0.32f, currentY + h * 0.5f, baseDepth * 0.22f), new Vector3(baseWidth * 0.35f, h * 0.95f, baseDepth * 0.45f), GetSelectedMaterial());
            currentY += h;
        }
    }

    void BuildHorizontalLouvers()
    {
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.5f, 0), new Vector3(baseWidth * 0.7f, totalHeight, baseDepth * 0.7f), GetSelectedMaterial());
        int louverCount = 6;
        float stepY = totalHeight / (louverCount + 1);
        for (int i = 0; i < louverCount; i++)
        {
            float y = stepY * (i + 1);
            CreateMeshPart(PrimitiveType.Cube, new Vector3(0, y, baseDepth * 0.38f), new Vector3(baseWidth * 1.15f, 0.25f, baseDepth * 0.35f), GetSelectedMaterial());
        }
    }

    void BuildAsymmetricalPylon()
    {
        CreateMeshPart(PrimitiveType.Cube, new Vector3(-baseWidth * 0.25f, totalHeight * 0.5f, 0), new Vector3(baseWidth * 0.5f, totalHeight, baseDepth * 0.9f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(baseWidth * 0.3f, totalHeight * 0.25f, 0), new Vector3(baseWidth * 0.55f, totalHeight * 0.5f, baseDepth * 0.85f), GetSelectedMaterial());
        CreateMeshPart(PrimitiveType.Cube, new Vector3(0, totalHeight * 0.48f, baseDepth * 0.2f), new Vector3(baseWidth, 1.2f, baseDepth * 0.4f), GetSelectedMaterial());
    }

    void CreateMeshPart(PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat)
    {
        CreateMeshPartReturn(type, localPos, scale, mat);
    }

    GameObject CreateMeshPartReturn(PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject tempObj = GameObject.CreatePrimitive(type);
        Mesh mesh = tempObj.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(tempObj);

        GameObject obj = new GameObject(type.ToString());
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = scale;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        return obj;
    }

    void CreateCone(Vector3 basePos, float radius, float height, Material mat)
    {
        GameObject coneObj = new GameObject("Roof_Cone");
        coneObj.transform.SetParent(transform, false);
        coneObj.transform.localPosition = basePos;

        MeshFilter mf = coneObj.AddComponent<MeshFilter>();
        MeshRenderer mr = coneObj.AddComponent<MeshRenderer>();

        int segments = 16;
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralCone";

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 6];

        vertices[0] = new Vector3(0, height, 0);
        vertices[1] = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            vertices[i + 2] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = next + 2;
        }

        int baseOffset = segments * 3;
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles[baseOffset + i * 3] = 1;
            triangles[baseOffset + i * 3 + 1] = next + 2;
            triangles[baseOffset + i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        mr.sharedMaterial = mat;
    }

    Material GetSelectedMaterial()
    {
        // 1. If you assigned saved project materials, pick one directly
        if (materials != null && materials.Length > 0)
        {
            Material picked = materials[Random.Range(0, materials.Length)];
            if (picked != null) return picked;
        }

        // 2. Fallback: create lit material with palette color
        Material fallback = fallbackBaseMaterial != null
            ? new Material(fallbackBaseMaterial)
            : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

        fallback.color = palette[Random.Range(0, palette.Length)];
        return fallback;
    }
}