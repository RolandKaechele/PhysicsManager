#if PHYSICSMANAGER_STM
using StateManager.Runtime;
using UnityEngine;

namespace PhysicsManager.Runtime
{
    /// <summary>
    /// <b>StateManagerBridge</b> connects PhysicsManager to StateManager.
    /// <para>
    /// When <c>PHYSICSMANAGER_STM</c> is defined:
    /// <list type="bullet">
    ///   <item>Pauses physics simulation when a Cutscene or Loading state becomes active.</item>
    ///   <item>Resumes physics when those states pop off the stack.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("PhysicsManager/StateManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class StateManagerBridge : UnityEngine.MonoBehaviour
    {
        [UnityEngine.SerializeField]
        [UnityEngine.Tooltip("State ids that trigger physics pause (default: Cutscene, Loading).")]
        private string[] pauseStateIds = { "Cutscene", "Loading" };

        private PhysicsManager _physicsManager;
        private StateManager _stateManager;

        private void Awake()
        {
            _physicsManager = GetComponent<PhysicsManager>() ?? FindFirstObjectByType<PhysicsManager>();
            _stateManager   = GetComponent<StateManager>()   ?? FindFirstObjectByType<StateManager>();

            if (_physicsManager == null) Debug.LogWarning("[PhysicsManager/StateManagerBridge] PhysicsManager not found.");
            if (_stateManager   == null) Debug.LogWarning("[PhysicsManager/StateManagerBridge] StateManager not found.");
        }

        private void OnEnable()
        {
            if (_stateManager == null) return;
            _stateManager.OnStatePushed += HandleStatePushed;
            _stateManager.OnStatePopped += HandleStatePopped;
        }

        private void OnDisable()
        {
            if (_stateManager == null) return;
            _stateManager.OnStatePushed -= HandleStatePushed;
            _stateManager.OnStatePopped -= HandleStatePopped;
        }

        private bool IsPauseState(string stateId)
        {
            foreach (var id in pauseStateIds)
                if (string.Equals(id, stateId, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private void HandleStatePushed(string stateId)
        {
            if (IsPauseState(stateId)) _physicsManager?.PausePhysics();
        }

        private void HandleStatePopped(string stateId)
        {
            if (IsPauseState(stateId)) _physicsManager?.ResumePhysics();
        }
    }
}
#endif
