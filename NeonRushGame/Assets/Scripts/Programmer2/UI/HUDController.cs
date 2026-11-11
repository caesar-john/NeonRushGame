using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text coinText;

    [Header("Game Over")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalDistanceText;
    [SerializeField] private TMP_Text finalCoinText;
    [SerializeField] private TMP_Text bestScoreText;

    [Header("Format")]
    [SerializeField] private string scorePrefix = "SKOR  ";
    [SerializeField] private string distanceSuffix = " m";
    [SerializeField] private string coinPrefix = "KOIN  x";

    private GameManager gameManager;
    private ScoreManager scoreManager;
    private bool subscribed;

    private void Start()
    {
        gameManager = GameManager.Instance;
        scoreManager = gameManager != null ? gameManager.ScoreSystem : FindObjectOfType<ScoreManager>();
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
        }

        subscribed = false;
    }

    private void RefreshAll()
    {
        if (scoreManager != null)
        {
            OnScoreChanged(scoreManager.Score);
            OnDistanceChanged(Mathf.FloorToInt(scoreManager.Distance));
        }

        if (gameManager != null) OnCoinsChanged(gameManager.Coins);
    }

    private void OnScoreChanged(int score)
    {
        if (scoreText != null) scoreText.text = scorePrefix + score;
    }

    private void OnDistanceChanged(int meters)
    {
        if (distanceText != null) distanceText.text = meters + distanceSuffix;
    }

    private void OnCoinsChanged(int coins)
    {
        if (coinText != null) coinText.text = coinPrefix + coins;
    }

    private void OnRunEnded()
    {
        if (scoreManager == null) return;

        if (finalScoreText != null) finalScoreText.text = scorePrefix + scoreManager.Score;
        if (finalDistanceText != null) finalDistanceText.text = Mathf.FloorToInt(scoreManager.Distance) + distanceSuffix;
        if (finalCoinText != null) finalCoinText.text = coinPrefix + (gameManager != null ? gameManager.Coins : 0);

        if (bestScoreText != null)
        {
            int best = Mathf.Max(scoreManager.BestScore, scoreManager.Score);
            bestScoreText.text = "TERBAIK  " + best;
        }
    }
}
