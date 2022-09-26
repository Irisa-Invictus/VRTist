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
    public abstract class PoseManipulation
    {

        public RigController ControllerRig;
        public Transform oTransform;

        public bool isGizmo = false;

        internal Vector3 targetPosition;
        internal Quaternion targetRotation;

        public List<GameObject> movedObjects = new List<GameObject>();
        internal List<Vector3> startPositions = new List<Vector3>();
        internal List<Quaternion> startRotations = new List<Quaternion>();
        internal List<Vector3> startScales = new List<Vector3>();
        internal List<Vector3> endPositions = new List<Vector3>();
        internal List<Quaternion> endRotations = new List<Quaternion>();
        internal List<Vector3> endScales = new List<Vector3>();

        internal Matrix4x4 InitialParentMatrixWorldToLocal;
        internal Matrix4x4 InitialParentMatrix;
        internal Matrix4x4 initialMouthMatrix;
        internal Matrix4x4 initialTransformMatrix;
        internal Matrix4x4 InitialTRS;

        internal List<Transform> fullHierarchy;
        internal int hierarchySize;

        public abstract void SetDestination(Transform mouthpiece);

        public abstract bool TrySolver();


        public CommandMoveObjects GetCommand()
        {
            return new CommandMoveObjects(movedObjects, startPositions, startRotations, startScales, endPositions, endRotations, endScales);
        }

        internal virtual void InitMatrices(Transform mouthpiece)
        {
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            InitialParentMatrix = oTransform.parent.localToWorldMatrix;
            InitialParentMatrixWorldToLocal = oTransform.parent.worldToLocalMatrix;
            InitialTRS = Matrix4x4.TRS(oTransform.localPosition, oTransform.localRotation, oTransform.localScale);
            initialTransformMatrix = oTransform.localToWorldMatrix;
        }

        internal virtual Transform InitHierarchy(DirectController Target, Transform origin)
        {
            fullHierarchy = new List<Transform>();
            if (origin == null) origin = Target.target.PathToRoot[Target.target.PathToRoot.Count - 2];
            fullHierarchy.Add(origin);
            int index = Target.target.PathToRoot.IndexOf(origin) + 1;

            if (index >= 0)
            {
                for (int i = index; i < Target.target.PathToRoot.Count; i++)
                {
                    fullHierarchy.Add(Target.target.PathToRoot[i]);
                }
            }
            if (Target.transform != origin) fullHierarchy.Add(Target.transform);
            hierarchySize = fullHierarchy.Count;
            return origin;
        }

    }
}
