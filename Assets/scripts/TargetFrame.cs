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
        // 1. DO NOT RUN CHECKS WHILE PAUSED IN MENU
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying)
        {
            return;
        }

        // 2. Continuous pulse glow while approaching
        if (!isResolved && !isFaulted && borderMaterials != null)
        {
            float pulseFactor = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, pulseFactor);

            Color currentGlow = normalColor * intensity;
            ApplyEmissionColor(currentGlow);
        }

        // 3. Miss check: only triggers while actively playing flight
        if (!isResolved && playerTransform != null)
        {
            if (playerTransform.position.z > transform.position.z + 3.0f)
            {
                TriggerMiss();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isResolved) return;
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying) return;

        if (IsPlayer(other.gameObject))
        {
            OnGlassShattered();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isResolved) return;
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying) return;

        if (IsPlayer(collision.gameObject))
        {
            OnBorderHit();
        }
    }

    public void OnGlassShattered()
    {
        if (isResolved) return;
        isResolved = true;

        ApplyEmissionColor(successColor * successIntensity);

        if (glassPane != null)
        {
            glassPane.SetActive(false);
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