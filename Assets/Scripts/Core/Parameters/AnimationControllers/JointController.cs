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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRtist
{
    public class JointController : MonoBehaviour
    {

        public List<Transform> PathToRoot = new List<Transform>();
        public List<AnimationSet> AnimToRoot = new List<AnimationSet>();
        public AnimationSet Animation { get { return GlobalState.Animation.GetObjectAnimation(gameObject); } }
        public RigController RootController;
        public bool ShowCurve = false;
        public Color color;
        public JointController LinkJoint;
        //public List<ObjectController> controllers = new List<ObjectController>();

        public Transform Parent { get { return PathToRoot.Count == 0 ? null : PathToRoot[PathToRoot.Count - 1]; } }

        bool updated = false;


        public void MoveJoint(Matrix4x4 localMatrix)
        {
            if (updated) return;
            updated = true;
            Matrix4x4 currentMatrix = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
            Matrix4x4 offset = localMatrix * currentMatrix.inverse;
            Maths.DecomposeMatrix(localMatrix, out Vector3 position, out Quaternion rotation, out Vector3 scale);
            transform.localPosition = position;
            transform.localRotation = rotation;
            transform.localScale = scale;
            if (LinkJoint != null) LinkJoint.MoveJoint(localMatrix);
            updated = false;
        }

        public void UpdateController()
        {
            //controllers.ForEach(x => x.MoveController());
        }

        public void SetPathToRoot(RigController controller, List<Transform> path)
        {
            path.ForEach(x =>
            {
                AnimationSet anim = GlobalState.Animation.GetOrCreateObjectAnimation(x.gameObject);
                PathToRoot.Add(x);

                AnimToRoot.Add(anim);
            });
            RootController = controller;
        }

        public Vector3 FramePosition(int frame)
        {
            if (null == Animation) return Vector3.zero;

            AnimationSet rootAnimation = GlobalState.Animation.GetObjectAnimation(RootController.gameObject);
            Matrix4x4 trsMatrix = RootController.transform.parent.localToWorldMatrix;
            if (null != rootAnimation) trsMatrix = trsMatrix * rootAnimation.GetTRSMatrix(frame);
            else trsMatrix = trsMatrix * Matrix4x4.TRS(RootController.transform.localPosition, RootController.transform.localRotation, RootController.transform.localScale);

            for (int i = 0; i < PathToRoot.Count; i++)
            {
                if (null != AnimToRoot[i])
                    trsMatrix = trsMatrix * AnimToRoot[i].GetTRSMatrix(frame);
            }
            trsMatrix = trsMatrix * Animation.GetTRSMatrix(frame);

            Maths.DecomposeMatrix(trsMatrix, out Vector3 parentPosition, out Quaternion quaternion, out Vector3 scale);
            return parentPosition;
        }

        /// <summary>
        /// local to world matrice at frame
        /// </summary>
        public Matrix4x4 MatrixAtFrame(int frame)
        {
            if (null == Animation) return Matrix4x4.identity;

            AnimationSet rootAnimation = GlobalState.Animation.GetObjectAnimation(RootController.gameObject);
            Matrix4x4 trsMatrix = RootController.transform.parent.localToWorldMatrix;
            if (null != rootAnimation) trsMatrix = trsMatrix * rootAnimation.GetTRSMatrix(frame);
            else trsMatrix = trsMatrix * Matrix4x4.TRS(RootController.transform.localPosition, RootController.transform.localRotation, RootController.transform.localScale);

            for (int i = 0; i < PathToRoot.Count; i++)
            {
                trsMatrix = trsMatrix * AnimToRoot[i].GetTRSMatrix(frame);
            }
            trsMatrix = trsMatrix * Animation.GetTRSMatrix(frame);
            return trsMatrix;
        }

        public Matrix4x4 ParentMatrixAtFrame(int frame)
        {
            if (null == Animation) return Matrix4x4.identity;

            AnimationSet rootAnimation = GlobalState.Animation.GetObjectAnimation(RootController.gameObject);
            Matrix4x4 trsMatrix = RootController.transform.parent.localToWorldMatrix;
            if (null != rootAnimation) trsMatrix = trsMatrix * rootAnimation.GetTRSMatrix(frame);
            else trsMatrix = trsMatrix * Matrix4x4.TRS(RootController.transform.localPosition, RootController.transform.localRotation, RootController.transform.localScale);

            for (int i = 0; i < PathToRoot.Count - 1; i++)
            {
                trsMatrix = trsMatrix * AnimToRoot[i].GetTRSMatrix(frame);
            }
            return trsMatrix;
        }

        //public void CheckAnimations()
        //{
        //    AnimToRoot.Clear();
        //    PathToRoot.ForEach(x =>
        //    {
        //        AnimationSet anim = GlobalState.Animation.GetOrCreateObjectAnimation(x.gameObject);
        //        AnimToRoot.Add(anim);
        //        if (x.TryGetComponent(out JointController joint)) joint.Animation = anim;
        //    });
        //    Animation = GlobalState.Animation.GetObjectAnimation(gameObject);
        //}

        public AnimationSet GetParentAnim()
        {
            return AnimToRoot[AnimToRoot.Count - 1];
        }

        public bool TryGetParentJoint(out JointController parent)
        {
            if (PathToRoot.Count == 0)
            {
                parent = null;
                return false;
            }
            else
            {
                parent = PathToRoot[PathToRoot.Count - 1].GetComponent<JointController>();
                return true;
            }
        }

        public JointController GetIKOrigin()
        {
            JointController controller = this;
            if (PathToRoot.Count > 2) controller = PathToRoot[PathToRoot.Count - 2].GetComponent<JointController>();
            return controller;
        }

    }
}