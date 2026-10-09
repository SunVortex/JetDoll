using UnityEngine;

public class TargetFrame : MonoBehaviour
{
    [Header("Components")]
    public GameObject glassPane;
    public Renderer[] borderRenderers;

    [Header("Glow Colors & Feedback")]
    public Color normalColor = Color.white;
    public Color successColor = Color.yellow;
    public Color errorColor = Color.red;
    public float successIntensity = 5.0f;
    public float pulseSpeed = 4.0f;
    public float minIntensity = 1.0f;
    public float maxIntensity = 4.0f;

    private bool isResolved = false;
    private bool isFaulted = false;
    private Material[] borderMaterials;
    private Transform playerTransform;

    private void Awake()
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

    private void Start()
    {
        // Tracks your Jetpack_Vehicle
        JetpackFlightController player = FindAnyObjectByType<JetpackFlightController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // 1. Continuous smooth breathing glow ONLY while approaching (before hit or miss)
        if (!isResolved && !isFaulted && borderMaterials != null)
        {
            float pulseFactor = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
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

    // TRIGGER EVENT: Passing through the center glass trigger
    private void OnTriggerEnter(Collider other)
    {
        if (isResolved) return;

        if (IsPlayer(other.gameObject))
        {
            OnGlassShattered();
        }
    }

    // COLLISION EVENT: Hitting the solid frame border
    private void OnCollisionEnter(Collision collision)
    {
        if (isResolved) return;

        if (IsPlayer(collision.gameObject))
        {
            OnBorderHit();
        }
    }

    public void OnGlassShattered()
    {
        if (isResolved) return;
        isResolved = true;

        // BURST NEON YELLOW ON SUCCESS!
        ApplyEmissionColor(successColor * successIntensity);

        if (glassPane != null)
        {
            glassPane.SetActive(false); // Shatters/clears glass pane
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFrameSuccess();
        }
    }

    public void OnBorderHit()
    {
        if (isResolved) return;
        isResolved = true;
        isFaulted = true;

        // Border flashes bright red
        ApplyEmissionColor(errorColor * 5.0f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage("Hit Frame Border!");
        }
    }

    private void TriggerMiss()
    {
        isResolved = true;
        isFaulted = true;

        // Border turns red because the player skipped it
        ApplyEmissionColor(errorColor * 4.0f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage("Missed Frame Target!");
        }
    }

    private void ApplyEmissionColor(Color color)
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

    private bool IsPlayer(GameObject hitObj)
    {
        return hitObj.GetComponentInParent<JetpackFlightController>() != null ||
               hitObj.transform.root.name.Contains("Jet") ||
               hitObj.CompareTag("Player");
    }
}