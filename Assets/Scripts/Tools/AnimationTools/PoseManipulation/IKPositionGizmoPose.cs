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

    public class IKPositionGizmoPose : IKPoseManipulation
    {
        private Vector3 previousPosition;
        private Vector3 acAxis;

        public IKPositionGizmoPose(DirectController goalController, Transform mouthpiece, AnimationTool.Vector3Axis axis, Transform origin = null, List<JointController> locks = null) : base(goalController, mouthpiece, origin, locks)
        {

            isGizmo = true;
            previousPosition = mouthpiece.position;
            switch (axis)
            {
                case AnimationTool.Vector3Axis.X:
                    acAxis = oTransform.right;
                    break;
                case AnimationTool.Vector3Axis.Y:
                    acAxis = oTransform.up;
                    break;
                case AnimationTool.Vector3Axis.Z:
                    acAxis = oTransform.forward;
                    break;
            }
        }

        public override void SetDestination(Transform mouthpiece)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            Matrix4x4 target = transformation * initialTransformMatrix;
            Maths.DecomposeMatrix(target, out targetPosition, out Quaternion rotation, out Vector3 scale);
            Maths.DecomposeMatrix(initialTransformMatrix, out Vector3 initialPosition, out Quaternion initialRotation, out Vector3 s);
            Vector3 movement = targetPosition - initialPosition;
            Vector3 movementProj = Vector3.Project(movement, acAxis);
            targetPosition = initialPosition + movementProj;
            targetRotation = initialRotation * Quaternion.Euler(-180, 0, 0);
        }


    }

}