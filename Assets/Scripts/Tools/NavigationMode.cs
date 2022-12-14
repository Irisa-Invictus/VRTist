﻿/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using UnityEngine;

namespace VRtist
{
    public class NavigationMode
    {
        public NavigationOptions options;

        protected Transform rig = null;
        protected Transform world = null;
        protected Transform paletteController = null;
        protected Transform toolsController = null;
        protected Transform pivot = null;
        protected Transform camera = null;
        protected Transform parameters = null;

        public UsedControls usedControls = UsedControls.NONE;

        // Clip Planes config. Can be set back to PlayerController if we need tweaking.
        private float nearPlane = 0.1f; // 10 cm, close enough to not clip the controllers.
        private float farPlane = 1000.0f; // 1km from us, far enough?

        protected enum ControllerVisibility { SHOW_NORMAL, HIDE, SHOW_GRIP };

        [System.Flags]
        public enum UsedControls
        {
            NONE = (1 << 0),

            LEFT_JOYSTICK = (1 << 1),
            LEFT_JOYSTICK_CLICK = (1 << 2),
            LEFT_TRIGGER = (1 << 3),
            LEFT_GRIP = (1 << 4),
            LEFT_PRIMARY = (1 << 5),
            LEFT_SECONDARY = (1 << 6),

            RIGHT_JOYSTICK = (1 << 7),
            RIGHT_JOYSTICK_CLICK = (1 << 8),
            RIGHT_TRIGGER = (1 << 9),
            RIGHT_GRIP = (1 << 10),
            RIGHT_PRIMARY = (1 << 11),
            RIGHT_SECONDARY = (1 << 12)
        }

        public static bool HasFlag(UsedControls a, UsedControls b)
        {
            return (a & b) == b;
        }

        //
        // Virtual functions used for navigation by the PlayerController
        //

        // Pass only rig and world and Find("") the other nodes?
        public virtual void Init(Transform rigTransform, Transform worldTransform, Transform leftHandleTransform, Transform rightHandleTransform, Transform pivotTransform, Transform cameraTransform, Transform parametersTransform)
        {
            rig = rigTransform;
            world = worldTransform;
            paletteController = leftHandleTransform;
            toolsController = rightHandleTransform;
            pivot = pivotTransform;
            camera = cameraTransform;
            parameters = parametersTransform;

            UpdateCameraClipPlanes();
        }

        public virtual void DeInit() { }

        public virtual void Update() { }

        //
        // Common Utils
        //
        protected void UpdateCameraClipPlanes()
        {            
            float scale = 1f / GlobalState.WorldScale;
            Camera.main.nearClipPlane = nearPlane * scale;
            Camera.main.farClipPlane = farPlane * scale;
        }

        protected void SetLeftControllerVisibility(ControllerVisibility visibility)
        {
            paletteController.localScale = visibility == ControllerVisibility.HIDE ? Vector3.zero : Vector3.one;
        }
    }
}
