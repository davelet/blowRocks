using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller: displays score, lives, and game over screen.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI livesText;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button restartButton;

    private GameManager gameManager;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnScoreChanged += UpdateScore;
            gameManager.OnLivesChanged += UpdateLives;
            gameManager.OnGameOver += ShowGameOver;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnScoreChanged -= UpdateScore;
            gameManager.OnLivesChanged -= UpdateLives;
            gameManager.OnGameOver -= ShowGameOver;
        }
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    private void UpdateLives(int lives)
    {
        if (livesText != null)
            livesText.text = $"Lives: {lives}";
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (finalScoreText != null)
            finalScoreText.text = $"Final Score: {gameManager?.Score ?? 0}";
    }

    private void OnRestartClicked()
    {
        gameManager?.RestartGame();
    }
}
