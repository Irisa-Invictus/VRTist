/* MIT License
 *
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

    public class PoseProperty
    {
        public DirectController controller;
        private Transform transform;
        private Transform rootTransform;
        private Transform targetTransform;
        public enum PropertyEnum { PositionX, PositionY, PositionZ, RotationX, RotationY, RotationZ };
        public PropertyEnum Property;

        public string GetString()
        {
            return controller.name + " / " + Property;
        }

        public PoseProperty(DirectController goal, Transform target, PropertyEnum property)
        {
            controller = goal;
            transform = goal.transform;
            rootTransform = target.GetComponent<JointController>().RootController.transform;
            Property = property;
            targetTransform = target;
        }

        public float GetValue()
        {
            return Property switch
            {
                PropertyEnum.PositionX => transform.localPosition.x,
                PropertyEnum.PositionY => transform.localPosition.y,
                PropertyEnum.PositionZ => transform.localPosition.z,
                PropertyEnum.RotationX => Mathf.DeltaAngle(0, transform.localEulerAngles.x),
                PropertyEnum.RotationY => Mathf.DeltaAngle(0, transform.localEulerAngles.y),
                PropertyEnum.RotationZ => Mathf.DeltaAngle(0, transform.localEulerAngles.z),
                _ => 0f
            };
        }

        public void Apply(float value)
        {
            switch (Property)
            {
                case PropertyEnum.PositionX: transform.localPosition += new Vector3(value, 0, 0); break;
                case PropertyEnum.PositionY: transform.localPosition += new Vector3(0, value, 0); break;
                case PropertyEnum.PositionZ: transform.localPosition += new Vector3(0, 0, value); break;
                case PropertyEnum.RotationX: transform.localRotation *= Quaternion.Euler(value, 0, 0); break;
                case PropertyEnum.RotationY: transform.localRotation *= Quaternion.Euler(0, value, 0); break;
                case PropertyEnum.RotationZ: transform.localRotation *= Quaternion.Euler(0, 0, value); break;
            }
        }

        public bool GetLockJS(Transform obj, ref double[] js)
        {
            switch (Property)
            {
                case PropertyEnum.RotationX: Rotation_dTheta(ref js, obj, 0, 50f); break;
                case PropertyEnum.RotationY: Rotation_dTheta(ref js, obj, 1, 50f); break;
                case PropertyEnum.RotationZ: Rotation_dTheta(ref js, obj, 2, 50f); break;
                case PropertyEnum.PositionX: Position_dTheta(ref js, obj, 0, 50f); break;
                case PropertyEnum.PositionY: Position_dTheta(ref js, obj, 1, 50f); break;
                case PropertyEnum.PositionZ: Position_dTheta(ref js, obj, 2, 50f); break;
            }
            return true;
        }

        public bool GetJs(ref double[] js)
        {
            switch (Property)
            {
                case PropertyEnum.RotationX: Rotation_dTheta(ref js, targetTransform, 0); break;
                case PropertyEnum.RotationY: Rotation_dTheta(ref js, targetTransform, 1); break;
                case PropertyEnum.RotationZ: Rotation_dTheta(ref js, targetTransform, 2); break;
                case PropertyEnum.PositionX: Position_dTheta(ref js, targetTransform, 0); break;
                case PropertyEnum.PositionY: Position_dTheta(ref js, targetTransform, 1); break;
                case PropertyEnum.PositionZ: Position_dTheta(ref js, targetTransform, 2); break;
            }
            return true;
        }

        private void Rotation_dTheta(ref double[] js, Transform target, int propIndex, float dtheta = 1f)
        {

            Quaternion currentRotation = transform.localRotation;

            Vector3 rotation = Vector3.zero;
            rotation[propIndex] = 1;
            transform.localRotation *= Quaternion.Euler(rotation);

            Vector3 plusPosition = rootTransform.InverseTransformPoint(target.position);
            Quaternion plusRotation = target.rotation;

            transform.localRotation = currentRotation;

            Vector3 minusPosition = rootTransform.transform.InverseTransformPoint(target.position);
            Quaternion minusRotation = target.rotation;

            js[0] = (plusPosition.x - minusPosition.x) * dtheta;
            js[1] = (plusPosition.y - minusPosition.y) * dtheta;
            js[2] = (plusPosition.z - minusPosition.z) * dtheta;
            js[3] = (plusRotation.x - minusRotation.x) * dtheta;
            js[4] = (plusRotation.y - minusRotation.y) * dtheta;
            js[5] = (plusRotation.z - minusRotation.z) * dtheta;
            js[6] = (plusRotation.w - minusRotation.w) * dtheta;
        }

        public void Position_dTheta(ref double[] js, Transform target, int propIndex, float dtheta = 1f)
        {
            Vector3 currentPosition = transform.localPosition;
            Vector3 movement = Vector3.zero;
            movement[propIndex] = 1;
            transform.localPosition += movement;

            Vector3 plusPosition = rootTransform.InverseTransformPoint(target.position);
            Quaternion plusRotation = target.rotation;

            transform.localPosition = currentPosition;

            Vector3 minusPosition = rootTransform.InverseTransformPoint(target.position);
            Quaternion minusRotation = target.rotation;

            js[0] = (plusPosition.x - minusPosition.x) * dtheta;
            js[1] = (plusPosition.y - minusPosition.y) * dtheta;
            js[2] = (plusPosition.z - minusPosition.z) * dtheta;
            js[3] = (plusRotation.x - minusRotation.x) * dtheta;
            js[4] = (plusRotation.y - minusRotation.y) * dtheta;
            js[5] = (plusRotation.z - minusRotation.z) * dtheta;
            js[6] = (plusRotation.w - minusRotation.w) * dtheta;
        }

        public double GetStifness()
        {
            return transform == targetTransform ? 0 : controller.stiffness;
        }

        public float GetLowerBound()
        {
            return Property switch
            {
                PropertyEnum.RotationX => GetRotationLowerBound(0),
                PropertyEnum.RotationY => GetRotationLowerBound(1),
                PropertyEnum.RotationZ => GetRotationLowerBound(2),
                _ => -10
            };
        }

        private float GetRotationLowerBound(int propIndex)
        {
            Quaternion currentRotation = transform.localRotation;
            return Maths.RotationBounds(currentRotation, controller.LowerAngleBound)[propIndex];
        }

        public float GetUpperBound()
        {
            return Property switch
            {
                PropertyEnum.RotationX => GetRotationUpperBound(0),
                PropertyEnum.RotationY => GetRotationUpperBound(1),
                PropertyEnum.RotationZ => GetRotationUpperBound(2),
                _ => 10
            };
        }

        private float GetRotationUpperBound(int propIndex)
        {
            Quaternion currentRotation = transform.localRotation;
            return Maths.RotationBounds(currentRotation, controller.UpperAngleBound)[propIndex];
        }

    }
}
