﻿// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.Serialization;
using XRTK.Definitions.Physics;
using XRTK.EventDatum.Input;
using XRTK.EventDatum.Teleport;
using XRTK.Interfaces.CameraSystem;
using XRTK.Interfaces.TeleportSystem;
using XRTK.Services;
using XRTK.Services.Teleportation;
using XRTK.Utilities.Physics;

namespace XRTK.SDK.UX.Pointers
{
    public class TeleportPointer : LinePointer
    {
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("The threshold amount for joystick input (Dead Zone)")]
        private float inputThreshold = 0.5f;

        [SerializeField]
        [Range(0f, 360f)]
        [Tooltip("If Pressing 'forward' on the thumbstick gives us an angle that doesn't quite feel like the forward direction, we apply this offset to make navigation feel more natural")]
        private float angleOffset = 0f;

        [SerializeField]
        [Range(5f, 90f)]
        [Tooltip("The angle from the pointer's forward position that will activate the teleport.")]
        private float teleportActivationAngle = 45f;

        [SerializeField]
        [Range(5f, 90f)]
        [Tooltip("The angle from the joystick left and right position that will activate a rotation")]
        private float rotateActivationAngle = 22.5f;

        [SerializeField]
        [Range(5f, 180f)]
        [Tooltip("The amount to rotate the camera when rotation is activated")]
        private float rotationAmount = 90f;

        [SerializeField]
        [Range(5, 90f)]
        [Tooltip("The angle from the joystick down position that will activate a strafe that will move the camera back")]
        private float backStrafeActivationAngle = 45f;

        [SerializeField]
        [Tooltip("The distance to move the camera when the strafe is activated")]
        private float strafeAmount = 0.25f;

        [SerializeField]
        [FormerlySerializedAs("LineColorHotSpot")]
        private Gradient lineColorHotSpot = new Gradient();

        protected Gradient LineColorHotSpot
        {
            get => lineColorHotSpot;
            set => lineColorHotSpot = value;
        }

        private bool currentDigitalInputState = false;
        private Vector2 currentDualAxisInputPosition = Vector2.zero;
        private bool teleportEnabled = false;

        private bool canTeleport = false;

        private bool canMove = false;

        private IMixedRealityTeleportValidationDataProvider validationDataProvider;
        private IMixedRealityTeleportValidationDataProvider ValidationDataProvider => validationDataProvider ?? (validationDataProvider = MixedRealityToolkit.GetService<IMixedRealityTeleportValidationDataProvider>());

        /// <summary>
        /// The result from the last raycast.
        /// </summary>
        public TeleportValidationResult TeleportValidationResult { get; private set; } = TeleportValidationResult.None;

        protected Gradient GetLineGradient(TeleportValidationResult targetResult)
        {
            switch (targetResult)
            {
                case TeleportValidationResult.None:
                    return LineColorNoTarget;
                case TeleportValidationResult.Valid:
                    return LineColorValid;
                case TeleportValidationResult.Invalid:
                    return LineColorInvalid;
                case TeleportValidationResult.HotSpot:
                    return lineColorHotSpot;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetResult), targetResult, null);
            }
        }

        #region IMixedRealityPointer Implementation

        /// <inheritdoc />
        public override bool IsInteractionEnabled => !IsTeleportRequestActive && teleportEnabled;

        /// <inheritdoc />
        public override float PointerOrientation
        {
            get
            {
                if (TeleportHotSpot != null &&
                    TeleportHotSpot.OverrideTargetOrientation &&
                    TeleportValidationResult == TeleportValidationResult.HotSpot)
                {
                    return TeleportHotSpot.TargetOrientation;
                }

                return base.PointerOrientation;
            }
            set => base.PointerOrientation = value;
        }

        public override void OnPreRaycast()
        {
            if (LineBase == null)
            {
                return;
            }

            // Make sure our array will hold
            if (Rays == null || Rays.Length != LineCastResolution)
            {
                Rays = new RayStep[LineCastResolution];
            }

            float stepSize = 1f / Rays.Length;
            Vector3 lastPoint = LineBase.GetUnClampedPoint(0f);

            for (int i = 0; i < Rays.Length; i++)
            {
                Vector3 currentPoint = LineBase.GetUnClampedPoint(stepSize * (i + 1));
                Rays[i].UpdateRayStep(ref lastPoint, ref currentPoint);
                lastPoint = currentPoint;
            }
        }

        public override void OnPostRaycast()
        {
            // Use the results from the last update to set our NavigationResult
            float clearWorldLength = 0f;
            TeleportValidationResult = TeleportValidationResult.None;

            if (IsInteractionEnabled)
            {
                LineBase.enabled = true;

                // If we hit something
                if (Result.CurrentPointerTarget != null)
                {
                    TeleportValidationResult = ValidationDataProvider.IsValid(Result, TeleportHotSpot);

                    // Use the step index to determine the length of the hit
                    for (int i = 0; i <= Result.RayStepIndex; i++)
                    {
                        if (i == Result.RayStepIndex)
                        {
                            if (MixedRealityRaycaster.DebugEnabled)
                            {
                                Color debugColor = TeleportValidationResult != TeleportValidationResult.None
                                    ? Color.yellow
                                    : Color.cyan;

                                Debug.DrawLine(Result.StartPoint + Vector3.up * 0.1f, Result.StartPoint + Vector3.up * 0.1f, debugColor);
                            }

                            // Only add the distance between the start point and the hit
                            clearWorldLength += Vector3.Distance(Result.StartPoint, Result.EndPoint);
                        }
                        else if (i < Result.RayStepIndex)
                        {
                            // Add the full length of the step to our total distance
                            clearWorldLength += Rays[i].Length;
                        }
                    }

                    // Clamp the end of the parabola to the result hit's point
                    LineBase.LineEndClamp = LineBase.GetNormalizedLengthFromWorldLength(clearWorldLength, LineCastResolution);
                    BaseCursor?.SetVisibility(TeleportValidationResult == TeleportValidationResult.Valid || TeleportValidationResult == TeleportValidationResult.HotSpot);
                }
                else
                {
                    BaseCursor?.SetVisibility(false);
                    LineBase.LineEndClamp = 1f;
                }

                // Set the line color
                for (int i = 0; i < LineRenderers.Length; i++)
                {
                    LineRenderers[i].LineColor = GetLineGradient(TeleportValidationResult);
                }
            }
            else
            {
                LineBase.enabled = false;
            }
        }

        #endregion IMixedRealityPointer Implementation

        #region IMixedRealityInputHandler Implementation

        /// <inheritdoc />
        public override void OnInputDown(InputEventData eventData)
        {
            // Don't process input if we've got an active teleport request in progress.
            if (eventData.used || IsTeleportRequestActive || TeleportSystem == null)
            {
                return;
            }

            if (eventData.SourceId == InputSourceParent.SourceId &&
                eventData.Handedness == Handedness &&
                eventData.MixedRealityInputAction == TeleportSystem.TeleportAction)
            {
                eventData.Use();
                ProcessDigitalTeleportInput(true);
            }
        }

        /// <inheritdoc />
        public override void OnInputUp(InputEventData eventData)
        {
            if (eventData.SourceId == InputSourceParent.SourceId &&
                eventData.Handedness == Handedness &&
                eventData.MixedRealityInputAction == TeleportSystem.TeleportAction)
            {
                eventData.Use();
                ProcessDigitalTeleportInput(false);
            }
        }

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<float> eventData)
        {
            // Don't process input if we've got an active teleport request in progress.
            if (eventData.used || IsTeleportRequestActive || TeleportSystem == null)
            {
                return;
            }

            if (eventData.SourceId == InputSourceParent.SourceId &&
                eventData.Handedness == Handedness &&
                eventData.MixedRealityInputAction == TeleportSystem.TeleportAction)
            {
                eventData.Use();
                ProcessSingleAxisTeleportInput(eventData);
            }
        }

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<Vector2> eventData)
        {
            // Don't process input if we've got an active teleport request in progress.
            if (eventData.used || IsTeleportRequestActive || TeleportSystem == null)
            {
                return;
            }

            if (eventData.SourceId == InputSourceParent.SourceId &&
                eventData.Handedness == Handedness &&
                eventData.MixedRealityInputAction == TeleportSystem.TeleportAction)
            {
                eventData.Use();
                ProcessDualAxisTeleportInput(eventData);
            }
        }

        #endregion IMixedRealityInputHandler Implementation

        #region IMixedRealityTeleportHandler Implementation

        /// <inheritdoc />
        public override void OnTeleportRequest(TeleportEventData eventData)
        {
            // Only turn off the pointer if we're not the one sending the request
            if (eventData.Pointer.PointerId == PointerId)
            {
                IsTeleportRequestActive = false;
            }
            else
            {
                IsTeleportRequestActive = true;
                BaseCursor?.SetVisibility(false);
            }
        }

        /// <inheritdoc />
        public override void OnTeleportCompleted(TeleportEventData eventData)
        {
            IsTeleportRequestActive = false;
            BaseCursor?.SetVisibility(false);
        }

        /// <inheritdoc />
        public override void OnTeleportCanceled(TeleportEventData eventData)
        {
            IsTeleportRequestActive = false;
            BaseCursor?.SetVisibility(false);
        }

        #endregion IMixedRealityTeleportHandler Implementation

        private void ProcessDigitalTeleportInput(bool isPressed)
        {
            currentDigitalInputState = isPressed;

            if (currentDigitalInputState && !teleportEnabled)
            {
                teleportEnabled = true;
                TeleportSystem?.RaiseTeleportRequest(this, TeleportHotSpot);
            }
            else if (!currentDigitalInputState)
            {
                bool isValid = TeleportValidationResult == TeleportValidationResult.Valid ||
                               TeleportValidationResult == TeleportValidationResult.HotSpot;

                if (teleportEnabled && isValid)
                {
                    teleportEnabled = false;
                    TeleportSystem?.RaiseTeleportStarted(this, TeleportHotSpot);
                }
                else if (teleportEnabled)
                {
                    teleportEnabled = false;
                    TeleportSystem?.RaiseTeleportCanceled(this, TeleportHotSpot);
                }
            }
        }

        private void ProcessSingleAxisTeleportInput(InputEventData<float> eventData) => ProcessDigitalTeleportInput(eventData.InputData > inputThreshold);

        private void ProcessDualAxisTeleportInput(InputEventData<Vector2> eventData)
        {
            currentDualAxisInputPosition = eventData.InputData;

            if (Mathf.Abs(currentDualAxisInputPosition.y) > inputThreshold ||
                Mathf.Abs(currentDualAxisInputPosition.x) > inputThreshold)
            {
                // Get the angle of the pointer input
                float angle = Mathf.Atan2(currentDualAxisInputPosition.x, currentDualAxisInputPosition.y) * Mathf.Rad2Deg;

                // Offset the angle so it's 'forward' facing
                angle += angleOffset;
                PointerOrientation = angle;

                if (!teleportEnabled)
                {
                    float absoluteAngle = Mathf.Abs(angle);

                    if (absoluteAngle < teleportActivationAngle)
                    {
                        teleportEnabled = true;

                        TeleportSystem?.RaiseTeleportRequest(this, TeleportHotSpot);
                    }
                    else if (canMove)
                    {
                        // wrap the angle value.
                        if (absoluteAngle > 180f)
                        {
                            absoluteAngle = Mathf.Abs(absoluteAngle - 360f);
                        }

                        // Calculate the offset rotation angle from the 90 degree mark.
                        // Half the rotation activation angle amount to make sure the activation angle stays centered at 90.
                        float offsetRotationAngle = 90f - rotateActivationAngle;

                        // subtract it from our current angle reading
                        offsetRotationAngle = absoluteAngle - offsetRotationAngle;

                        // if it's less than zero, then we don't have activation
                        if (offsetRotationAngle > 0)
                        {
                            var cameraRig = CameraSystem.MainCameraRig;

                            Debug.Assert(cameraRig != null, $"{nameof(TeleportPointer)} requires the {nameof(IMixedRealityCameraSystem)} be enabled with a valid {nameof(IMixedRealityCameraRig)}!");

                            // check to make sure we're still under our activation threshold.
                            if (offsetRotationAngle < rotateActivationAngle)
                            {
                                canMove = false;
                                // Rotate the camera by the rotation amount.  If our angle is positive then rotate in the positive direction, otherwise in the opposite direction.
                                cameraRig.PlayspaceTransform.RotateAround(cameraRig.CameraTransform.position, Vector3.up, angle >= 0.0f ? rotationAmount : -rotationAmount);
                            }
                            else // We may be trying to strafe backwards.
                            {
                                // Calculate the offset rotation angle from the 180 degree mark.
                                // Half the strafe activation angle to make sure the activation angle stays centered at 180f
                                float offsetStrafeAngle = 180f - backStrafeActivationAngle;
                                // subtract it from our current angle reading
                                offsetStrafeAngle = absoluteAngle - offsetStrafeAngle;

                                // Check to make sure we're still under our activation threshold.
                                if (offsetStrafeAngle > 0 && offsetStrafeAngle < backStrafeActivationAngle)
                                {
                                    canMove = false;
                                    var playspacePosition = cameraRig.PlayspaceTransform.position;
                                    var height = playspacePosition.y;
                                    var newPosition = -cameraRig.CameraTransform.forward * strafeAmount + playspacePosition;
                                    newPosition.y = height;
                                    cameraRig.PlayspaceTransform.position = newPosition;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (!canTeleport && !teleportEnabled)
                {
                    // Reset the move flag when the user stops moving the joystick
                    // but hasn't yet started teleport request.
                    canMove = true;
                }

                if (canTeleport)
                {
                    canTeleport = false;
                    teleportEnabled = false;

                    if (TeleportValidationResult == TeleportValidationResult.Valid ||
                        TeleportValidationResult == TeleportValidationResult.HotSpot)
                    {
                        TeleportSystem?.RaiseTeleportStarted(this, TeleportHotSpot);
                    }
                }

                if (teleportEnabled)
                {
                    canTeleport = false;
                    teleportEnabled = false;
                    TeleportSystem?.RaiseTeleportCanceled(this, TeleportHotSpot);
                }
            }

            if (teleportEnabled &&
                TeleportValidationResult == TeleportValidationResult.Valid ||
                TeleportValidationResult == TeleportValidationResult.HotSpot)
            {
                canTeleport = true;
            }
        }
    }
}