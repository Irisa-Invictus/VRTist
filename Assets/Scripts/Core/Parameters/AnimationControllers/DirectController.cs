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

        public override void OnGrab(Transform mouthpiece, bool isFK)
        {
            if (isFK)
            {
                poseManip = new FKPoseManipulation(this, mouthpiece);
            }
            else
            {
                poseManip = new IKPoseManipulation(this, mouthpiece, GetIKOrigin().transform);
            }
        }

        public override void OnDrag(Transform mouthpiece)
        {
            poseManip.SetDestination(mouthpiece);
            poseManip.TrySolver();
        }

        public override void OnRelease()
        {
            CommandGroup group = new CommandGroup("Add Keyframe");
            poseManip.GetCommand().Submit();
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
            group.Submit();
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