using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhysicsManager.Runtime
{
    // -------------------------------------------------------------------------
    // PhysicsSimulationMode
    // -------------------------------------------------------------------------

    /// <summary>The physics simulation update mode.</summary>
    public enum PhysicsSimulationMode
    {
        /// <summary>Physics updated in FixedUpdate (default Unity behaviour).</summary>
        FixedUpdate,
        /// <summary>Physics updated in Update.</summary>
        Update,
        /// <summary>Physics simulation is driven manually via script.</summary>
        Script
    }

    // -------------------------------------------------------------------------
    // PhysicsProfile
    // -------------------------------------------------------------------------

    /// <summary>
    /// A named snapshot of physics settings that can be activated at runtime.
    /// Designed for use in different gameplay contexts (e.g. Normal, Underwater, Space, Boss).
    /// Authored in JSON or via Inspector.
    /// </summary>
    [Serializable]
    public class PhysicsProfile
    {
        /// <summary>Unique identifier (e.g. "default", "underwater", "lowgravity").</summary>
        public string id;

        /// <summary>Human-readable label.</summary>
        public string label;

        /// <summary>Gravity vector applied when this profile is active.</summary>
        public Vector3 gravity = new Vector3(0f, -9.81f, 0f);

        /// <summary>Fixed timestep in seconds. 0 means use default (0.02).</summary>
        public float fixedTimestep = 0.02f;

        /// <summary>Maximum allowed timestep. 0 means use default (0.3333).</summary>
        public float maxAllowedTimestep = 0.3333f;

        /// <summary>Global default physics material bounce combine.</summary>
        public PhysicMaterialCombine bounceCombine = PhysicMaterialCombine.Average;

        /// <summary>Global default physics material friction combine.</summary>
        public PhysicMaterialCombine frictionCombine = PhysicMaterialCombine.Average;

        /// <summary>Whether to automatically simulate physics (Unity's Physics.autoSimulation).</summary>
        public bool autoSimulation = true;

        /// <summary>Simulation mode for this profile.</summary>
        public PhysicsSimulationMode simulationMode = PhysicsSimulationMode.FixedUpdate;

        /// <summary>Global linear velocity threshold for sleeping rigidbodies.</summary>
        public float sleepThreshold = 0.005f;

        /// <summary>Raw JSON stored during deserialisation (non-serialised).</summary>
        [NonSerialized] public string rawJson;
    }

    // -------------------------------------------------------------------------
    // CollisionLayerRule
    // -------------------------------------------------------------------------

    /// <summary>
    /// Defines a collision enable/disable rule between two named physics layers.
    /// </summary>
    [Serializable]
    public class CollisionLayerRule
    {
        /// <summary>Name of the first layer (as in Project Settings › Physics › Layer Collision Matrix).</summary>
        public string layerA;

        /// <summary>Name of the second layer.</summary>
        public string layerB;

        /// <summary>Whether collisions between these two layers are enabled.</summary>
        public bool enabled = true;
    }

    // -------------------------------------------------------------------------
    // ImpactData
    // -------------------------------------------------------------------------

    /// <summary>
    /// Data passed to <see cref="PhysicsManager.OnImpact"/> when a tagged impact occurs.
    /// </summary>
    public class ImpactData
    {
        /// <summary>Collision magnitude.</summary>
        public float impulse;

        /// <summary>World position of the impact.</summary>
        public Vector3 point;

        /// <summary>The first involved GameObject.</summary>
        public GameObject objectA;

        /// <summary>The second involved GameObject.</summary>
        public GameObject objectB;
    }
}
