using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    private Transform playerTransform;

    void Start()
    {
        // Find the player GameObject by tag and get its Transform
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Player object with tag 'Player' not found in the scene.");
        }
    }

    void LateUpdate()
    {
        if (playerTransform != null)
        {
            // Fix this object's position to the player's position
            transform.position = playerTransform.position;
        }
    }
}