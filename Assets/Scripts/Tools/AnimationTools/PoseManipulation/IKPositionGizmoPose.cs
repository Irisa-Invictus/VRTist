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