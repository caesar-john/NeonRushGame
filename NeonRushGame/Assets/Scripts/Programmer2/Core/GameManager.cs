using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver
}

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referensi")]
    [Tooltip("Transform karakter. Jika kosong, dicari otomatis lewat tag Player.")]
    [SerializeField] private Transform player;
    [SerializeField] private DifficultyManager difficulty;
    [SerializeField] private ScoreManager scoreManager;

    [Header("Pengaturan Awal")]
    [SerializeField] private GameState startState = GameState.Menu;
    [SerializeField] private float fallbackSpeed = 8f;

    [Header("Waktu")]
    [SerializeField] private bool pauseStopsTime = true;
    [SerializeField] private bool gameOverStopsTime = true;

    public GameState State { get; private set; } = GameState.Menu;
    public Transform Player => player;
    public DifficultyManager Difficulty => difficulty;
    public ScoreManager ScoreSystem => scoreManager;
    public float Speed => difficulty != null ? difficulty.CurrentSpeed : fallbackSpeed;
    public int Coins { get; private set; }
    public int Score => scoreManager != null ? scoreManager.Score : 0;
    public float Distance => scoreManager != null ? scoreManager.Distance : 0f;
    public bool IsPlaying => State == GameState.Playing;
    public bool IsRunning => State == GameState.Playing || State == GameState.Paused;

    public event Action<GameState> StateChanged;
    public event Action<int> CoinsChanged;
    public event Action RunStarted;
    public event Action RunEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
        if (difficulty == null) difficulty = GetComponentInChildren<DifficultyManager>();
        if (difficulty == null) difficulty = FindObjectOfType<DifficultyManager>();
        if (scoreManager == null) scoreManager = FindObjectOfType<ScoreManager>();
    }

    private void Start()
    {
        State = startState;
        ApplyTimeScale();
        StateChanged?.Invoke(State);
        if (State == GameState.Playing) RunStarted?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void StartGame()
    {
        if (State == GameState.Playing) return;

        Coins = 0;
        CoinsChanged?.Invoke(Coins);
        SetState(GameState.Playing);
        RunStarted?.Invoke();
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        SetState(GameState.Playing);
    }

    public void EndGame()
    {
        if (State != GameState.Playing && State != GameState.Paused) return;
        SetState(GameState.GameOver);
        RunEnded?.Invoke();
    }

    public void CollectCoin(int amount)
    {
        if (!IsPlaying || amount <= 0) return;
        Coins += amount;
        CoinsChanged?.Invoke(Coins);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
        else SceneManager.LoadScene(scene.name);
    }

    private void SetState(GameState next)
    {
        if (State == next) return;
        State = next;
        ApplyTimeScale();
        StateChanged?.Invoke(State);
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = 1f;
        if (State == GameState.Paused && pauseStopsTime) Time.timeScale = 0f;
        else if (State == GameState.GameOver && gameOverStopsTime) Time.timeScale = 0f;
    }
}
