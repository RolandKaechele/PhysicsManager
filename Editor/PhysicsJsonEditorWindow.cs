#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using PhysicsManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace PhysicsManager.Editor
{
    // ────────────────────────────────────────────────────────────────────────────
    // Physics JSON Editor Window
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Editor window for creating and editing <c>physics_profiles.json</c> and <c>collision_rules.json</c> in StreamingAssets.
    /// Profiles and collision rules are stored in separate files.
    /// Open via <b>JSON Editors → Physics Manager</b> or via the Manager Inspector button.
    /// </summary>
    public class PhysicsJsonEditorWindow : EditorWindow
    {
        private const string ProfilesFolderName   = "physics_profiles";
        private const string ProfilesSaveFileName = "physics_profiles.json";
        private const string CollisionFolderName   = "collision_rules";
        private const string CollisionSaveFileName = "collision_rules.json";

        private PhysicsDataEditorBridge  _bridge;
        private UnityEditor.Editor       _bridgeEditor;
        private Vector2                  _scroll;
        private string                   _status;
        private bool                     _statusError;

        [MenuItem("JSON Editors/Physics Manager")]
        public static void ShowWindow() =>
            GetWindow<PhysicsJsonEditorWindow>("Physics JSON");

        private void OnEnable()
        {
            _bridge = CreateInstance<PhysicsDataEditorBridge>();
            Load();
        }

        private void OnDisable()
        {
            if (_bridgeEditor != null) DestroyImmediate(_bridgeEditor);
            if (_bridge      != null) DestroyImmediate(_bridge);
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusError ? MessageType.Error : MessageType.Info);

            if (_bridge == null) return;
            if (_bridgeEditor == null)
                _bridgeEditor = UnityEditor.Editor.CreateEditor(_bridge);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _bridgeEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(
                $"StreamingAssets/{ProfilesFolderName}/ + {CollisionFolderName}/",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50))) Load();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) Save();
            EditorGUILayout.EndHorizontal();
        }

        private void Load()
        {
            string profilesFolderPath  = Path.Combine(Application.streamingAssetsPath, ProfilesFolderName);
            string collisionFolderPath = Path.Combine(Application.streamingAssetsPath, CollisionFolderName);
            try
            {
                var profileList = new List<PhysicsProfile>();
                if (Directory.Exists(profilesFolderPath))
                {
                    foreach (var file in Directory.GetFiles(profilesFolderPath, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var pw = JsonUtility.FromJson<PhysicsProfilesEditorWrapper>(File.ReadAllText(file));
                        if (pw?.profiles != null) profileList.AddRange(pw.profiles);
                    }
                }
                else
                {
                    Directory.CreateDirectory(profilesFolderPath);
                    File.WriteAllText(Path.Combine(profilesFolderPath, ProfilesSaveFileName), JsonUtility.ToJson(new PhysicsProfilesEditorWrapper(), true));
                    AssetDatabase.Refresh();
                }

                var collisionList = new List<CollisionLayerRule>();
                if (Directory.Exists(collisionFolderPath))
                {
                    foreach (var file in Directory.GetFiles(collisionFolderPath, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var cw = JsonUtility.FromJson<CollisionRulesEditorWrapper>(File.ReadAllText(file));
                        if (cw?.collisionRules != null) collisionList.AddRange(cw.collisionRules);
                    }
                }
                else
                {
                    Directory.CreateDirectory(collisionFolderPath);
                    File.WriteAllText(Path.Combine(collisionFolderPath, CollisionSaveFileName), JsonUtility.ToJson(new CollisionRulesEditorWrapper(), true));
                    AssetDatabase.Refresh();
                }

                _bridge.profiles       = profileList;
                _bridge.collisionRules = collisionList;
                if (_bridgeEditor != null) { DestroyImmediate(_bridgeEditor); _bridgeEditor = null; }
                _status = $"Loaded {profileList.Count} profiles from {ProfilesFolderName}/, {collisionList.Count} rules from {CollisionFolderName}/.";
                _statusError = false;
            }
            catch (Exception e) { _status = $"Load error: {e.Message}"; _statusError = true; }
        }

        private void Save()
        {
            try
            {
                string profilesFolderPath  = Path.Combine(Application.streamingAssetsPath, ProfilesFolderName);
                string collisionFolderPath = Path.Combine(Application.streamingAssetsPath, CollisionFolderName);
                if (!Directory.Exists(profilesFolderPath))  Directory.CreateDirectory(profilesFolderPath);
                if (!Directory.Exists(collisionFolderPath)) Directory.CreateDirectory(collisionFolderPath);

                var pw = new PhysicsProfilesEditorWrapper { profiles = _bridge.profiles.ToArray() };
                File.WriteAllText(Path.Combine(profilesFolderPath, ProfilesSaveFileName), JsonUtility.ToJson(pw, true));

                var cw = new CollisionRulesEditorWrapper { collisionRules = _bridge.collisionRules.ToArray() };
                File.WriteAllText(Path.Combine(collisionFolderPath, CollisionSaveFileName), JsonUtility.ToJson(cw, true));

                AssetDatabase.Refresh();
                _status = $"Saved {_bridge.profiles.Count} profiles to {ProfilesFolderName}/{ProfilesSaveFileName}, {_bridge.collisionRules.Count} rules to {CollisionFolderName}/{CollisionSaveFileName}.";
                _statusError = false;
            }
            catch (Exception e) { _status = $"Save error: {e.Message}"; _statusError = true; }
        }
    }

    // ── ScriptableObject bridge ──────────────────────────────────────────────
    internal class PhysicsDataEditorBridge : ScriptableObject
    {
        public List<PhysicsProfile>      profiles       = new List<PhysicsProfile>();
        public List<CollisionLayerRule>  collisionRules = new List<CollisionLayerRule>();
    }

    // ── Local wrappers mirror the private physics JSON root classes ──────────
    [Serializable]
    internal class PhysicsProfilesEditorWrapper
    {
        public PhysicsProfile[] profiles = Array.Empty<PhysicsProfile>();
    }

    [Serializable]
    internal class CollisionRulesEditorWrapper
    {
        public CollisionLayerRule[] collisionRules = Array.Empty<CollisionLayerRule>();
    }
}
#endif
