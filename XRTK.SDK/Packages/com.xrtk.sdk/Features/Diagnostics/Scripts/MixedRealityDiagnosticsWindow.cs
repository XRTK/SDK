﻿// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using TMPro;
using UnityEngine;
using XRTK.SDK.Utilities.Solvers;
using XRTK.Services;

namespace XRTK.SDK.DiagnosticsSystem
{
    public class MixedRealityDiagnosticsWindow : MonoBehaviour
    {
        private SolverHandler solverHandler;

        [SerializeField]
        [Tooltip("The text component used to display the application build version and identifier.")]
        private TextMeshProUGUI applicationSignatureText = null;

        /// <summary>
        /// The diagnostics window was enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            applicationSignatureText.text = MixedRealityToolkit.DiagnosticsSystem.ApplicationSignature;
        }

        /// <summary>
        /// Pins or unpins the diagnostics window at its current location.
        /// </summary>
        /// <param name="isPinned"></param>
        public void Toggle_PinWindow(bool isPinned)
        {
            if (solverHandler == null)
            {
                solverHandler = MixedRealityToolkit.DiagnosticsSystem.DiagnosticsWindow.GetComponent<SolverHandler>();
            }

            if (solverHandler != null)
            {
                solverHandler.enabled = !isPinned;
            }
        }
    }
}