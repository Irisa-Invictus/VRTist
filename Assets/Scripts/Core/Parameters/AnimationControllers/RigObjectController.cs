using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public abstract class RigObjectController : MonoBehaviour
    {
        internal MeshRenderer meshRenderer;
        internal bool isHovered;
        internal bool isSelected;
        internal Color startColor;
        internal int startLayer;

        public void Start()
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            startLayer = gameObject.layer;
        }


        public abstract void OnSelect();
        public abstract void OnDeselect();
        public abstract void OnGrab(Transform mouthpiece, bool data);
        public abstract void OnDrag(Transform mouthpiece);
        public abstract void OnRelease();

        //public abstract void OnGrabGizmo(Transform mouthpiece, GoalGizmo gizmo, GoalGizmo.GizmoTool tool, AnimationTool.Vector3Axis axis, bool data);

        //public abstract void OnDragGizmo(Transform mouthpiece);
        //public abstract void OnReleaseGizmo();

        public abstract void StartHover();
        public abstract void EndHover();

        public abstract List<JointController> GetTargets();
    }
}