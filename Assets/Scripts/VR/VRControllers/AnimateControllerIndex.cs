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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class AnimateControllerIndex : AnimateControllerAbstract
    {
        private float triggerRotationAmplitude = 12f;
        private float joystickRotationAmplitude = 17f;
        private float primaryTranslationAmplitude = -0.001f;
        private float secondaryTranslationAmplitude = -0.001f;

        protected override void AnimateGrip(float gripAmount)
        {
            gripTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", gripAmount > 0.01f ? UIOptions.SelectedColor : Color.black);
        }

        protected override void AnimateJoystick(Vector2 joystick)
        {
            joystickTransform.localRotation = initJoystickRotation * Quaternion.Euler(joystick.y * joystickRotationAmplitude, joystick.x * joystickRotationAmplitude, 0);
            joystickTransform.gameObject.GetComponentInChildren<MeshRenderer>().materials[0].SetColor("_BaseColor", joystick.magnitude > 0.05f ? UIOptions.SelectedColor : Color.black);
        }

        protected override void AnimatePrimaryButton(bool primaryState)
        {
            primaryTransform.localPosition = initPrimaryTranslation;
            primaryTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", primaryState ? UIOptions.SelectedColor : Color.black);
            if (primaryState)
            {
                primaryTransform.localPosition += new Vector3(0, primaryTranslationAmplitude, 0); // TODO: quick anim? CoRoutine.
            }
        }

        protected override void AnimateSecondaryButton(bool secondaryState)
        {
            secondaryTransform.localPosition = initSecondaryTranslation;
            secondaryTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", secondaryState ? UIOptions.SelectedColor : Color.black);
            if (secondaryState)
            {
                secondaryTransform.localPosition += new Vector3(0, secondaryTranslationAmplitude, 0); // TODO: quick anim? CoRoutine.
            }
        }

        protected override void AnimateTrigger(float triggerAmount)
        {
            triggerTransform.localRotation = initTriggerRotation * Quaternion.Euler(triggerAmount * -triggerRotationAmplitude, 0, 0);
            triggerTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", triggerAmount > 0.01f ? UIOptions.SelectedColor : Color.black);
        }
    }
}
