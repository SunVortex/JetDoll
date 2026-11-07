using UnityEngine;
using UnityEngine.EventSystems;

public class TouchAimZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Vector2 TouchDelta { get; private set; }
    public bool IsHolding { get; private set; } = false;

    private int activeFingerId = -1;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activeFingerId == -1)
        {
            activeFingerId = eventData.pointerId;
            IsHolding = true;
            TouchDelta = Vector2.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == activeFingerId)
        {
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            TouchDelta = new Vector2(eventData.delta.x / dpi * 100f, eventData.delta.y / dpi * 100f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activeFingerId)
        {
            activeFingerId = -1;
            IsHolding = false;
            TouchDelta = Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        TouchDelta = Vector2.zero;
    }

    private void OnDisable()
    {
        activeFingerId = -1;
        IsHolding = false;
        TouchDelta = Vector2.zero;
    }
}