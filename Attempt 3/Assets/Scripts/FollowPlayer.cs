using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [Tooltip("Assign the specific Transform to follow. If left empty the script will try to find an object tagged 'Player'.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("If true the follower will mirror the player's horizontal facing by matching the sign of player's localScale.x")]
    [SerializeField] private bool matchPlayerHorizontalFlip = true;

    void Start()
    {
        // If no specific transform assigned, fallback to finding the Player by tag.
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("FollowPlayer: no target assigned and no object with tag 'Player' found.");
            }
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // Match position to the target transform
        transform.position = playerTransform.position;

    }
}