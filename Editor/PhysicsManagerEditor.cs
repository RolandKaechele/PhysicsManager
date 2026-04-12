#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PhysicsManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="PhysicsManager.Runtime.PhysicsManager"/>.
    /// Adds runtime profile controls, pause/resume buttons, and collision matrix helpers.
    /// </summary>
    [CustomEditor(typeof(PhysicsManager.Runtime.PhysicsManager))]
    public class PhysicsManagerEditor : UnityEditor.Editor
    {
        private string _activateProfileId = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Open JSON Editor")) PhysicsJsonEditorWindow.ShowWindow();

            var mgr = (PhysicsManager.Runtime.PhysicsManager)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use runtime controls.", MessageType.Info);
                return;
            }

            // Status
            EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Active Profile", mgr.CurrentProfileId ?? "(none)");
            EditorGUILayout.TextField("Gravity", Physics.gravity.ToString("F2"));
            EditorGUILayout.Toggle("Physics Paused", mgr.IsPaused);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);

            // Pause / Resume
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(mgr.IsPaused);
            if (GUILayout.Button("Pause Physics"))  mgr.PausePhysics();
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(!mgr.IsPaused);
            if (GUILayout.Button("Resume Physics")) mgr.ResumePhysics();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Profile activation
            EditorGUILayout.LabelField("Activate Profile", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _activateProfileId = EditorGUILayout.TextField("Profile Id", _activateProfileId);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_activateProfileId));
            if (GUILayout.Button("Activate", GUILayout.Width(80)))
                mgr.ActivateProfile(_activateProfileId);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Profile list
            EditorGUILayout.LabelField("Registered Profiles", EditorStyles.miniBoldLabel);
            foreach (var id in mgr.GetAllProfileIds())
            {
                EditorGUILayout.BeginHorizontal();
                bool isCurrent = id == mgr.CurrentProfileId;
                EditorGUILayout.LabelField(isCurrent ? "▶ " + id : "  " + id);
                if (!isCurrent && GUILayout.Button("Use", GUILayout.Width(50)))
                    mgr.ActivateProfile(id);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif
