using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int numberOfKeys = 0;
    public int numberOfLives = 3;
    public int numberOfCoins = 0;

    public TextMeshProUGUI keyText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI coinText;

    public Vector3 spawnPoint;
    public AudioSource audioSource;
    public AudioClip keySound;
    public AudioClip coinSound;
    public AudioClip portalSound;

    void Awake()
    {
        // Simple singleton: keep one persistent GameManager across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        spawnPoint = Vector3.zero;

        // Respect inspector assignment, but try to find one if it's missing
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogWarning("GameManager: no AudioSource found on GameObject. Adding one automatically.");
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (keySound == null)
            Debug.LogWarning("GameManager: keySound AudioClip is not assigned in the Inspector.");
    }

    // Update is called once per frame
    void Update()
    {
        keyText.text = "Keys: " + numberOfKeys;
        livesText.text = "Lives: " + numberOfLives;
        coinText.text = "Coins: " + numberOfCoins;
    }

    public void OnKeyPickedUp()
    {
        numberOfKeys++;

        if (keySound == null)
        {
            Debug.LogWarning("OnKeyPickedUp called but keySound is null. Assign an AudioClip in the Inspector.");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("OnKeyPickedUp: audioSource missing; creating one.");
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.PlayOneShot(keySound);
    }

    public void OnCoinPickedUp()
    {
        numberOfCoins++;

        if (coinSound == null)
        {
            Debug.LogWarning("OnCoinPickedUp called but coinSound is null. Assign an AudioClip in the Inspector.");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("OnCoinPickedUp: audioSource missing; creating one.");
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.PlayOneShot(coinSound);
    }


    public async Task OnPortalEntered()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        }

        if (portalSound != null)
            audioSource.PlayOneShot(portalSound);

        spawnPoint = Vector3.zero;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}