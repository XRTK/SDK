﻿// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.InputSystem;
using XRTK.Definitions.SpatialAwarenessSystem;
using XRTK.EventDatum.Input;
using XRTK.Extensions;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Services;

namespace XRTK.SDK.Input.Handlers
{
    /// <summary>
    /// This input handler is designed to help facilitate the physical manipulation of <see cref="GameObject"/>s across all platforms.
    /// Users will be able to use the select action to activate the manipulation phase, then various gestures and supplemental actions to
    /// nudge, rotate, and scale the object.
    /// </summary>
    public class ManipulationHandler : BaseInputHandler,
        IMixedRealityPointerHandler,
        IMixedRealityInputHandler,
        IMixedRealityInputHandler<float>,
        IMixedRealityInputHandler<Vector2>
    {
        #region Input Actions

        [Header("Input Actions")]

        [SerializeField]
        [Tooltip("The action to use to select the GameObject and begin/end the manipulation phase")]
        private MixedRealityInputAction selectAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to select the GameObject and begin/end the manipulation phase
        /// </summary>
        public MixedRealityInputAction SelectAction
        {
            get => selectAction;
            set => selectAction = value;
        }

        [SerializeField]
        [Tooltip("The action to use to enable pressed actions")]
        private MixedRealityInputAction touchpadPressAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to enable pressed actions
        /// </summary>
        public MixedRealityInputAction TouchpadPressAction
        {
            get => touchpadPressAction;
            set => touchpadPressAction = value;
        }

        [Range(0f, 1f)]
        [SerializeField]
        private float pressThreshold = 0.25f;

        public float PressThreshold
        {
            get => pressThreshold;
            set => pressThreshold = value;
        }

        [SerializeField]
        [Tooltip("The action to use to rotate the GameObject")]
        private MixedRealityInputAction rotateAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to rotate the GameObject
        /// </summary>
        public MixedRealityInputAction RotateAction
        {
            get => rotateAction;
            set => rotateAction = value;
        }

        [SerializeField]
        [Tooltip("The action to use to scale the GameObject")]
        private MixedRealityInputAction scaleAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to scale the GameObject
        /// </summary>
        public MixedRealityInputAction ScaleAction
        {
            get => scaleAction;
            set => scaleAction = value;
        }

        [SerializeField]
        [Tooltip("The action to use to nudge the pointer extent closer or further away from the pointer source")]
        private MixedRealityInputAction nudgeAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to nudge the <see cref="GameObject"/> closer or further away from the pointer source.
        /// </summary>
        public MixedRealityInputAction NudgeAction
        {
            get => nudgeAction;
            set => nudgeAction = value;
        }

        [SerializeField]
        [Tooltip("The action to use to immediately cancel any current manipulation.")]
        private MixedRealityInputAction cancelAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to immediately cancel any current manipulation.
        /// </summary>
        public MixedRealityInputAction CancelAction
        {
            get => cancelAction;
            set => cancelAction = value;
        }

        #endregion Input Actions

        #region Manipulation Options

        [Header("Options")]

        [SerializeField]
        [Tooltip("The object to manipulate using this handler. Automatically uses this transform if none is set.")]
        private Transform manipulationTarget;

        [SerializeField]
        [Tooltip("Should the user press and hold the select action or press to hold and press again to release?")]
        private bool useHold = true;

        /// <summary>
        /// Should the user press and hold the select action or press to hold and press again to release?
        /// </summary>
        public bool UseHold
        {
            get => useHold;
            set => useHold = value;
        }

        #region Scale Options

        [SerializeField]
        [Tooltip("The min/max values to activate the scale action.\nNote: this is transformed into and used as absolute values.")]
        private Vector2 scaleZone = new Vector2(0.25f, 1f);

        /// <summary>
        /// The dual axis zone to process and activate the scale action.
        /// </summary>
        /// <remarks>This is transformed into and used as absolute values.</remarks>
        public Vector2 ScaleZone
        {
            get => scaleZone;
            set => scaleZone = value;
        }

        [SerializeField]
        [Tooltip("The amount to scale the GameObject")]
        private float scaleAmount = 0.5f;

        /// <summary>
        /// The amount to scale the <see cref="GameObject"/>
        /// </summary>
        public float ScaleAmount
        {
            get => scaleAmount;
            set => scaleAmount = value;
        }

        [SerializeField]
        [Tooltip("The min and max size this object can be scaled to.")]
        private Vector2 scaleConstraints = new Vector2(0.01f, 1f);

        /// <summary>
        /// The min and max size this object can be scaled to.
        /// </summary>
        public Vector2 ScaleConstraints
        {
            get => scaleConstraints;
            set => scaleConstraints = value;
        }

        #endregion Scale Options

        #region Rotation Options

        [SerializeField]
        [Tooltip("The min/max values to activate the rotate action.\nNote: this is transformed into and used as absolute values.")]
        private Vector2 rotationZone = new Vector2(0.25f, 1f);

        /// <summary>
        /// The dual axis zone to process and activate the scale action.
        /// </summary>
        /// <remarks>This is transformed into and used as absolute values.</remarks>
        public Vector2 RotationZone
        {
            get => rotationZone;
            set => rotationZone = value;
        }

        [SerializeField]
        [Range(2.8125f, 45f)]
        [Tooltip("The amount a user has to scroll in a circular motion to activate the rotation action.")]
        private float rotationAngleActivation = 11.25f;

        /// <summary>
        /// The amount a user has to scroll in a circular motion to activate the rotation action.
        /// </summary>
        public float RotationAngleActivation
        {
            get => rotationAngleActivation;
            set => rotationAngleActivation = value;
        }

        #endregion Rotation Options

        #region Nudge Options

        [SerializeField]
        [Tooltip("The min/max values to activate the nudge action.\nNote: this is transformed into and used as absolute values.")]
        private Vector2 nudgeZone = new Vector2(0.25f, 1f);

        /// <summary>
        /// The dual axis zone to process and activate the nudge action.
        /// </summary>
        /// <remarks>This is transformed into and used as absolute values.</remarks>
        public Vector2 NudgeZone
        {
            get => nudgeZone;
            set => nudgeZone = value;
        }

        [SerializeField]
        [Range(0.1f, 0.001f)]
        [Tooltip("The amount to nudge the position of the GameObject")]
        private float nudgeAmount = 0.01f;

        /// <summary>
        /// The amount to nudge the position of the <see cref="GameObject"/>
        /// </summary>
        public float NudgeAmount
        {
            get => nudgeAmount;
            set => nudgeAmount = value;
        }

        [SerializeField]
        [Tooltip("The min and max values for the nudge amount.")]
        private Vector2 nudgeConstraints = new Vector2(0.25f, 10f);

        /// <summary>
        /// The min and max values for the nudge amount.
        /// </summary>
        public Vector2 NudgeConstraints
        {
            get => nudgeConstraints;
            set => nudgeConstraints = value;
        }

        #endregion Nudge Options

        #endregion Manipulation Options

        /// <summary>
        /// The current status of the hold.
        /// </summary>
        /// <remarks>
        /// Used to determine if the <see cref="GameObject"/> is currently being manipulated by the user.
        /// </remarks>
        private bool isBeingHeld = false;

        /// <summary>
        /// The first input source to start the manipulation phase of this object.
        /// </summary>
        private IMixedRealityInputSource primaryInputSource = null;

        /// <summary>
        /// The first pointer to start the manipulation phase of this object.
        /// </summary>
        private IMixedRealityPointer primaryPointer = null;

        /// <summary>
        /// The last rotation reading used to calculate if the rotation action is active.
        /// </summary>
        private Vector2 lastPositionReading = Vector2.zero;

        private bool isPressed = false;

        private bool isRotating = false;

        #region Monobehaviour Implementation

        private void Awake()
        {
            if (manipulationTarget == null)
            {
                manipulationTarget = transform;
            }
        }

        private void Update()
        {
            if (isBeingHeld)
            {
                transform.position = primaryPointer.Result.Details.Point;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EndHold();
        }

        #endregion Monobehaviour Implementation

        #region IMixedRealityInputHandler Implementation

        /// <inheritdoc />
        public virtual void OnInputDown(InputEventData eventData)
        {
            if (eventData.MixedRealityInputAction == cancelAction)
            {
                EndHold();
            }
        }

        /// <inheritdoc />
        public virtual void OnInputUp(InputEventData eventData)
        {

            if (eventData.MixedRealityInputAction == cancelAction &&
                eventData.InputSource.SourceId == primaryInputSource.SourceId)
            {
                EndHold();
            }
        }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<float> eventData)
        {
            if (!isBeingHeld ||
                primaryInputSource == null ||
                eventData.InputSource.SourceId != primaryInputSource.SourceId)
            {
                return;
            }

            if (eventData.MixedRealityInputAction == touchpadPressAction &&
                eventData.InputData <= 0.00001f)
            {
                isRotating = false;
                lastPositionReading.x = 0f;
                lastPositionReading.y = 0f;
                eventData.Use();
            }

            if (isRotating) { return; }

            if (!isPressed &&
                eventData.MixedRealityInputAction == touchpadPressAction &&
                eventData.InputData >= pressThreshold)
            {
                isPressed = true;
                eventData.Use();
            }

            if (isPressed &&
                eventData.MixedRealityInputAction == touchpadPressAction &&
                eventData.InputData <= pressThreshold)
            {
                isPressed = false;
                eventData.Use();
            }
        }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<Vector2> eventData)
        {
            if (!isBeingHeld ||
                primaryInputSource == null ||
                eventData.InputSource.SourceId != primaryInputSource.SourceId)
            {
                return;
            }

            // Filter our actions
            if (eventData.MixedRealityInputAction != nudgeAction ||
                eventData.MixedRealityInputAction != scaleAction ||
                eventData.MixedRealityInputAction != rotateAction)
            {
                return;
            }

            var absoluteInputData = eventData.InputData;
            absoluteInputData.x = Mathf.Abs(absoluteInputData.x);
            absoluteInputData.y = Mathf.Abs(absoluteInputData.y);

            var isRotationPossible = eventData.MixedRealityInputAction == rotateAction &&
                                     (absoluteInputData.x >= rotationZone.x ||
                                      absoluteInputData.y >= rotationZone.x);

            if (!isPressed &&
                isRotationPossible &&
                !lastPositionReading.x.Equals(0f) && !lastPositionReading.y.Equals(0f))
            {
                var rotationAngle = Vector2.SignedAngle(lastPositionReading, eventData.InputData);

                if (Mathf.Abs(rotationAngle) > rotationAngleActivation)
                {
                    isRotating = true;
                }

                if (isRotating)
                {
                    transform.Rotate(0f, -rotationAngle, 0f, Space.Self);
                    eventData.Use();
                }
            }

            lastPositionReading = eventData.InputData;

            if (!isPressed || isRotating) { return; }

            bool isScalePossible = eventData.MixedRealityInputAction == scaleAction && absoluteInputData.x > 0f;
            bool isNudgePossible = eventData.MixedRealityInputAction == nudgeAction && absoluteInputData.y > 0f;

            // Check to make sure that input values fall between min/max zone values
            if (isScalePossible &&
                (absoluteInputData.x <= scaleZone.x ||
                 absoluteInputData.x >= scaleZone.y))
            {
                isScalePossible = false;
            }

            // Check to make sure that input values fall between min/max zone values
            if (isNudgePossible &&
                (absoluteInputData.y <= nudgeZone.x ||
                 absoluteInputData.y >= nudgeZone.y))
            {
                isNudgePossible = false;
            }

            // Disable any actions if min zone values overlap.
            if (absoluteInputData.x <= scaleZone.x &&
                absoluteInputData.y <= nudgeZone.x)
            {
                isNudgePossible = false;
                isScalePossible = false;
            }

            if (isScalePossible && isNudgePossible)
            {
                isNudgePossible = false;
                isScalePossible = false;
            }

            if (isNudgePossible)
            {
                var pointers = eventData.InputSource.Pointers;

                for (int i = 0; i < pointers.Length; i++)
                {
                    var newExtent = pointers[i].PointerExtent;
                    var currentRaycastDistance = pointers[i].Result.Details.RayDistance;

                    // Reset the cursor extent to the nearest value in case we're hitting something close
                    // and the user wants to adjust. That way it doesn't take forever to see the change.
                    if (currentRaycastDistance < newExtent)
                    {
                        newExtent = currentRaycastDistance;
                    }

                    var prevExtent = newExtent;

                    if (eventData.InputData.y < 0f)
                    {
                        newExtent = prevExtent - nudgeAmount;

                        if (newExtent <= nudgeConstraints.x)
                        {
                            newExtent = prevExtent;
                        }
                    }
                    else
                    {
                        newExtent = prevExtent + nudgeAmount;

                        if (newExtent >= nudgeConstraints.y)
                        {
                            newExtent = prevExtent;
                        }
                    }

                    pointers[i].PointerExtent = newExtent;
                }

                eventData.Use();
            }

            if (isScalePossible)
            {
                var newScale = transform.localScale;
                var prevScale = newScale;

                if (eventData.InputData.x < 0f)
                {
                    newScale *= scaleAmount;

                    // We can check any axis, they should all be the same as we do uniform scales.
                    if (newScale.x <= scaleConstraints.x)
                    {
                        newScale = prevScale;
                    }
                }
                else
                {
                    newScale /= scaleAmount;

                    // We can check any axis, they should all be the same as we do uniform scales.
                    if (newScale.y >= scaleConstraints.y)
                    {
                        newScale = prevScale;
                    }
                }

                transform.localScale = newScale;
                eventData.Use();
            }
        }

        #endregion IMixedRealityInputHandler Implementation

        #region IMixedRealityPointerHandler Implementation

        /// <inheritdoc />
        public virtual void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (eventData.MixedRealityInputAction == selectAction)
            {
                if (!useHold)
                {
                    BeginHold(eventData);
                }

                eventData.Use();
            }
        }

        /// <inheritdoc />
        public virtual void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (eventData.used ||
                eventData.MixedRealityInputAction != selectAction ||
                primaryInputSource != null && eventData.InputSource.SourceId != primaryInputSource.SourceId) { return; }

            if (useHold)
            {
                if (isBeingHeld)
                {
                    EndHold();
                }
                else
                {
                    BeginHold(eventData);
                }
            }

            if (!useHold && isBeingHeld)
            {
                EndHold();
            }

            eventData.Use();
        }

        /// <inheritdoc />
        public virtual void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

        #endregion IMixedRealityPointerHandler Implementation

        public virtual void BeginHold(MixedRealityPointerEventData eventData)
        {
            if (isBeingHeld) { return; }

            isBeingHeld = true;

            MixedRealityToolkit.InputSystem.PushModalInputHandler(gameObject);
            MixedRealityToolkit.SpatialAwarenessSystem.SetMeshVisibility(SpatialMeshDisplayOptions.Collision);

            if (primaryInputSource == null)
            {
                primaryInputSource = eventData.InputSource;
            }

            if (primaryPointer == null)
            {
                primaryPointer = eventData.Pointer;
            }

            transform.SetCollidersActive(false);

            eventData.Use();
        }

        public virtual void EndHold()
        {
            if (!isBeingHeld) { return; }

            MixedRealityToolkit.SpatialAwarenessSystem.SetMeshVisibility(SpatialMeshDisplayOptions.None);

            primaryPointer = null;
            primaryInputSource = null;

            isBeingHeld = false;
            MixedRealityToolkit.InputSystem.PopModalInputHandler();
            transform.SetCollidersActive(true);
        }
    }
}
