using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main game manager: tracks score, lives, game state.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _ExitOnClose();
#endif

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private AsteroidSpawner spawner;

    [Header("Game Settings")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float respawnDelay = 2f;

    private int score;
    private int lives;
    private bool isGameOver;

    public int Score => score;
    public int Lives => lives;
    public bool IsGameOver => isGameOver;

    // Events for UI
    public System.Action<int> OnScoreChanged;
    public System.Action<int> OnLivesChanged;
    public System.Action OnGameOver;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        lives = startingLives;
        score = 0;
        isGameOver = false;

        // Subscribe to player events
        if (player != null)
        {
            player.OnPlayerDied += HandlePlayerDeath;
        }

        // Notify UI
        OnScoreChanged?.Invoke(score);
        OnLivesChanged?.Invoke(lives);

        // Start the game
        spawner?.StartSpawning();

        // 通知 MusicManager 游戏开始
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.NotifyGameStarted();
        }

        // macOS: close window immediately on red button click
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        Application.wantsToQuit += () =>
        {
            _ExitOnClose();
            return true;
        };
#endif
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnPlayerDied -= HandlePlayerDeath;
        }
    }

    public void AddScore(int points)
    {
        score += points;
        OnScoreChanged?.Invoke(score);
    }

    private void HandlePlayerDeath()
    {
        lives--;
        OnLivesChanged?.Invoke(lives);

        if (lives <= 0)
        {
            GameOver();
        }
        else
        {
            // Respawn after delay
            Invoke(nameof(RespawnPlayer), respawnDelay);
        }
    }

    private void RespawnPlayer()
    {
        if (player == null) return;

        player.transform.position = Vector3.zero;
        player.gameObject.SetActive(true);
        player.ResetHealth();
    }

    private void GameOver()
    {
        isGameOver = true;
        spawner?.StopSpawning();
        OnGameOver?.Invoke();

        // Destroy all remaining asteroids
        foreach (var asteroid in FindObjectsByType<Asteroid>(FindObjectsSortMode.None))
        {
            Destroy(asteroid.gameObject);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}