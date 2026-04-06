#if PHYSICSMANAGER_MLF
using MapLoaderFramework.Runtime;
using UnityEngine;

namespace PhysicsManager.Runtime
{
    /// <summary>
    /// <b>MapLoaderBridge</b> connects PhysicsManager to MapLoaderFramework.
    /// <para>
    /// When <c>PHYSICSMANAGER_MLF</c> is defined:
    /// <list type="bullet">
    ///   <item>Subscribes to <c>MapLoaderFramework.OnMapLoaded</c> and activates the physics
    ///   profile specified in the map's <c>physicsProfileId</c> field (if registered).</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("PhysicsManager/MapLoader Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MapLoaderBridge : UnityEngine.MonoBehaviour
    {
        private PhysicsManager    _physicsManager;
        private MapLoaderFramework _mapLoader;

        private void Awake()
        {
            _physicsManager = GetComponent<PhysicsManager>()    ?? FindFirstObjectByType<PhysicsManager>();
            _mapLoader      = GetComponent<MapLoaderFramework>() ?? FindFirstObjectByType<MapLoaderFramework>();

            if (_physicsManager == null) Debug.LogWarning("[PhysicsManager/MapLoaderBridge] PhysicsManager not found.");
            if (_mapLoader      == null) Debug.LogWarning("[PhysicsManager/MapLoaderBridge] MapLoaderFramework not found.");
        }

        private void OnEnable()
        {
            if (_mapLoader != null) _mapLoader.OnMapLoaded += HandleMapLoaded;
        }

        private void OnDisable()
        {
            if (_mapLoader != null) _mapLoader.OnMapLoaded -= HandleMapLoaded;
        }

        private void HandleMapLoaded(MapData mapData)
        {
            if (_physicsManager == null || mapData == null) return;
            if (!string.IsNullOrEmpty(mapData.physicsProfileId))
                _physicsManager.ActivateProfile(mapData.physicsProfileId);
        }
    }
}
#endif
