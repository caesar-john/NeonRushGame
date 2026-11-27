
using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text topScoreText;

    [Header("Game Over")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalDistanceText;
    [SerializeField] private TMP_Text finalCoinText;
    [SerializeField] private TMP_Text bestScoreText;

    [Header("Format")]
    [SerializeField] private string scorePrefix = "SKOR  ";
    [SerializeField] private string distanceSuffix = " m";
    [SerializeField] private string coinPrefix = "KOIN  x";
    [SerializeField] private string topScorePrefix = "TOP  ";

    private GameManager gameManager;
    private ScoreManager scoreManager;
    private bool subscribed;
    private bool compactHudMode;

    /// <summary>
    /// Dipanggil oleh NeonUITheme saat HUD sudah memiliki label terpisah
    /// (SCORE / BEST / COINS) agar teks angka tampil ringkas dan rapi.
    /// </summary>
    public void SetCompactHudMode(bool enabled)
    {
        if (compactHudMode == enabled) return;
        compactHudMode = enabled;
        RefreshAll();
    }

    private void Start()
    {
        gameManager = GameManager.Instance;

        scoreManager = gameManager != null
            ? gameManager.ScoreSystem
            : FindFirstObjectByType<ScoreManager>();

        Subscribe();
        RefreshAll();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed) return;

        if (gameManager != null)
        {
            gameManager.CoinsChanged += OnCoinsChanged;
            gameManager.RunEnded += OnRunEnded;
        }

        if (scoreManager != null)
        {
            scoreManager.ScoreChanged += OnScoreChanged;
            scoreManager.DistanceChanged += OnDistanceChanged;
            scoreManager.BestScoreChanged += OnBestScoreChanged;
        }

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (gameManager != null)
        {
            gameManager.CoinsChanged -= OnCoinsChanged;
            gameManager.RunEnded -= OnRunEnded;
        }

        if (scoreManager != null)
        {
            scoreManager.ScoreChanged -= OnScoreChanged;
            scoreManager.DistanceChanged -= OnDistanceChanged;
            scoreManager.BestScoreChanged -= OnBestScoreChanged;
        }

        subscribed = false;
    }

    private void RefreshAll()
    {
        if (scoreManager != null)
        {
            OnScoreChanged(scoreManager.Score);

            OnDistanceChanged(
                Mathf.FloorToInt(scoreManager.Distance)
            );

            OnBestScoreChanged(scoreManager.BestScore);
        }

        if (gameManager != null)
        {
            OnCoinsChanged(gameManager.Coins);
        }
    }

    private void OnScoreChanged(int score)
    {
        if (scoreText != null)
            scoreText.text = compactHudMode ? score.ToString("N0") : scorePrefix + score;

        // Tampilkan rekor lama atau skor saat ini,
        // mana yang lebih tinggi.
        if (topScoreText != null && scoreManager != null)
        {
            int top = Mathf.Max(scoreManager.BestScore, score);
            topScoreText.text = compactHudMode ? top.ToString("N0") : topScorePrefix + top;
        }
    }

    private void OnDistanceChanged(int meters)
    {
        if (distanceText != null)
            distanceText.text = meters.ToString("N0") + distanceSuffix;
    }

    private void OnCoinsChanged(int coins)
    {
        if (coinText != null)
            coinText.text = compactHudMode ? coins.ToString("N0") : coinPrefix + coins;
    }

    private void OnBestScoreChanged(int bestScore)
    {
        if (topScoreText != null)
            topScoreText.text = compactHudMode ? bestScore.ToString("N0") : topScorePrefix + bestScore;
    }

    private void OnRunEnded()
    {
        Debug.Log(
            "[HUD] GAME OVER | Score: " +
            (scoreManager != null ? scoreManager.Score : -1) +
            " | Distance: " +
            (scoreManager != null ? scoreManager.Distance : -1) +
            " | Coins: " +
            (gameManager != null ? gameManager.Coins : -1)
        );

        if (scoreManager == null)
        {
            Debug.LogError("[HUD] ScoreManager tidak ditemukan!");
            return;
        }

        if (finalScoreText != null)
            finalScoreText.text = scorePrefix + scoreManager.Score;
        else
            Debug.LogWarning("[HUD] Final Score Text belum terhubung!");

        if (finalDistanceText != null)
            finalDistanceText.text =
                Mathf.FloorToInt(scoreManager.Distance) + distanceSuffix;
        else
            Debug.LogWarning("[HUD] Final Distance Text belum terhubung!");

        if (finalCoinText != null)
            finalCoinText.text =
                coinPrefix + (gameManager != null ? gameManager.Coins : 0);
        else
            Debug.LogWarning("[HUD] Final Coin Text belum terhubung!");

        int best = Mathf.Max(
            scoreManager.BestScore,
            scoreManager.Score
        );

        if (bestScoreText != null)
            bestScoreText.text = "TERBAIK  " + best;
        else
            Debug.LogWarning("[HUD] Best Score Text belum terhubung!");

        if (topScoreText != null)
            topScoreText.text = compactHudMode ? best.ToString("N0") : topScorePrefix + best;
    }
}
