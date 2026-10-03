using UnityEngine;

public class FrameTriggerChild : MonoBehaviour
{
    public enum TriggerType { Glass, Border }
    public TriggerType type = TriggerType.Glass;
    public TargetFrame parentFrame;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerFlightController>() != null)
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