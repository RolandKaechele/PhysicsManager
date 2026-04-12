using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PhysicsManager.Runtime
{
    /// <summary>
    /// <b>PhysicsManager</b> is the central controller for global physics simulation settings,
    /// collision layer rules, and physics profile switching.
    ///
    /// <para><b>Responsibilities:</b>
    /// <list type="number">
    ///   <item>Store and apply named <see cref="PhysicsProfile"/> entries (gravity, timestep, simulation mode).</item>
    ///   <item>Apply <see cref="CollisionLayerRule"/> lists to Unity's layer collision matrix.</item>
    ///   <item>Pause and resume physics simulation (e.g. during cutscenes or loading).</item>
    ///   <item>Broadcast global impact events for camera shake and game-logic triggers.</item>
    ///   <item>Load profiles and collision rules from JSON for modding.</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Modding / JSON:</b> Enable <c>loadFromJson</c> and place a
    /// <c>physics.json</c> in <c>StreamingAssets/</c>.
    /// JSON entries are <b>merged by id</b>: JSON overrides Inspector entries with the same id.</para>
    ///
    /// <para><b>Optional integration defines:</b>
    /// <list type="bullet">
    ///   <item><c>PHYSICSMANAGER_STM</c>  — StateManager: auto-pause physics during Cutscene/Loading states.</item>
    ///   <item><c>PHYSICSMANAGER_EM</c>   — EventManager: fire <c>physics.impact</c> and <c>physics.profile.changed</c> events.</item>
    ///   <item><c>PHYSICSMANAGER_MLF</c>  — MapLoaderFramework: switch to the map-defined physics profile on chapter load.</item>
    ///   <item><c>PHYSICSMANAGER_CAM</c>  — CameraManager: trigger camera shake on significant impacts.</item>
    ///   <item><c>PHYSICSMANAGER_DOTWEEN</c> — DOTween Pro: tween gravity and time-scale for slow-motion effects.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("PhysicsManager/Physics Manager")]
    [DisallowMultipleComponent]
#if ODIN_INSPECTOR
    public class PhysicsManager : SerializedMonoBehaviour
#else
    public class PhysicsManager : MonoBehaviour
#endif
    {
        // ─── Inspector ───────────────────────────────────────────────────────────

        [Header("Profiles")]
        [Tooltip("Built-in physics profiles. JSON entries are merged on top by id.")]
        [SerializeField] private List<PhysicsProfile> profiles = new List<PhysicsProfile>();

        [Tooltip("Profile id to activate on Awake.")]
        [SerializeField] private string initialProfileId = "default";

        [Header("Collision Rules")]
        [Tooltip("Layer collision rules applied on Awake. JSON rules are appended.")]
        [SerializeField] private List<CollisionLayerRule> collisionRules = new List<CollisionLayerRule>();

        [Header("Impact Detection")]
        [Tooltip("Minimum collision impulse required to fire OnImpact event.")]
        [SerializeField] private float impactThreshold = 5f;

        [Header("Modding / JSON")]
        [Tooltip("Merge additional profiles from StreamingAssets/<jsonPath> at startup.")]
        [SerializeField] private bool loadFromJson = false;

        [Tooltip("Path relative to StreamingAssets/ for physics profiles (e.g. 'physics_profiles/' or 'physics_profiles.json').")]
        [SerializeField] private string jsonPath = "physics_profiles/";

        [Tooltip("Path relative to StreamingAssets/ for collision rules (e.g. 'collision_rules/' or 'collision_rules.json').")]
        [SerializeField] private string collisionJsonPath = "collision_rules/";

        [Header("Debug")]
        [Tooltip("Log profile switches and collision rule changes to the Console.")]
        [SerializeField] private bool verboseLogging = false;

        // ─── Events ──────────────────────────────────────────────────────────────

        /// <summary>Fired when an impact above <see cref="impactThreshold"/> is detected.</summary>
        public event Action<ImpactData> OnImpact;

        /// <summary>Fired when the active physics profile changes. Parameters: (previousId, newId).</summary>
        public event Action<string, string> OnProfileChanged;

        /// <summary>Fired when physics simulation is paused. </summary>
        public event Action OnPhysicsPaused;

        /// <summary>Fired when physics simulation is resumed.</summary>
        public event Action OnPhysicsResumed;

        // ─── Delegate hooks ──────────────────────────────────────────────────────

        /// <summary>
        /// Invoked when activating a profile, allowing bridge components to apply
        /// additional tween-based effects (e.g. slow-motion gravity ramp).
        /// Signature: (profile, onComplete). If set, called alongside (not instead of) the default apply.
        /// </summary>
        public Action<PhysicsProfile, Action> ProfileActivatedOverride;

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly Dictionary<string, PhysicsProfile> _profileIndex =
            new Dictionary<string, PhysicsProfile>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Currently active profile id.</summary>
        public string CurrentProfileId { get; private set; }

        /// <summary>Returns the currently active profile, or null.</summary>
        public PhysicsProfile CurrentProfile =>
            _profileIndex.TryGetValue(CurrentProfileId ?? "", out var p) ? p : null;

        /// <summary>True while physics simulation is paused.</summary>
        public bool IsPaused { get; private set; }

        // ─── Unity lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            BuildIndex();
            if (loadFromJson) LoadJsonDefinitions();
            ApplyCollisionRules(collisionRules);
            if (!string.IsNullOrEmpty(initialProfileId))
                ActivateProfile(initialProfileId);
        }

        // ─── Profile API ──────────────────────────────────────────────────────────

        /// <summary>Activate a physics profile by id, applying all its settings immediately.</summary>
        public void ActivateProfile(string profileId)
        {
            if (!_profileIndex.TryGetValue(profileId, out var profile))
            {
                Debug.LogWarning($"[PhysicsManager] No physics profile with id '{profileId}'.");
                return;
            }

            string previous = CurrentProfileId;
            CurrentProfileId = profileId;

            ApplyProfile(profile);
            ProfileActivatedOverride?.Invoke(profile, null);

            OnProfileChanged?.Invoke(previous, profileId);

            if (verboseLogging)
                Debug.Log($"[PhysicsManager] Activated profile '{profileId}'.");
        }

        /// <summary>Register or replace a physics profile at runtime.</summary>
        public void RegisterProfile(PhysicsProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.id)) return;
            _profileIndex[profile.id] = profile;
        }

        /// <summary>Return a profile by id, or null.</summary>
        public PhysicsProfile GetProfile(string id) =>
            _profileIndex.TryGetValue(id, out var p) ? p : null;

        /// <summary>All registered profile ids.</summary>
        public IEnumerable<string> GetAllProfileIds() => _profileIndex.Keys;

        // ─── Pause / Resume ───────────────────────────────────────────────────────

        /// <summary>Pause all physics simulation (sets <c>Physics.simulationMode</c> to Script and stops auto-update).</summary>
        public void PausePhysics()
        {
            if (IsPaused) return;
            IsPaused = true;
#if UNITY_2022_3_OR_NEWER
            Physics.simulationMode = SimulationMode.Script;
#else
            Physics.autoSimulation = false;
#endif
            OnPhysicsPaused?.Invoke();
            if (verboseLogging) Debug.Log("[PhysicsManager] Physics paused.");
        }

        /// <summary>Resume physics simulation, restoring the current profile's simulation mode.</summary>
        public void ResumePhysics()
        {
            if (!IsPaused) return;
            IsPaused = false;
            var profile = CurrentProfile;
            if (profile != null)
                ApplySimulationMode(profile.simulationMode);
            else
#if UNITY_2022_3_OR_NEWER
                Physics.simulationMode = SimulationMode.FixedUpdate;
#else
                Physics.autoSimulation = true;
#endif
            OnPhysicsResumed?.Invoke();
            if (verboseLogging) Debug.Log("[PhysicsManager] Physics resumed.");
        }

        // ─── Collision Layer Rules ────────────────────────────────────────────────

        /// <summary>Apply a list of collision layer rules to Unity's physics layer matrix.</summary>
        public void ApplyCollisionRules(IEnumerable<CollisionLayerRule> rules)
        {
            if (rules == null) return;
            foreach (var rule in rules)
            {
                if (string.IsNullOrEmpty(rule.layerA) || string.IsNullOrEmpty(rule.layerB)) continue;
                int a = LayerMask.NameToLayer(rule.layerA);
                int b = LayerMask.NameToLayer(rule.layerB);
                if (a < 0 || b < 0)
                {
                    Debug.LogWarning($"[PhysicsManager] Unknown layer in collision rule: '{rule.layerA}' / '{rule.layerB}'.");
                    continue;
                }
                Physics.IgnoreLayerCollision(a, b, !rule.enabled);
                if (verboseLogging)
                    Debug.Log($"[PhysicsManager] Collision {rule.layerA} ↔ {rule.layerB}: {(rule.enabled ? "enabled" : "disabled")}.");
            }
        }

        // ─── Impact reporting ─────────────────────────────────────────────────────

        /// <summary>
        /// Report an impact from an <see cref="ImpactReporter"/> component.
        /// Fires <see cref="OnImpact"/> if the impulse exceeds <see cref="impactThreshold"/>.
        /// </summary>
        public void ReportImpact(ImpactData data)
        {
            if (data == null || data.impulse < impactThreshold) return;
            OnImpact?.Invoke(data);
        }

        // ─── Time scale / slow-motion helpers ────────────────────────────────────

        /// <summary>
        /// Set Unity's <c>Time.timeScale</c> directly. Use for bullet-time or slow-motion effects.
        /// Note: fixed timestep is adjusted proportionally to maintain stable physics.
        /// </summary>
        public void SetTimeScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0f, 10f);
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * scale;
        }

        /// <summary>Restore time scale to 1.</summary>
        public void ResetTimeScale() => SetTimeScale(1f);

        // ─── Internal ────────────────────────────────────────────────────────────

        private void ApplyProfile(PhysicsProfile p)
        {
            Physics.gravity           = p.gravity;
            Time.fixedDeltaTime       = p.fixedTimestep > 0f ? p.fixedTimestep : 0.02f;
            Time.maximumDeltaTime     = p.maxAllowedTimestep > 0f ? p.maxAllowedTimestep : 0.3333f;
            Physics.sleepThreshold    = p.sleepThreshold;
            if (!IsPaused) ApplySimulationMode(p.simulationMode);
        }

        private static void ApplySimulationMode(PhysicsSimulationMode mode)
        {
#if UNITY_2022_3_OR_NEWER
            Physics.simulationMode = mode switch
            {
                PhysicsSimulationMode.Update  => SimulationMode.Update,
                PhysicsSimulationMode.Script  => SimulationMode.Script,
                _                             => SimulationMode.FixedUpdate
            };
#else
            Physics.autoSimulation = mode != PhysicsSimulationMode.Script;
#endif
        }

        private void BuildIndex()
        {
            _profileIndex.Clear();
            foreach (var p in profiles)
            {
                if (p == null || string.IsNullOrEmpty(p.id)) continue;
                _profileIndex[p.id] = p;
            }
        }

        private void LoadJsonDefinitions()
        {
            // ── Load physics profiles ──────────────────────────────────────────────
            string profileFullPath = Path.Combine(Application.streamingAssetsPath, jsonPath);
            if (Directory.Exists(profileFullPath))
            {
                foreach (var file in Directory.GetFiles(profileFullPath, "*.json", SearchOption.TopDirectoryOnly))
                    MergePhysicsProfilesFromFile(file);
            }
            else if (File.Exists(profileFullPath))
            {
                MergePhysicsProfilesFromFile(profileFullPath);
            }
            else
            {
                Debug.LogWarning($"[PhysicsManager] Profiles JSON not found: {profileFullPath}");
            }

            // ── Load collision rules ───────────────────────────────────────────────
            string collisionFullPath = Path.Combine(Application.streamingAssetsPath, collisionJsonPath);
            if (Directory.Exists(collisionFullPath))
            {
                foreach (var file in Directory.GetFiles(collisionFullPath, "*.json", SearchOption.TopDirectoryOnly))
                    MergeCollisionRulesFromFile(file);
            }
            else if (File.Exists(collisionFullPath))
            {
                MergeCollisionRulesFromFile(collisionFullPath);
            }
            else
            {
                Debug.LogWarning($"[PhysicsManager] Collision rules JSON not found: {collisionFullPath}");
            }
        }

        private void MergePhysicsProfilesFromFile(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                var root = JsonUtility.FromJson<PhysicsProfilesRoot>(json);
                if (root?.profiles == null) return;
                foreach (var p in root.profiles)
                {
                    if (p == null || string.IsNullOrEmpty(p.id)) continue;
                    p.rawJson = json;
                    _profileIndex[p.id] = p;
                    if (verboseLogging) Debug.Log($"[PhysicsManager] JSON profile override '{p.id}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysicsManager] Failed to load profiles from '{path}': {e.Message}");
            }
        }

        private void MergeCollisionRulesFromFile(string path)
        {
            try
            {
                var root = JsonUtility.FromJson<CollisionRulesRoot>(File.ReadAllText(path));
                if (root?.collisionRules != null)
                    ApplyCollisionRules(root.collisionRules);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysicsManager] Failed to load collision rules from '{path}': {e.Message}");
            }
        }

        // ─── JSON wrappers ────────────────────────────────────────────────────────

        [Serializable]
        private class PhysicsProfilesRoot
        {
            public List<PhysicsProfile> profiles;
        }

        [Serializable]
        private class CollisionRulesRoot
        {
            public List<CollisionLayerRule> collisionRules;
        }
    }
}
