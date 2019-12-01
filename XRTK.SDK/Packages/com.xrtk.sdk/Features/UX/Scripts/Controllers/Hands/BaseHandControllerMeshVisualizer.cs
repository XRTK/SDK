﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.EventDatum.Input;
using XRTK.Providers.Controllers.Hands;

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Base hand controller visualizer for hand meshes.
    /// </summary>
    public class BaseHandControllerMeshVisualizer : BaseHandControllerVisualizer
    {
        private DefaultMixedRealityControllerVisualizer controllerVisualizer;

        /// <summary>
        /// The mesh filter used for visualization.
        /// </summary>
        protected MeshFilter MeshFilter { get; set; }

        protected override void Start()
        {
            base.Start();
            controllerVisualizer = GetComponent<DefaultMixedRealityControllerVisualizer>();
        }

        protected override void OnDestroy()
        {
            ClearMesh();
            base.OnDestroy();
        }

        public override void OnHandDataUpdated(InputEventData<HandData> eventData)
        {
            if (eventData.Handedness != controllerVisualizer.Controller?.ControllerHandedness)
            {
                return;
            }

            if (Profile == null || !Profile.EnableHandMeshVisualization || eventData.InputData.Mesh == null)
            {
                ClearMesh();
                return;
            }

            HandMeshData handMeshData = eventData.InputData.Mesh;
            if (handMeshData.Empty)
            {
                return;
            }

            if (MeshFilter == null && Profile?.HandMeshPrefab != null)
            {
                CreateMesh();
            }

            if (MeshFilter != null)
            {
                Mesh mesh = MeshFilter.mesh;

                mesh.vertices = handMeshData.Vertices;
                mesh.normals = handMeshData.Normals;
                mesh.triangles = handMeshData.Triangles;

                if (handMeshData.Uvs != null && handMeshData.Uvs.Length > 0)
                {
                    mesh.uv = handMeshData.Uvs;
                }

                MeshFilter.transform.position = handMeshData.Position;
                MeshFilter.transform.rotation = handMeshData.Rotation;
            }
        }

        protected virtual void ClearMesh()
        {
            if (MeshFilter != null)
            {
                Destroy(MeshFilter.gameObject);
            }
        }

        protected virtual void CreateMesh()
        {
            MeshFilter = Instantiate(Profile.HandMeshPrefab).GetComponent<MeshFilter>();
        }
    }
}