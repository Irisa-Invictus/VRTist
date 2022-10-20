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
    public class FKPositionGizmoPose : PoseManipulation
    {
        private Vector3 fromRotation;
        private Vector3 acAxis;
        private Quaternion initialRotation;
        private Transform parent;

        public FKPositionGizmoPose(DirectController goalController, Transform mouthpiece, AnimationTool.Vector3Axis axis)
        {
            isGizmo = true;
            ControllerRig = goalController.target.RootController;
            oTransform = goalController.transform;
            InitMatrices(mouthpiece);
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
            if (goalController.target.PathToRoot.Count > 0) parent = goalController.target.PathToRoot[goalController.target.PathToRoot.Count - 1];
            acAxis = oTransform.parent.InverseTransformVector(acAxis);
            InitFKData();
        }

        public override void SetDestination(Transform mouthpiece)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            Matrix4x4 transformed = InitialParentMatrixWorldToLocal *
                    transformation * InitialParentMatrix *
                    InitialTRS;
            Maths.DecomposeMatrix(transformed, out targetPosition, out Quaternion rotation, out Vector3 scale);
            Vector3 movement = targetPosition - oTransform.localPosition;
            Vector3 movementProj = Vector3.Project(movement, acAxis);
            targetPosition = oTransform.localPosition + movementProj;

            Maths.DecomposeMatrix(oTransform.parent.worldToLocalMatrix * initialTransformMatrix, out Vector3 p, out targetRotation, out Vector3 s);
        }

        public override bool TrySolver()
        {
            if (parent != null)
            {
                Vector3 to = Quaternion.FromToRotation(Vector3.forward, targetPosition) * Vector3.forward;
                parent.localRotation = initialRotation * Quaternion.FromToRotation(fromRotation, to);
                endRotations[1] = parent.localRotation;
            }
            else
            {
                oTransform.localPosition = targetPosition;
                endPositions[0] = targetPosition;
            }
            return true;
        }

        private void InitFKData()
        {
            movedObjects = new List<GameObject>() { oTransform.gameObject };
            startPositions = new List<Vector3>() { oTransform.localPosition };
            endPositions = new List<Vector3>() { oTransform.localPosition };

            startRotations = new List<Quaternion>() { oTransform.localRotation };
            endRotations = new List<Quaternion>() { oTransform.localRotation };

            startScales = new List<Vector3> { oTransform.localScale };
            endScales = new List<Vector3> { oTransform.localScale };

            fromRotation = Quaternion.FromToRotation(Vector3.forward, oTransform.localPosition) * Vector3.forward;
            if (parent != null)
            {
                initialRotation = parent.localRotation;

                movedObjects.Add(parent.gameObject);
                startPositions.Add(parent.localPosition);
                endPositions.Add(parent.localPosition);

                startRotations.Add(parent.localRotation);
                endRotations.Add(parent.localRotation);

                startScales.Add(parent.localScale);
                endScales.Add(parent.localScale);
            }
        }
    }

}