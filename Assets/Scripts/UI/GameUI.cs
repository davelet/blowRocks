using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI livesText;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverTitleText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private TextMeshProUGUI restartText;

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
        LocalizationManager.OnLanguageChanged += UpdateLocalizedTexts;
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnScoreChanged -= UpdateScore;
            gameManager.OnLivesChanged -= UpdateLives;
            gameManager.OnGameOver -= ShowGameOver;
        }
        LocalizationManager.OnLanguageChanged -= UpdateLocalizedTexts;
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"{LocalizationManager.Get("hud.score")}: {score}";
    }

    private void UpdateLives(int lives)
    {
        if (livesText != null)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "\u2665 ";
            livesText.text = $"{LocalizationManager.Get("hud.lives")}: {hearts}".TrimEnd();
        }
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverTitleText != null)
            gameOverTitleText.text = LocalizationManager.Get("gameover.title");

        if (finalScoreText != null && gameManager != null)
            finalScoreText.text = $"{LocalizationManager.Get("gameover.finalScore")}: {gameManager.Score}";
    }

    private void UpdateLocalizedTexts()
    {
        if (gameManager != null)
        {
            UpdateScore(gameManager.Score);
            UpdateLives(gameManager.Lives);
        }

        if (restartText != null)
            restartText.text = LocalizationManager.Get("gameover.restart");

        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            if (gameOverTitleText != null)
                gameOverTitleText.text = LocalizationManager.Get("gameover.title");
            if (finalScoreText != null && gameManager != null)
                finalScoreText.text = $"{LocalizationManager.Get("gameover.finalScore")}: {gameManager.Score}";
        }
    }

    private void OnRestartClicked()
    {
        gameManager?.RestartGame();
    }
}