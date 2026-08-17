using System;
using UnityEngine;
using VRIQ.Timing;

namespace VRIQ.Sessions
{
    public sealed class VriqSession : MonoBehaviour
    {
        public static VriqSession Current { get; private set; }

        [Header("Session Information")]
        [SerializeField]
        private DataStage dataStage = DataStage.Test;

        [SerializeField]
        private string participantId = "TEST001";

        [SerializeField]
        private string conditionId = "baseline";

        [SerializeField]
        private bool autoStart = false;

        public event Action SessionStarted;
        public event Action SessionStopping;

        public string SessionId { get; private set; }
        public DataStage Stage => dataStage;
        public string ParticipantId => participantId;
        public string ConditionId => conditionId;

        public SessionClock Clock { get; private set; }
        public bool IsRunning { get; private set; }

        public void Configure(
            DataStage stage,
            string participant,
            string condition)
        {
            if (IsRunning)
            {
                Debug.LogWarning(
                    "[VRIQ] Cannot change session information while running."
                );
                return;
            }

            dataStage = stage;

            participantId = string.IsNullOrWhiteSpace(participant)
                ? "UNKNOWN"
                : participant.Trim();

            conditionId = string.IsNullOrWhiteSpace(condition)
                ? "default"
                : condition.Trim();
        }

        private void Awake()
        {
            if (Current != null && Current != this)
            {
                Destroy(gameObject);
                return;
            }

            Current = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (autoStart)
                StartSession();
        }

        public void StartSession()
        {
            if (IsRunning)
                return;

            SessionId =
                $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{participantId}";

            Clock = new SessionClock();
            IsRunning = true;

            Debug.Log("---------------Session started---------------");
            Debug.Log($"[VRIQ] Session ID: {SessionId}");
            Debug.Log($"[VRIQ] Data Stage: {dataStage}");
            Debug.Log($"[VRIQ] Participant: {participantId}");
            Debug.Log($"[VRIQ] Condition: {conditionId}");

            SessionStarted?.Invoke();
        }

        public void StopSession()
        {
            if (!IsRunning)
                return;

            // Recorder가 남은 데이터를 저장하고 파일을 닫게 한다.
            SessionStopping?.Invoke();

            long durationNs = Clock.NowNanoseconds;
            IsRunning = false;

            Debug.Log("---------------Session stopped---------------");
            Debug.Log($"[VRIQ] Duration: {durationNs} ns");
        }

        [ContextMenu("Print Current Session Time")]
        private void PrintCurrentSessionTime()
        {
            if (!IsRunning)
            {
                Debug.LogWarning("[VRIQ] Session is not running.");
                return;
            }

            Debug.Log(
                $"[VRIQ] Current time: {Clock.NowNanoseconds} ns"
            );
        }

        private void OnApplicationQuit()
        {
            StopSession();
        }
    }
}