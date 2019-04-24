﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using UnityEditor;
using XRTK.SDK.UX.Pointers;

namespace XRTK.SDK.Inspectors.UX.Pointers
{
    [CustomEditor(typeof(MousePointer))]
    public class MousePointerInspector : BaseControllerPointerInspector
    {
        private SerializedProperty hideCursorWhenInactive;
        private SerializedProperty hideTimeout;
        private SerializedProperty movementThresholdToUnHide;
        private SerializedProperty speed;
        private bool mousePointerFoldout = true;

        protected override void OnEnable()
        {
            DrawBasePointerActions = false;
            base.OnEnable();

            hideCursorWhenInactive = serializedObject.FindProperty("hideCursorWhenInactive");
            movementThresholdToUnHide = serializedObject.FindProperty("movementThresholdToUnHide");
            hideTimeout = serializedObject.FindProperty("hideTimeout");
            speed = serializedObject.FindProperty("speed");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            mousePointerFoldout = EditorGUILayout.Foldout(mousePointerFoldout, "Mouse Pointer Settings", true);

            if (mousePointerFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(hideCursorWhenInactive);

                if (hideCursorWhenInactive.boolValue)
                {
                    EditorGUILayout.PropertyField(hideTimeout);
                    EditorGUILayout.PropertyField(movementThresholdToUnHide);
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.PropertyField(speed);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}