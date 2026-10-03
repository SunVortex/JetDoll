using UnityEngine;

public class TargetFrame : MonoBehaviour
{
    [Header("Components")]
    public GameObject glassPane;
    public Renderer[] borderRenderers;

    [Header("Glow Pulsing (Breathing)")]
    public Color normalColor = Color.white;
    public Color errorColor = Color.red;
    public float pulseSpeed = 4.0f;
    public float minIntensity = 1.0f;
    public float maxIntensity = 4.0f;

    private bool isResolved = false;
    private bool isFaulted = false;
    private Material[] borderMaterials;
    private Transform playerTransform;

    void Awake()
    {
        // Cache material instances for direct emission control
        if (borderRenderers != null && borderRenderers.Length > 0)
        {
            borderMaterials = new Material[borderRenderers.Length];
            for (int i = 0; i < borderRenderers.Length; i++)
            {
                if (borderRenderers[i] != null)
                {
                    borderMaterials[i] = borderRenderers[i].material;
                    borderMaterials[i].EnableKeyword("_EMISSION");
                }
            }
        }
    }

    void Start()
    {
        PlayerFlightController player = FindAnyObjectByType<PlayerFlightController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        // 1. Continuous smooth breathing glow in / glow out
        if (!isFaulted && borderMaterials != null)
        {
            float pulseFactor = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0 to 1
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, pulseFactor);

            Color currentGlow = normalColor * intensity;
            ApplyEmissionColor(currentGlow);
        }

        // 2. Miss check: if player flies 3 meters past this frame without hitting glass
        if (!isResolved && playerTransform != null)
        {
            if (playerTransform.position.z > transform.position.z + 3.0f)
            {
                TriggerMiss();
            }
        }
    }

    public void OnGlassShattered()
    {
        if (isResolved) return;
        isResolved = true;

        if (glassPane != null)
        {
            glassPane.SetActive(false); // Shatters glass pane
        }

        GameManager.Instance?.OnFrameSuccess();
    }

    public void OnBorderHit()
    {
        if (isResolved) return;
        isResolved = true;
        isFaulted = true;

        // Border flashes bright red
        ApplyEmissionColor(errorColor * 5.0f);
        GameManager.Instance?.TakeDamage("Hit Frame Border!");
    }

    void TriggerMiss()
    {
        isResolved = true;
        isFaulted = true;

        // Border turns red because the player skipped it
        ApplyEmissionColor(errorColor * 4.0f);
        GameManager.Instance?.TakeDamage("Missed Frame Target!");
    }

    void ApplyEmissionColor(Color color)
    {
        if (borderMaterials == null) return;

        for (int i = 0; i < borderMaterials.Length; i++)
        {
            if (borderMaterials[i] != null)
            {
                borderMaterials[i].SetColor("_EmissionColor", color);
            }
        }
    }
}