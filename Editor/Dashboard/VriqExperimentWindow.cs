using UnityEditor;
using UnityEngine;
using VRIQ.Recording;
using VRIQ.Sessions;

namespace VRIQ.Editor
{
    public sealed class VriqExperimentWindow : EditorWindow
    {
        private DataStage _dataStage = DataStage.Test;
        private string _participantId = "TEST001";
        private string _conditionId = "baseline";

        [MenuItem("VRIQ/Experiment Dashboard")]
        private static void OpenWindow()
        {
            var window =
                GetWindow<VriqExperimentWindow>();

            window.titleContent =
                new GUIContent("VRIQ Dashboard");

            window.minSize = new Vector2(380f, 400f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += HandleEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= HandleEditorUpdate;
        }

        private void HandleEditorUpdate()
        {
            if (EditorApplication.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(
                "VRIQ Experiment Dashboard",
                EditorStyles.boldLabel
            );

            EditorGUILayout.Space(10);

            VriqSession session = FindSession();

            bool isRunning =
                session != null &&
                session.IsRunning;

            using (new EditorGUI.DisabledScope(isRunning))
            {
                _dataStage = (DataStage)
                    EditorGUILayout.EnumPopup(
                        "Data Stage",
                        _dataStage
                    );

                _participantId =
                    EditorGUILayout.TextField(
                        "Participant ID",
                        _participantId
                    );

                _conditionId =
                    EditorGUILayout.TextField(
                        "Condition ID",
                        _conditionId
                    );
            }

            EditorGUILayout.Space(15);

            DrawStatus(session);

            EditorGUILayout.Space(15);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Unity Play 버튼을 누른 후 실험을 시작할 수 있습니다.",
                    MessageType.Info
                );

                return;
            }

            if (session == null)
            {
                EditorGUILayout.HelpBox(
                    "현재 씬에서 VriqSession을 찾을 수 없습니다.",
                    MessageType.Error
                );

                return;
            }

            DrawControlButtons(session);
            DrawSessionInformation(session);
            DrawFolderButton();
        }

        private void DrawStatus(VriqSession session)
        {
            string status;

            if (!EditorApplication.isPlaying)
                status = "EDIT MODE";
            else if (session == null)
                status = "SESSION NOT FOUND";
            else if (session.IsRunning)
                status = "RECORDING";
            else
                status = "READY";

            EditorGUILayout.LabelField(
                "Status",
                status
            );
        }

        private void DrawControlButtons(
            VriqSession session)
        {
            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(
                       session.IsRunning))
            {
                if (GUILayout.Button(
                        "START",
                        GUILayout.Height(40)))
                {
                    session.Configure(
                        _dataStage,
                        _participantId,
                        _conditionId
                    );

                    session.StartSession();
                }
            }

            using (new EditorGUI.DisabledScope(
                       !session.IsRunning))
            {
                if (GUILayout.Button(
                        "STOP",
                        GUILayout.Height(40)))
                {
                    session.StopSession();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSessionInformation(
            VriqSession session)
        {
            EditorGUILayout.Space(15);

            EditorGUILayout.LabelField(
                "Current Session",
                EditorStyles.boldLabel
            );

            EditorGUILayout.LabelField(
                "Session ID",
                string.IsNullOrWhiteSpace(session.SessionId)
                    ? "-"
                    : session.SessionId
            );

            EditorGUILayout.LabelField(
                "Participant",
                session.ParticipantId
            );

            EditorGUILayout.LabelField(
                "Condition",
                session.ConditionId
            );

            string elapsed =
                session.IsRunning &&
                session.Clock != null
                    ? $"{session.Clock.NowSeconds:F2} s"
                    : "-";

            EditorGUILayout.LabelField(
                "Elapsed Time",
                elapsed
            );
        }

        private static void DrawFolderButton()
        {
            SessionRecorder recorder =
                Object.FindFirstObjectByType<
                    SessionRecorder>();

            if (recorder == null ||
                string.IsNullOrWhiteSpace(
                    recorder.SessionDirectory))
            {
                return;
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button(
                    "Open Session Folder",
                    GUILayout.Height(28)))
            {
                EditorUtility.RevealInFinder(
                    recorder.SessionDirectory
                );
            }
        }

        private static VriqSession FindSession()
        {
            return Object.FindFirstObjectByType<
                VriqSession>();
        }
    }
}