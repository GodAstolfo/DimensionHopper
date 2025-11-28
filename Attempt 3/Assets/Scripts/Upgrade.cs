using UnityEngine;

public class Upgrade : MonoBehaviour
{
    [Tooltip("Current delay between shots. Will be halved when applied if you treat lower as faster.")]
    public float fireRate = 1.0f;

    // Common handling for both 2D and 3D trigger events
    private void HandlePickup(GameObject other)
    {
        if (other == null) return;

        // require exact tag "Pickup"
        if (!other.CompareTag("Pickup")) return;

        // double fire rate
        fireRate *= 2f;

        // remove the pickup object
        Destroy(other);
    }

    // 3D physics trigger
    private void OnTriggerEnter(Collider other)
    {
        HandlePickup(other?.gameObject);
    }

    // 2D physics trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandlePickup(other?.gameObject);
    }
}