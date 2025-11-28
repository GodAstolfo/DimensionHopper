using UnityEngine;

public class Respawn : MonoBehaviour
{
    [SerializeField] private GameObject respawnPrefab;

    private void OnDestroy()
    {
        // Prevent respawning if the game is quitting
        if (!Application.isPlaying) return;

        // Only respawn if prefab is assigned
        if (respawnPrefab == null) return;

        // Use fixed bounds for respawn position
        float randomX = Random.Range(-7f, 7f);
        float randomY = Random.Range(0.5f, 5f);
        Vector2 randomPos = new Vector2(randomX, randomY);

        Instantiate(respawnPrefab, randomPos, Quaternion.identity);
    }
}