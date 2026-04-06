#if PHYSICSMANAGER_DOTWEEN
using System;
using UnityEngine;
using DG.Tweening;

namespace PhysicsManager.Runtime
{
    /// <summary>
    /// Optional bridge that adds DOTween-driven gravity ramp and time-scale slow-motion
    /// effects to <see cref="PhysicsManager"/>.
    /// Enable define <c>PHYSICSMANAGER_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// Hooks <see cref="PhysicsManager.ProfileActivatedOverride"/> to smoothly tween
    /// <c>Physics.gravity</c> when switching profiles instead of applying it instantly.
    /// Also exposes <see cref="SlowMotion"/> and <see cref="ResetTimeScale"/> methods with
    /// DOTween easing for bullet-time effects.
    /// </para>
    /// </summary>
    [AddComponentMenu("PhysicsManager/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenPhysicsBridge : MonoBehaviour
    {
        [Header("Gravity Transition")]
        [Tooltip("Duration of the gravity tween when switching profiles.")]
        [SerializeField] private float gravityTweenDuration = 0.5f;

        [Tooltip("Ease applied to gravity transitions between profiles.")]
        [SerializeField] private Ease gravityEase = Ease.OutSine;

        [Header("Slow Motion")]
        [Tooltip("Default slow-motion time scale.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float slowMotionScale = 0.25f;

        [Tooltip("Duration to ramp into slow motion.")]
        [SerializeField] private float slowMotionInDuration = 0.3f;

        [Tooltip("Duration to ramp out of slow motion back to normal.")]
        [SerializeField] private float slowMotionOutDuration = 0.5f;

        [Tooltip("Ease applied to time-scale tweens.")]
        [SerializeField] private Ease timeScaleEase = Ease.InOutSine;

        private PhysicsManager _physicsManager;
        private Tweener _gravityTween;
        private Tweener _timeScaleTween;

        private void Awake()
        {
            _physicsManager = GetComponent<PhysicsManager>() ?? FindFirstObjectByType<PhysicsManager>();
            if (_physicsManager == null)
                Debug.LogWarning("[PhysicsManager/DotweenPhysicsBridge] PhysicsManager not found.");
        }

        private void OnEnable()
        {
            if (_physicsManager != null)
                _physicsManager.ProfileActivatedOverride = HandleProfileActivated;
        }

        private void OnDisable()
        {
            if (_physicsManager == null) return;
            if (_physicsManager.ProfileActivatedOverride == (Action<PhysicsProfile, Action>)HandleProfileActivated)
                _physicsManager.ProfileActivatedOverride = null;
        }

        private void OnDestroy()
        {
            _gravityTween?.Kill();
            _timeScaleTween?.Kill();
        }

        // ─── Slow motion API ──────────────────────────────────────────────────────

        /// <summary>Ramp time scale down to <see cref="slowMotionScale"/> with DOTween easing.</summary>
        public void SlowMotion()
        {
            _timeScaleTween?.Kill();
            _timeScaleTween = DOTween.To(
                () => Time.timeScale,
                v  => { Time.timeScale = v; Time.fixedDeltaTime = 0.02f * v; },
                slowMotionScale,
                slowMotionInDuration
            ).SetEase(timeScaleEase).SetUpdate(true);
        }

        /// <summary>Ramp time scale back to 1 with DOTween easing.</summary>
        public void ResetTimeScale()
        {
            _timeScaleTween?.Kill();
            _timeScaleTween = DOTween.To(
                () => Time.timeScale,
                v  => { Time.timeScale = v; Time.fixedDeltaTime = 0.02f * v; },
                1f,
                slowMotionOutDuration
            ).SetEase(timeScaleEase).SetUpdate(true);
        }

        // ─── Profile gravity tween ────────────────────────────────────────────────

        private void HandleProfileActivated(PhysicsProfile profile, Action onComplete)
        {
            _gravityTween?.Kill();
            Vector3 target = profile.gravity;
            _gravityTween = DOTween.To(
                () => Physics.gravity,
                v  => Physics.gravity = v,
                target,
                gravityTweenDuration
            ).SetEase(gravityEase).OnComplete(() => onComplete?.Invoke());
        }
    }
}
#else
namespace PhysicsManager.Runtime
{
    /// <summary>No-op stub — enable define <c>PHYSICSMANAGER_DOTWEEN</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("PhysicsManager/DOTween Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DotweenPhysicsBridge : UnityEngine.MonoBehaviour { }
}
#endif
