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

    public class FKRotationGizmoPose : PoseManipulation
    {
        private Transform gizmo;
        private Vector3 acAxis;
        private Vector3 initForward;
        private float previousAngle;

        public FKRotationGizmoPose(DirectController goalController, Transform mouthpiece, Transform gizmo, AnimationTool.Vector3Axis axis)
        {
            isGizmo = true;
            ControllerRig = goalController.target.RootController;
            oTransform = goalController.transform;
            this.gizmo = gizmo;

            switch (axis)
            {
                case AnimationTool.Vector3Axis.X:
                    acAxis = oTransform.right;
                    initForward = oTransform.up;
                    break;
                case AnimationTool.Vector3Axis.Y:
                    acAxis = oTransform.up;
                    initForward = oTransform.forward;
                    break;
                case AnimationTool.Vector3Axis.Z:
                    acAxis = oTransform.forward;
                    initForward = oTransform.right;
                    break;
            }
            previousAngle = Vector3.SignedAngle(initForward, mouthpiece.position - gizmo.position, acAxis);
            movedObjects = new List<GameObject>() { oTransform.gameObject };
            startPositions = new List<Vector3>() { oTransform.localPosition };
            endPositions = new List<Vector3>() { oTransform.localPosition };
            startRotations = new List<Quaternion>() { oTransform.localRotation };
            endRotations = new List<Quaternion>() { oTransform.localRotation };
            startScales = new List<Vector3>() { oTransform.localScale };
            endScales = new List<Vector3>() { oTransform.localScale };
        }

        public override void SetDestination(Transform mouthpiece)
        {
            Vector3 projection = Vector3.ProjectOnPlane(mouthpiece.position - gizmo.position, acAxis);
            float currentAngle = Vector3.SignedAngle(initForward, projection, acAxis);
            float angleOffset = Mathf.DeltaAngle(previousAngle, currentAngle);
            oTransform.Rotate(acAxis, angleOffset, Space.World);
            endRotations[0] = oTransform.localRotation;
            previousAngle = currentAngle;
        }

        public override bool TrySolver()
        {
            return true;
        }
    }

}