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
    public class FKPoseManipulation : PoseManipulation
    {

        internal Vector3 fromRotation;
        internal Quaternion initialRotation;

        public FKPoseManipulation(DirectController goalController, Transform mouthpiece)
        {
            oTransform = goalController.transform;
            ControllerRig = goalController.target.RootController;

            InitMatrices(mouthpiece);
            Transform origin = goalController.target.PathToRoot.Count > 0 ? goalController.target.PathToRoot[0] : goalController.transform;
            InitHierarchy(goalController, origin);
            InitFKData();
        }

        public override void SetDestination(Transform mouthpiece)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            Matrix4x4 transformed = InitialParentMatrixWorldToLocal *
                    transformation * InitialParentMatrix *
                    InitialTRS;
            Maths.DecomposeMatrix(transformed, out Vector3 position, out Quaternion rotation, out Vector3 scale);
            targetPosition = position;
            targetRotation = rotation;
        }

        public override bool TrySolver()
        {
            if (hierarchySize > 1)
            {
                Vector3 to = Quaternion.FromToRotation(Vector3.forward, targetPosition) * Vector3.forward;
                fullHierarchy[hierarchySize - 2].localRotation = initialRotation * Quaternion.FromToRotation(fromRotation, to);
                endRotations[0] = fullHierarchy[hierarchySize - 2].localRotation;
            }
            else
            {
                oTransform.localPosition = targetPosition;
                endPositions[0] = targetPosition;
            }
            oTransform.localRotation = targetRotation;
            endRotations[endRotations.Count - 1] = targetRotation;
            return true;
        }


        internal virtual void InitFKData()
        {
            movedObjects = new List<GameObject>();
            startPositions = new List<Vector3>();
            endPositions = new List<Vector3>();

            startRotations = new List<Quaternion>();
            endRotations = new List<Quaternion>();

            startScales = new List<Vector3>();
            endScales = new List<Vector3>();
            fromRotation = Quaternion.FromToRotation(Vector3.forward, oTransform.localPosition) * Vector3.forward;
            if (hierarchySize > 2)
            {
                initialRotation = fullHierarchy[hierarchySize - 2].localRotation;

                movedObjects.Add(fullHierarchy[hierarchySize - 2].gameObject);
                startPositions.Add(fullHierarchy[hierarchySize - 2].localPosition);
                endPositions.Add(fullHierarchy[hierarchySize - 2].localPosition);

                startRotations.Add(fullHierarchy[hierarchySize - 2].localRotation);
                endRotations.Add(fullHierarchy[hierarchySize - 2].localRotation);

                startScales.Add(fullHierarchy[hierarchySize - 2].localScale);
                endScales.Add(fullHierarchy[hierarchySize - 2].localScale);
            }

            movedObjects.Add(oTransform.gameObject);
            startPositions.Add(oTransform.localPosition);
            endPositions.Add(oTransform.localPosition);
            startRotations.Add(oTransform.localRotation);
            endRotations.Add(oTransform.localRotation);
            startScales.Add(oTransform.localScale);
            endScales.Add(oTransform.localScale);
        }

    }

}