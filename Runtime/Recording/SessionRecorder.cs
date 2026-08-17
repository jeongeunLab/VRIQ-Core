using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using VRIQ.Data;
using VRIQ.Sessions;

namespace VRIQ.Recording
{
    [RequireComponent(typeof(VriqSession))]
    public sealed class SessionRecorder : MonoBehaviour
    {
        [Serializable]
        private sealed class SessionManifest
        {
            public string schemaVersion;
            public string dataStage;
            public string sessionId;
            public string participantId;
            public string conditionId;
            public string utcStart;
            public string unityVersion;
            public string applicationVersion;
            public string platform;
        }

        private readonly struct QueuedRecord
        {
            public readonly string StreamName;
            public readonly string Json;

            public QueuedRecord(string streamName, string json)
            {
                StreamName = streamName;
                Json = json;
            }
        }

        [Header("Recording")]
        [SerializeField]
        private string rootFolderName = "VRIQData";

        [SerializeField]
        private int maxRecordsPerFrame = 2048;

        private readonly ConcurrentQueue<QueuedRecord> _queue =
            new ConcurrentQueue<QueuedRecord>();

        private readonly Dictionary<string, StreamWriter> _writers =
            new Dictionary<string, StreamWriter>();

        private VriqSession _session;

        public string SessionDirectory { get; private set; }
        public bool IsRecording { get; private set; }

        private void Awake()
        {
            _session = GetComponent<VriqSession>();

            _session.SessionStarted += HandleSessionStarted;
            _session.SessionStopping += HandleSessionStopping;
        }

        private void Update()
        {
            if (IsRecording)
                DrainQueue(maxRecordsPerFrame);
        }

        private void HandleSessionStarted()
        {
            string stageFolder =
                _session.Stage
                    .ToString()
                    .ToUpperInvariant();

            SessionDirectory = Path.Combine(
                Application.persistentDataPath,
                rootFolderName,
                stageFolder,
                _session.SessionId
            );

            Directory.CreateDirectory(SessionDirectory);

            WriteManifest();

            IsRecording = true;

            Debug.Log(
                $"[VRIQ] Recording directory:\n{SessionDirectory}"
            );
        }

        private void WriteManifest()
        {
            var manifest = new SessionManifest
            {
                schemaVersion = "0.2.0",
                dataStage = _session.Stage.ToString(),
                sessionId = _session.SessionId,
                participantId = _session.ParticipantId,
                conditionId = _session.ConditionId,
                utcStart = _session.Clock.UtcStart.ToString("O"),
                unityVersion = Application.unityVersion,
                applicationVersion = Application.version,
                platform = Application.platform.ToString()
            };

            string json = JsonUtility.ToJson(manifest, true);
            string path = Path.Combine(
                SessionDirectory,
                "manifest.json"
            );

            File.WriteAllText(
                path,
                json,
                new UTF8Encoding(false)
            );
        }

        public void RecordEvent(
            string eventType,
            string interactionId = "",
            string phase = "",
            string targetId = "",
            string actionId = "",
            string outcomeId = "")
        {
            if (!IsRecording || !_session.IsRunning)
            {
                Debug.LogWarning(
                    "[VRIQ] Cannot record event: " +
                    "session is not recording."
                );
                return;
            }

            var record = new InteractionEventRecord
            {
                sessionTimeNs =
                    _session.Clock.NowNanoseconds,

                frameIndex = Time.frameCount,
                eventType = eventType,
                interactionId = interactionId,
                phase = phase,
                targetId = targetId,
                actionId = actionId,
                outcomeId = outcomeId
            };

            EnqueueJson(
                "events",
                JsonUtility.ToJson(record)
            );
        }

        public void EnqueueJson(
            string streamName,
            string json)
        {
            if (!IsRecording)
                return;

            _queue.Enqueue(
                new QueuedRecord(streamName, json)
            );
        }

        private void DrainQueue(int maximum)
        {
            int count = 0;

            while (
                count < maximum &&
                _queue.TryDequeue(out QueuedRecord record))
            {
                StreamWriter writer =
                    GetOrCreateWriter(record.StreamName);

                writer.WriteLine(record.Json);
                count++;
            }
        }

        private StreamWriter GetOrCreateWriter(
            string streamName)
        {
            string safeName = MakeSafeFileName(streamName);

            if (_writers.TryGetValue(
                safeName,
                out StreamWriter existing))
            {
                return existing;
            }

            string path = Path.Combine(
                SessionDirectory,
                $"{safeName}.jsonl"
            );

            var writer = new StreamWriter(
                path,
                false,
                new UTF8Encoding(false)
            );

            _writers.Add(safeName, writer);
            return writer;
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            foreach (char invalid in
                     Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            value = value
                .Replace('/', '_')
                .Replace('\\', '_');

            return value;
        }

        private void HandleSessionStopping()
        {
            if (!IsRecording)
                return;

            DrainQueue(int.MaxValue);

            foreach (StreamWriter writer in _writers.Values)
            {
                writer.Flush();
                writer.Dispose();
            }

            _writers.Clear();
            IsRecording = false;

            Debug.Log("[VRIQ] Recording files saved.");
        }


        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.SessionStarted -=
                    HandleSessionStarted;

                _session.SessionStopping -=
                    HandleSessionStopping;
            }

            if (IsRecording)
                HandleSessionStopping();
        }
    }
}