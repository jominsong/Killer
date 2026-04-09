using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 라운드 설정 데이터
[Serializable]
public class RoundData
{
    [Tooltip("라운드 제한 시간(초)")]
    public float roundDuration = 60f;

    [Tooltip("동시에 존재할 수 있는 최대 적 수")]
    public int maxEnemiesAlive = 10;

    [Tooltip("적 생성 주기 (초)")]
    public float spawnInterval = 2f;

    [Tooltip("한 번에 생성되는 적 수")]
    public int spawnCountPerTick = 1;

    [Tooltip("스폰 포인트 등장 후 실제 적이 나타나기까지의 딜레이 (초)")]
    public float spawnLatency = 1.2f;
}

// 라운드 상태
public enum RoundPhase
{
    WaitingToStart,   // 라운드 시작 전
    InProgress,       // 라운드 진행 중 (스폰 활성)
    TimeUp,           // 제한시간 종료 (스폰 중단, 문 활성화)
    Complete          // 플레이어가 문을 통과해 라운드 완전 종료
}

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance {  get; private set; }

    [Header("Round Configuration")]
    [SerializeField]
    private List<RoundData> rounds = new List<RoundData>();
    [SerializeField]
    private bool loopLastRound = true;

    [Header("debug")]
    [SerializeField]
    private bool autoStartOnAwake = true;

    // 라운드 시작
    public event Action<int, RoundData> OnRoundStarted;
    // 제한시간 종료 문 열림
    public event Action<int> OnRoundTimeUp;
    // 플레이어가 문 통과시 라운드 종료
    public event Action<int> OnRoundCompleted;
    // 적 카운트 변경
    public event Action<int> OnEnemyCountChanged;
    // 남은 시간 변경
    public event Action<float> OnTimerTick; 

    public RoundPhase Phase { get; private set; } = RoundPhase.WaitingToStart;
    public int CurrentRoundIndex { get; private set; } = 0;
    public float TimeRemaining { get; private set; }
    public int AliveEnemyCount { get; private set; }

    public RoundData CurrentRoundData =>
        rounds.Count > 0
            ? rounds[Mathf.Clamp(CurrentRoundIndex, 0 , rounds.Count - 1)] : null;
    
    public bool IsSpawningAllowed => 
        Phase == RoundPhase.InProgress && AliveEnemyCount < CurrentRoundData.maxEnemiesAlive;

    private Coroutine timerCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 기본 라운드 데이터가 없으면 1개 자동 생성
        if (rounds.Count == 0)
            rounds.Add(new RoundData());
    }

    private void Start()
    {
        if (autoStartOnAwake)
        {
            MapManager.Instance.LoadRandomMap();

            // 플레이어를 PlayerSpawnPoint로 이동
            Transform spawnPoint = MapManager.Instance.CurrentPlayerSpawnPoint;
            if (spawnPoint != null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    CharacterController cc = player.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
                    if (cc != null) cc.enabled = true;
                }
            }

            StartRound(0);
        }
    }

    public void StartRound(int roundIndex)
    {
        CurrentRoundIndex = loopLastRound
            ? Mathf.Min(roundIndex, rounds.Count - 1)
            : roundIndex;

        if (CurrentRoundIndex >= rounds.Count)
        {
            Debug.Log("[RoundManager] 모든 라운드 완료!");
            return;
        }

        Phase = RoundPhase.InProgress;
        TimeRemaining = CurrentRoundData.roundDuration;
        AliveEnemyCount = 0;

        OnRoundStarted?.Invoke(CurrentRoundIndex, CurrentRoundData);

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    // 플레이어가 출구 문을 통과 했을 떄 호출
    public void PlayerExitedRound()
    {
        if (Phase != RoundPhase.TimeUp) return;

        Phase = RoundPhase.Complete;
        OnRoundCompleted?.Invoke(CurrentRoundIndex);
    }

    // EnemyMemoryPool이 적을 활성화 헀을때 호출
    public void RegisterEnemySpawned()
    {
        AliveEnemyCount++;
        OnEnemyCountChanged?.Invoke(AliveEnemyCount);
    }

    // EnemyMemoryPool이 적을 비활성화 했을때 호출
    public void RegisterEnemyDeactivated()
    {
        AliveEnemyCount = Mathf.Max(0, AliveEnemyCount - 1);
        OnEnemyCountChanged?.Invoke(AliveEnemyCount);
    }

    private IEnumerator TimerRoutine()
    {
        float nextTickTime = Time.time + 1f;

        while (TimeRemaining > 0f)
        {
            yield return null;
            TimeRemaining -= Time.deltaTime;

            if (Time.time >= nextTickTime)
            {
                nextTickTime += 1f;
                OnTimerTick?.Invoke(Mathf.Max(0f, TimeRemaining));
            }
        }

        TimeRemaining = 0f;
        Phase = RoundPhase.TimeUp;
        OnRoundTimeUp?.Invoke(CurrentRoundIndex);

        Debug.Log($"[RoundManager] 라운드 {CurrentRoundIndex + 1} 시간 종료 → 출구 문 활성화");
    }
}
