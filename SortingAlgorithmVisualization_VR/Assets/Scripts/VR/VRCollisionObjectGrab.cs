using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using CollisionLab;

namespace CollisionLab.VR
{
    /// <summary>
    /// Specialized XR grab interactable for the primary physics demonstration objects (Sphere and Cube).
    /// Enforces:
    /// 1. Grabbing allowed in Ready and Collided states, blocked during InMotion simulation.
    /// 2. Clean release before Launch or Reset without overriding physics velocities.
    /// 3. Suppression of collision telemetry reports during active hand manipulation.
    /// 4. Re-enabling normal Rigidbody physics on drop without simulated-controller throw velocity.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CollisionObject))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class VRCollisionObjectGrab : XRGrabInteractable
    {
        private static readonly List<VRCollisionObjectGrab> s_ActiveInstances = new List<VRCollisionObjectGrab>();

        private CollisionObject _collisionObject;
        private Rigidbody _rb;

        /// <summary>
        /// Releases all currently held demonstration objects cleanly.
        /// </summary>
        public static void ReleaseAllHeldObjects()
        {
            for (int i = s_ActiveInstances.Count - 1; i >= 0; i--)
            {
                if (s_ActiveInstances[i] != null && s_ActiveInstances[i].isSelected)
                {
                    s_ActiveInstances[i].ForceRelease();
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();

            _collisionObject = GetComponent<CollisionObject>();
            _rb = GetComponent<Rigidbody>();

            // Requirement 8: Disable throw velocity on detach so releasing drops cleanly under gravity
            throwOnDetach = false;

            // Requirement 4: Match interaction layers with default controller ray/direct interactors
            interactionLayers = InteractionLayerMask.GetMask("Default");

            // Use VelocityTracking for smooth hand tracking while respecting physics boundaries
            movementType = MovementType.VelocityTracking;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!s_ActiveInstances.Contains(this))
            {
                s_ActiveInstances.Add(this);
            }

            CollisionLabManager.ReleaseHeldObjectsHandler += ForceRelease;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            s_ActiveInstances.Remove(this);

            CollisionLabManager.ReleaseHeldObjectsHandler -= ForceRelease;
        }

        /// <summary>
        /// Requirement 5: Permit grabbing in Ready and Collided states,
        /// but prevent new grabs while CollisionLabManager.CurrentState == SimulationState.InMotion.
        /// </summary>
        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            if (CollisionLabManager.Instance != null && CollisionLabManager.Instance.CurrentState == SimulationState.InMotion)
            {
                return false;
            }

            return base.IsSelectableBy(interactor);
        }

        /// <summary>
        /// Requirement 7: Suppress collision telemetry reports while an object is actively held.
        /// </summary>
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);

            if (_collisionObject != null)
            {
                _collisionObject.SuppressCollisionReports = true;
            }
        }

        /// <summary>
        /// Requirement 7 & 8: Restore collision reports and zero linear/angular velocities
        /// so the object drops naturally under normal gravity without erratic controller fling.
        /// </summary>
        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            if (_collisionObject != null)
            {
                _collisionObject.SuppressCollisionReports = false;
            }

            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Requirement 6 & 9: Cleanly forces selection exit when Launch or Reset is triggered.
        /// </summary>
        public void ForceRelease()
        {
            if (isSelected && interactionManager != null)
            {
                for (int i = interactorsSelecting.Count - 1; i >= 0; i--)
                {
                    interactionManager.SelectExit(interactorsSelecting[i], this);
                }
            }

            if (_collisionObject != null)
            {
                _collisionObject.SuppressCollisionReports = false;
            }

            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
