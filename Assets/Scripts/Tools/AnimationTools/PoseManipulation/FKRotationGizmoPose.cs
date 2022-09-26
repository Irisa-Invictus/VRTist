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