using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHoverJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Settings")]
    public float hoverScale = 1.12f;
    public float clickScale = 0.95f;
    public float transitionSpeed = 16f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isHovered = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
        isHovered = false;
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
        isHovered = false;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, transitionSpeed * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = originalScale * hoverScale;

        // Play UI Hover Sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIHover();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * clickScale;

        // Play UI Click Sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = isHovered ? originalScale * hoverScale : originalScale;
    }
}