/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 * &
 * Université de Rennes 1 / Invictus Project
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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public abstract class AnimateControllerAbstract : MonoBehaviour
    {
        protected Transform gripTransform;
        protected Quaternion initGripRotation;

        protected Transform triggerTransform;
        protected Quaternion initTriggerRotation;

        protected Transform joystickTransform;
        protected Quaternion initJoystickRotation;

        protected Transform primaryTransform;
        protected Vector3 initPrimaryTranslation;

        protected Transform secondaryTransform;
        protected Vector3 initSecondaryTranslation;

        public bool isPrimaryController = true;
        protected InputDevice device;

        public enum GripDirection { Left = -1, Right = 1 }
        public GripDirection gripDirection = GripDirection.Right;

        // Start is called before the first frame update
        void Start()
        {
            CaptureController();
            CaptureInitialTransforms();
        }
        private void CaptureController()
        {
            if (isPrimaryController) device = VRInput.primaryController;
            else device = VRInput.secondaryController;
        }

        protected virtual void CaptureInitialTransforms()
        {
            gripTransform = transform.Find("GripButtonPivot/GripButton");
            if (null != gripTransform)
            {
                initGripRotation = gripTransform.localRotation;
            }

            triggerTransform = transform.Find("TriggerButtonPivot/TriggerButton");
            if (null != triggerTransform)
            {
                initTriggerRotation = triggerTransform.localRotation;
            }

            joystickTransform = transform.Find("PrimaryAxisPivot");
            if (null != joystickTransform)
            {
                initJoystickRotation = joystickTransform.localRotation;
            }

            primaryTransform = transform.Find("PrimaryButtonPivot/PrimaryButton");
            if (null != primaryTransform)
            {
                initPrimaryTranslation = primaryTransform.localPosition;
            }

            secondaryTransform = transform.Find("SecondaryButtonPivot/SecondaryButton");
            if (null != secondaryTransform)
            {
                initSecondaryTranslation = secondaryTransform.localPosition;
            }
        }

        public void OnRightHanded(bool isRightHanded)
        {
            // TODO: handle what needs to be handled when we change hands.

            //gripDirection = isRightHanded ? 1.0f : -1.0f;
        }

        // Update is called once per frame
        void Update()
        {
            if (!device.isValid)
            {
                CaptureController();
                CaptureInitialTransforms();
            }

            // GRIP
            if (null != gripTransform)
            {
                float gripAmount = VRInput.GetValue(device, CommonUsages.grip);
                AnimateGrip(gripAmount);
            }

            // TRIGGER
            if (null != triggerTransform)
            {
                float triggerAmout = VRInput.GetValue(device, CommonUsages.trigger);
                AnimateTrigger(triggerAmout);
            }

            // JOYSTICK
            if (null != joystickTransform)
            {
                Vector2 joystick = VRInput.GetValue(device, CommonUsages.primary2DAxis);
                AnimateJoystick(joystick);
            }

            // PRIMARY
            if (null != primaryTransform)
            {
                bool primaryState = VRInput.GetValue(device, CommonUsages.primaryButton);
                AnimatePrimaryButton(primaryState);
            }

            // SECONDARY
            if (null != secondaryTransform)
            {
                bool secondaryState = VRInput.GetValue(device, CommonUsages.secondaryButton);
                AnimateSecondaryButton(secondaryState);
            }
        }

        protected abstract void AnimateSecondaryButton(bool secondaryState);
        protected abstract void AnimatePrimaryButton(bool primaryState);
        protected abstract void AnimateJoystick(Vector2 joystick);
        protected abstract void AnimateTrigger(float triggerAmount);
        protected abstract void AnimateGrip(float gripAmount);
    }
}
