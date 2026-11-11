using System;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class DifficultyManager : MonoBehaviour
{
    [Header("Kecepatan")]
    [SerializeField] private float startSpeed = 8f;
    [SerializeField] private float maxSpeed = 24f;
    [Tooltip("Kenaikan kecepatan per meter jarak tempuh.")]
    [SerializeField] private float speedIncreasePerMeter = 0.02f;

    [Header("Referensi")]
    [SerializeField] private Transform player;

    public float CurrentSpeed { get; private set; }
    public float RunDistance { get; private set; }

    public event Action<float> SpeedChanged;

    private float startZ;
    private bool subscribed;

    private void Awake()
    {
        CurrentSpeed = startSpeed;
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (!subscribed) return;
        if (GameManager.Instance != null) GameManager.Instance.RunStarted -= OnRunStarted;
        subscribed = false;
    }

    private void Start()
    {
        Subscribe();
    }

    private void Subscribe()
    {
        if (subscribed || GameManager.Instance == null) return;
        GameManager.Instance.RunStarted += OnRunStarted;
        subscribed = true;
        if (player == null) player = GameManager.Instance.Player;
    }

    private void OnRunStarted()
    {
        if (player == null && GameManager.Instance != null) player = GameManager.Instance.Player;
        startZ = player != null ? player.position.z : 0f;
        RunDistance = 0f;
        CurrentSpeed = startSpeed;
        SpeedChanged?.Invoke(CurrentSpeed);
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (player == null) return;

        RunDistance = Mathf.Max(0f, player.position.z - startZ);
        float target = Mathf.Min(maxSpeed, startSpeed + speedIncreasePerMeter * RunDistance);

        if (!Mathf.Approximately(target, CurrentSpeed))
        {
            CurrentSpeed = target;
            SpeedChanged?.Invoke(CurrentSpeed);
        }
    }
}
