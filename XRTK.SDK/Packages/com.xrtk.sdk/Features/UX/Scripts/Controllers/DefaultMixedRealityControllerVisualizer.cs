﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.SDK.Input.Handlers;
using XRTK.Providers.Controllers.Hands;
using XRTK.Services;
using XRTK.Definitions.Controllers;
using XRTK.SDK.UX.Controllers.Hands;

namespace XRTK.SDK.UX.Controllers
{
    /// <summary>
    /// The Mixed Reality Visualization component is primarily responsible for synchronizing the user's current input with controller models.
    /// </summary>
    /// <seealso cref="MixedRealityControllerVisualizationProfile"/>
    public class DefaultMixedRealityControllerVisualizer : ControllerPoseSynchronizer, IMixedRealityControllerVisualizer
    {
        /// <inheritdoc />
        public GameObject GameObjectProxy
        {
            get
            {
                try
                {
                    return gameObject;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        protected override void Start()
        {
            base.Start();

            if (typeof(BaseHandController).IsAssignableFrom(Controller.GetType()))
            {
                MixedRealityHandControllerVisualizationProfile handControllerVisualizationProfile = MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.HandVisualizationProfile;

                if (handControllerVisualizationProfile.EnableHandJointVisualization)
                {
                    Type jointVisualizerType = handControllerVisualizationProfile.HandJointVisualizer.Type;
                    BaseHandControllerVisualizer jointVisualizer = (BaseHandControllerVisualizer)GameObjectProxy.AddComponent(jointVisualizerType);
                    jointVisualizer.Handedness = Controller.ControllerHandedness;
                }

                if (handControllerVisualizationProfile.EnableHandMeshVisualization)
                {
                    Type meshVisualizerType = handControllerVisualizationProfile.HandMeshVisualizer.Type;
                    BaseHandControllerVisualizer meshVisualizer = (BaseHandControllerVisualizer)GameObjectProxy.AddComponent(meshVisualizerType);
                    meshVisualizer.Handedness = Controller.ControllerHandedness;
                }
            }
        }
    }
}