using UnityEngine;

public class FrameTriggerChild : MonoBehaviour
{
    public enum TriggerType { Glass, Border }
    public TriggerType type = TriggerType.Glass;
    public TargetFrame parentFrame;

    private void Awake()
    {
        if (parentFrame == null)
        {
            parentFrame = GetComponentInParent<TargetFrame>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (parentFrame == null) return;

        // Recognizes your Jetpack vehicle, ragdoll, or player tag
        bool isPlayer = other.GetComponentInParent<JetpackFlightController>() != null ||
                        other.transform.root.name.Contains("Jet") ||
                        other.CompareTag("Player");

        if (isPlayer)
        {
            if (type == TriggerType.Glass)
            {
                parentFrame.OnGlassShattered();
            }
            else
            {
                parentFrame.OnBorderHit();
            }
        }
    }
}