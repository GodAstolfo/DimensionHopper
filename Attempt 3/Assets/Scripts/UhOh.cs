using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Hazard: MonoBehaviour
{
    GameManager gameManager;
    private bool isColliding = false;

    private void Start()
    {
        // only works if the Canvas is literally named "Canvas"
        gameManager = GameObject.Find("Canvas").GetComponent<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // OnTriggerEnter2D can get called a LOT, so here, if it's already 
        // colliding, we are bailing out so we don't lose 2 lives at once
        if (isColliding)
        {
            return;
        }

        if (other.gameObject.tag == "Player")
        {
            isColliding = true;

            if (gameManager.numberOfLives == 0)
            {
                gameManager.numberOfLives = 3;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            else
            {
                gameManager.numberOfLives--;
                other.gameObject.transform.position = gameManager.spawnPoint;
            }

            StartCoroutine(Reset());
        }
    }

    IEnumerator Reset()
    {
        yield return new WaitForSeconds(1);
        isColliding = false;
    }
}