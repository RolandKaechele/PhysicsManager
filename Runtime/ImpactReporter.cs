using UnityEngine;

namespace PhysicsManager.Runtime
{
    /// <summary>
    /// Attach to any GameObject with a <see cref="Collider"/> to report collisions
    /// above the configured threshold to <see cref="PhysicsManager"/>.
    /// Works on any Rigidbody — no manual wiring required beyond placing the component.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("PhysicsManager/Impact Reporter")]
    public class ImpactReporter : MonoBehaviour
    {
        [Tooltip("Override the global threshold for this object. 0 = use global PhysicsManager value.")]
        [SerializeField] private float localThresholdOverride = 0f;

        private PhysicsManager _physicsManager;

        private void Awake()
        {
            _physicsManager = FindFirstObjectByType<PhysicsManager>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_physicsManager == null) return;

            float impulse = collision.impulse.magnitude;
            if (localThresholdOverride > 0f && impulse < localThresholdOverride) return;

            var data = new ImpactData
            {
                impulse = impulse,
                point   = collision.GetContact(0).point,
                objectA = gameObject,
                objectB = collision.gameObject
            };
            _physicsManager.ReportImpact(data);
        }
    }
}
