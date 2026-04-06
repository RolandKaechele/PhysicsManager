#if PHYSICSMANAGER_CAM
using CameraManager.Runtime;
using UnityEngine;

namespace PhysicsManager.Runtime
{
    /// <summary>
    /// <b>CameraManagerBridge</b> connects PhysicsManager to CameraManager.
    /// <para>
    /// When <c>PHYSICSMANAGER_CAM</c> is defined:
    /// <list type="bullet">
    ///   <item>Triggers a camera shake via <c>CameraManager.Shake()</c> whenever
    ///   <see cref="PhysicsManager.OnImpact"/> fires with an impulse above the configured threshold.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("PhysicsManager/CameraManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class CameraManagerBridge : UnityEngine.MonoBehaviour
    {
        [UnityEngine.Tooltip("Minimum impulse to trigger a camera shake (in addition to PhysicsManager.impactThreshold).")]
        [UnityEngine.SerializeField] private float shakeImpulseThreshold = 10f;

        [UnityEngine.Tooltip("Shake intensity multiplier. Final strength = impulse / shakeImpulseThreshold * shakeStrength.")]
        [UnityEngine.SerializeField] private float shakeStrength = 0.3f;

        [UnityEngine.Tooltip("Camera shake duration in seconds.")]
        [UnityEngine.SerializeField] private float shakeDuration = 0.25f;

        private PhysicsManager _physicsManager;
        private CameraManager  _cameraManager;

        private void Awake()
        {
            _physicsManager = GetComponent<PhysicsManager>() ?? FindFirstObjectByType<PhysicsManager>();
            _cameraManager  = GetComponent<CameraManager>()  ?? FindFirstObjectByType<CameraManager>();

            if (_physicsManager == null) Debug.LogWarning("[PhysicsManager/CameraManagerBridge] PhysicsManager not found.");
            if (_cameraManager  == null) Debug.LogWarning("[PhysicsManager/CameraManagerBridge] CameraManager not found.");
        }

        private void OnEnable()
        {
            if (_physicsManager != null) _physicsManager.OnImpact += HandleImpact;
        }

        private void OnDisable()
        {
            if (_physicsManager != null) _physicsManager.OnImpact -= HandleImpact;
        }

        private void HandleImpact(ImpactData data)
        {
            if (_cameraManager == null || data.impulse < shakeImpulseThreshold) return;
            float strength = Mathf.Clamp01(data.impulse / shakeImpulseThreshold) * shakeStrength;
            _cameraManager.Shake(strength, shakeDuration);
        }
    }
}
#endif
