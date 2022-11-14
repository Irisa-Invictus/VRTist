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
    public class DirectController : RigObjectController
    {
        public JointController target;
        public Renderer MeshRenderer;
        public Collider goalCollider;
        public float stiffness;
        public bool ShowCurve;
        public Vector3 LowerAngleBound;
        public Vector3 UpperAngleBound;
        public bool FreePosition;
        public Vector3 LowerPositionBound;
        public Vector3 UpperPositionBound;
        private PoseManipulation poseManip;
        public List<RigConstraintController> rigControllers = new List<RigConstraintController>();

        private CommandGroup cmdGroup;

        public override void OnSelect()
        {
            isSelected = true;
            gameObject.layer = 20;
        }

        public override void OnDeselect()
        {
            isSelected = false;
            gameObject.layer = startLayer;
        }

        public override void UpdateController(bool applyToPair = true, bool applyToChild = true)
        {
        }

        public override void OnGrab(Transform mouthpiece, bool isFK)
        {
            cmdGroup = new CommandGroup("Add Keyframe");
            if (isFK)
            {
                poseManip = new FKPoseManipulation(this, mouthpiece);
            }
            else
            {
                poseManip = new IKPoseManipulation(this, mouthpiece, GetIKOrigin().transform);
            }
            rigControllers.ForEach(x => x.DirectGrab(transform));
        }

        public override void OnDrag(Transform mouthpiece)
        {
            poseManip.SetDestination(mouthpiece);
            poseManip.TrySolver();
            rigControllers.ForEach(x => x.DirectDrag(transform));
        }

        public override void OnRelease()
        {
            if (cmdGroup == null) cmdGroup = new CommandGroup("Add Keyframe");
            poseManip.GetCommand().Submit();
            rigControllers.ForEach(x => x.DirectRelease());
            if (GlobalState.Animation.autoKeyEnabled)
            {
                RigController rigController = null;
                foreach (GameObject item in poseManip.movedObjects)
                {
                    if (item.TryGetComponent(out JointController itemController) && itemController.RootController != rigController)
                    {
                        rigController = itemController.RootController;
                        new CommandAddKeyframes(rigController.gameObject, true).Submit();
                    }
                }
            }
            cmdGroup.Submit();
            cmdGroup = null;
            poseManip = null;
        }

        public void UseController(bool state)
        {
            ShowRenderer(state);
            UseCollider(state);
        }
        private void ShowRenderer(bool state)
        {
            if (null != MeshRenderer) MeshRenderer.enabled = state;
        }
        private void UseCollider(bool state)
        {
            if (null != goalCollider) goalCollider.enabled = state;
        }

        public DirectController GetIKOrigin()
        {
            DirectController controller = this;
            if (target.PathToRoot.Count > 2) controller = target.PathToRoot[target.PathToRoot.Count - 2].GetComponent<DirectController>();
            return controller;
        }

        #region Gizmo
        public override void OnGrabGizmo(Transform mouthpiece, GoalGizmo gizmo, GoalGizmo.GizmoTool tool, AnimationTool.Vector3Axis axis, bool isFK)
        {
            cmdGroup = new CommandGroup("Add Keyframe");
            if (isFK)
            {
                if (tool == GoalGizmo.GizmoTool.Position)
                {
                    poseManip = new FKPositionGizmoPose(this, mouthpiece, axis);
                }
                if (tool == GoalGizmo.GizmoTool.Rotation)
                {
                    poseManip = new FKRotationGizmoPose(this, mouthpiece, gizmo.transform, axis);
                }
            }
            else
            {
                if (tool == GoalGizmo.GizmoTool.Position)
                {
                    poseManip = new IKPositionGizmoPose(this, mouthpiece, axis, GetIKOrigin().transform);
                }
                if (tool == GoalGizmo.GizmoTool.Rotation)
                {
                    poseManip = new FKRotationGizmoPose(this, mouthpiece, gizmo.transform, axis);
                }
            }
        }
        public override void OnDragGizmo(Transform mouthpiece)
        {
            OnDrag(mouthpiece);
        }

        public override void OnReleaseGizmo()
        {
            OnRelease();
        }

        #endregion

        public override void StartHover()
        {
            if (!isHovered)
            {
                isHovered = true;
                if (!isSelected)
                {
                    gameObject.layer = 22;
                }
            }
        }

        public override void EndHover()
        {
            if (isHovered)
            {
                isHovered = false;
                if (!isSelected)
                {
                    gameObject.layer = startLayer;
                }
            }
        }

        public override List<JointController> GetTargets()
        {
            return new List<JointController>() { target };
        }
    }

}