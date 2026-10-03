using UnityEngine;

public class TestCorridorFlyer : MonoBehaviour
{
    [Header("Flight Speed")]
    public float forwardSpeed = 25f;

    void Update()
    {
        // Smoothly flies forward along the Z axis (no legacy Input calls)
        transform.position += Vector3.forward * forwardSpeed * Time.deltaTime;
    }
}