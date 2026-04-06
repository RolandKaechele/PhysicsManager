#if PHYSICSMANAGER_EM
using EventManager.Runtime;
using UnityEngine;

namespace PhysicsManager.Runtime
{
    /// <summary>
    /// <b>EventManagerBridge</b> connects PhysicsManager to EventManager.
    /// <para>
    /// When <c>PHYSICSMANAGER_EM</c> is defined, fires the following named events:
    /// <list type="bullet">
    ///   <item><c>physics.impact</c> — payload: impulse magnitude (float as string)</item>
    ///   <item><c>physics.profile.changed</c> — payload: new profile id</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("PhysicsManager/EventManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class EventManagerBridge : UnityEngine.MonoBehaviour
    {
        private PhysicsManager _physicsManager;
        private EventManager   _eventManager;

        private void Awake()
        {
            _physicsManager = GetComponent<PhysicsManager>() ?? FindFirstObjectByType<PhysicsManager>();
            _eventManager   = GetComponent<EventManager>()   ?? FindFirstObjectByType<EventManager>();

            if (_physicsManager == null) Debug.LogWarning("[PhysicsManager/EventManagerBridge] PhysicsManager not found.");
            if (_eventManager   == null) Debug.LogWarning("[PhysicsManager/EventManagerBridge] EventManager not found.");
        }

        private void OnEnable()
        {
            if (_physicsManager == null) return;
            _physicsManager.OnImpact         += HandleImpact;
            _physicsManager.OnProfileChanged += HandleProfileChanged;
        }

        private void OnDisable()
        {
            if (_physicsManager == null) return;
            _physicsManager.OnImpact         -= HandleImpact;
            _physicsManager.OnProfileChanged -= HandleProfileChanged;
        }

        private void HandleImpact(ImpactData data)
            => _eventManager?.FireEvent("physics.impact", data.impulse.ToString("F2"));

        private void HandleProfileChanged(string prev, string next)
            => _eventManager?.FireEvent("physics.profile.changed", next);
    }
}
#endif
