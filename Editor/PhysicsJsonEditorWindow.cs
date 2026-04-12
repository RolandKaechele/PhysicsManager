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
    /// Editor window for creating and editing <c>physics.json</c> in StreamingAssets.
    /// Contains both physics profiles and collision layer rules.
    /// Open via <b>JSON Editors → Physics Manager</b> or via the Manager Inspector button.
    /// </summary>
    public class PhysicsJsonEditorWindow : EditorWindow
    {
        private const string JsonFileName = "physics.json";

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
                Path.Combine("StreamingAssets", JsonFileName),
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50))) Load();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) Save();
            EditorGUILayout.EndHorizontal();
        }

        private void Load()
        {
            var path = Path.Combine(Application.streamingAssetsPath, JsonFileName);
            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, JsonUtility.ToJson(new PhysicsDataEditorWrapper(), true));
                    AssetDatabase.Refresh();
                }

                var w = JsonUtility.FromJson<PhysicsDataEditorWrapper>(File.ReadAllText(path));
                _bridge.profiles       = new List<PhysicsProfile>(w.profiles       ?? Array.Empty<PhysicsProfile>());
                _bridge.collisionRules = new List<CollisionLayerRule>(w.collisionRules ?? Array.Empty<CollisionLayerRule>());

                if (_bridgeEditor != null) { DestroyImmediate(_bridgeEditor); _bridgeEditor = null; }

                _status     = $"Loaded {_bridge.profiles.Count} profiles and {_bridge.collisionRules.Count} collision rules.";
                _statusError = false;
            }
            catch (Exception e)
            {
                _status     = $"Load error: {e.Message}";
                _statusError = true;
            }
        }

        private void Save()
        {
            try
            {
                var w = new PhysicsDataEditorWrapper
                {
                    profiles       = _bridge.profiles.ToArray(),
                    collisionRules = _bridge.collisionRules.ToArray()
                };
                var path = Path.Combine(Application.streamingAssetsPath, JsonFileName);
                File.WriteAllText(path, JsonUtility.ToJson(w, true));
                AssetDatabase.Refresh();
                _status     = $"Saved {_bridge.profiles.Count} profiles and {_bridge.collisionRules.Count} rules to {JsonFileName}.";
                _statusError = false;
            }
            catch (Exception e)
            {
                _status     = $"Save error: {e.Message}";
                _statusError = true;
            }
        }
    }

    // ── ScriptableObject bridge ──────────────────────────────────────────────
    internal class PhysicsDataEditorBridge : ScriptableObject
    {
        public List<PhysicsProfile>      profiles       = new List<PhysicsProfile>();
        public List<CollisionLayerRule>  collisionRules = new List<CollisionLayerRule>();
    }

    // ── Local wrapper mirrors the private PhysicsDataRoot ────────────────────
    [Serializable]
    internal class PhysicsDataEditorWrapper
    {
        public PhysicsProfile[]     profiles       = Array.Empty<PhysicsProfile>();
        public CollisionLayerRule[] collisionRules = Array.Empty<CollisionLayerRule>();
    }
}
#endif
