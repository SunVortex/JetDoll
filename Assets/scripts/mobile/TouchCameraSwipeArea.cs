using UnityEngine;
using UnityEngine.EventSystems;

public class TouchCameraSwipeArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Vector2 TouchDelta { get; private set; }
    private int activeFingerId = -1;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activeFingerId == -1)
        {
            activeFingerId = eventData.pointerId;
            TouchDelta = Vector2.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == activeFingerId)
        {
            // Normalize touch delta for consistent speed across different mobile screen pixel densities
            TouchDelta = new Vector2(eventData.delta.x / Screen.dpi * 100f, eventData.delta.y / Screen.dpi * 100f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activeFingerId)
        {
            activeFingerId = -1;
            TouchDelta = Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        // Reset delta every frame so movement doesn't stick
        TouchDelta = Vector2.zero;
    }
}