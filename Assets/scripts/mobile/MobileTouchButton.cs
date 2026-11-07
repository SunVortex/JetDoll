using UnityEngine;
using UnityEngine.EventSystems;

public class MobileTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool IsPressed { get; private set; }
    private bool tappedThisFrame = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
        tappedThisFrame = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
    }

    public bool ConsumeTap()
    {
        if (tappedThisFrame)
        {
            tappedThisFrame = false;
            return true;
        }
        return false;
    }

    private void LateUpdate()
    {
        tappedThisFrame = false;
    }

    private void OnDisable()
    {
        IsPressed = false;
        tappedThisFrame = false;
    }
}