﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;
using XRTK.Providers.Controllers.Hands;

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Base hand controller visualizer for hand joints.
    /// </summary>
    public class BaseHandControllerJointVisualizer : BaseHandControllerVisualizer
    {
        private DefaultMixedRealityControllerVisualizer controllerVisualizer;
        private Dictionary<TrackedHandJoint, Transform> jointTransforms;

        /// <summary>
        /// Provides read-only access to the joint transforms used for visualization.
        /// </summary>
        public IReadOnlyDictionary<TrackedHandJoint, Transform> JointTransforms => jointTransforms;

        protected override void Start()
        {
            base.Start();

            jointTransforms = new Dictionary<TrackedHandJoint, Transform>();
            controllerVisualizer = GetComponent<DefaultMixedRealityControllerVisualizer>();
        }

        protected override void OnDestroy()
        {
            ClearJoints();
            base.OnDestroy();
        }

        public override void OnHandDataUpdated(InputEventData<HandData> eventData)
        {
            if (eventData.Handedness != controllerVisualizer.Controller?.ControllerHandedness)
            {
                return;
            }

            if (Profile == null || !Profile.EnableHandJointVisualization)
            {
                ClearJoints();
            }
            else
            {
                Dictionary<TrackedHandJoint, MixedRealityPose> jointPoses = HandUtils.ToJointPoseDictionary(eventData.InputData.Joints);
                foreach (TrackedHandJoint handJoint in jointPoses.Keys)
                {
                    if (jointTransforms.TryGetValue(handJoint, out Transform jointTransform))
                    {
                        jointTransform.position = jointPoses[handJoint].Position;
                        jointTransform.rotation = jointPoses[handJoint].Rotation;
                    }
                    else if (handJoint != TrackedHandJoint.None)
                    {
                        CreateJoint(handJoint, jointPoses);
                    }
                }
            }
        }

        protected virtual void ClearJoints()
        {
            foreach (var joint in jointTransforms)
            {
                Destroy(joint.Value.gameObject);
            }

            jointTransforms.Clear();
        }

        protected virtual void CreateJoint(TrackedHandJoint handJoint, Dictionary<TrackedHandJoint, MixedRealityPose> jointPoses)
        {
            GameObject prefab = Profile.JointPrefab;
            if (handJoint == TrackedHandJoint.Palm)
            {
                prefab = Profile.PalmJointPrefab;
            }
            else if (handJoint == TrackedHandJoint.IndexTip)
            {
                prefab = Profile.FingerTipPrefab;
            }

            GameObject jointObject;
            if (prefab != null)
            {
                jointObject = Instantiate(prefab);
            }
            else
            {
                jointObject = new GameObject();
            }

            jointObject.name = handJoint.ToString() + " Proxy Transform";
            jointObject.transform.position = jointPoses[handJoint].Position;
            jointObject.transform.rotation = jointPoses[handJoint].Rotation;
            jointObject.transform.parent = transform;

            jointTransforms.Add(handJoint, jointObject.transform);
        }
    }
}