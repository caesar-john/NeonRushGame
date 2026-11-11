using System;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class ScoreManager : MonoBehaviour
{
    [Header("Aturan Skor")]
    [SerializeField] private float scorePerMeter = 1f;
    [SerializeField] private int pointsPerCoin = 10;

    [Header("Penyimpanan")]
    [SerializeField] private string bestScoreKey = "NeonRush_BestScore";

    public int Score { get; private set; }
    public float Distance { get; private set; }
    public int BestScore { get; private set; }

    public event Action<int> ScoreChanged;
    public event Action<int> DistanceChanged;
    public event Action<int> BestScoreChanged;

    private Transform player;
    private float startZ;
    private int lastDisplayedDistance = -1;
    private bool subscribed;

    private void Awake()
    {
        BestScore = PlayerPrefs.GetInt(bestScoreKey, 0);
        Subscribe();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDestroy()
    {
        if (!subscribed) return;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RunStarted -= OnRunStarted;
            GameManager.Instance.RunEnded -= OnRunEnded;
            GameManager.Instance.CoinsChanged -= OnCoinsChanged;
        }
        subscribed = false;
    }

    private void Subscribe()
    {
        if (subscribed || GameManager.Instance == null) return;
        GameManager.Instance.RunStarted += OnRunStarted;
        GameManager.Instance.RunEnded += OnRunEnded;
        GameManager.Instance.CoinsChanged += OnCoinsChanged;
        if (player == null) player = GameManager.Instance.Player;
        subscribed = true;
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (player == null)
        {
            player = GameManager.Instance.Player;
            if (player == null) return;
        }

        Distance = Mathf.Max(0f, player.position.z - startZ);

        int meters = Mathf.FloorToInt(Distance);
        if (meters != lastDisplayedDistance)
        {
            lastDisplayedDistance = meters;
            DistanceChanged?.Invoke(meters);
        }

        RecalculateScore();
    }

    private void OnRunStarted()
    {
        if (GameManager.Instance != null) player = GameManager.Instance.Player;
        startZ = player != null ? player.position.z : 0f;
        Distance = 0f;
        Score = 0;
        lastDisplayedDistance = 0;
        ScoreChanged?.Invoke(0);
        DistanceChanged?.Invoke(0);
    }

    private void OnCoinsChanged(int coins)
    {
        RecalculateScore();
    }

    private void OnRunEnded()
    {
        if (Score <= BestScore) return;
        BestScore = Score;
        PlayerPrefs.SetInt(bestScoreKey, BestScore);
        PlayerPrefs.Save();
        BestScoreChanged?.Invoke(BestScore);
    }

    private void RecalculateScore()
    {
        int coins = GameManager.Instance != null ? GameManager.Instance.Coins : 0;
        int newScore = Mathf.FloorToInt(Distance * scorePerMeter) + coins * pointsPerCoin;

        if (newScore == Score) return;
        Score = newScore;
        ScoreChanged?.Invoke(Score);
    }
}
