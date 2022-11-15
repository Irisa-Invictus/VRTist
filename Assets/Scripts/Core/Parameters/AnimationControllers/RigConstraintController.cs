/* MIT License
 *
 * © Dada ! Animation
 * &
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
using Dada.URig;

namespace VRtist
{
    public class RigConstraintController : RigObjectController
    {
        public Range xTranslationRange;
        public Range yTranslationRange;
        public Range zTranslationRange;

        public Range xRotationRange;
        public Range yRotationRange;
        public Range zRotationRange;

        public Range xScaleRange;
        public Range yScaleRange;
        public Range zScaleRange;

        public Dada.URig.Descriptors.Constraint[] constraints;

        internal Matrix4x4 InitialParentMatrixWorldToLocal;
        internal Matrix4x4 InitialParentMatrix;
        internal Matrix4x4 initialMouthMatrix;
        internal Matrix4x4 initialTransformMatrix;
        internal Matrix4x4 InitialTRS;

        private Vector3 startPosition, endPosition;
        private Quaternion startRotation, endRotation;
        private Vector3 startScale, endScale;

        private CommandGroup cmdGroup;




        public override void StartHover()
        {
            if (isSelected) return;
            gameObject.layer = 22;
        }
        public override void EndHover()
        {
            if (isSelected) return;
            gameObject.layer = startLayer;
        }
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

        public void DirectGrab(Transform controller)
        {
            InitMatrix(controller);

            startPosition = transform.localPosition;
            startRotation = transform.localRotation;
            startScale = transform.localScale;
        }
        public void DirectDrag(Transform controller)
        {
            Matrix4x4 transformation = controller.localToWorldMatrix * initialMouthMatrix;
            Matrix4x4 transformed = InitialParentMatrixWorldToLocal *
                    transformation * InitialParentMatrix *
                    InitialTRS;
            Maths.DecomposeMatrix(transformed, out Vector3 controllerPosition, out Quaternion controllerRotation, out Vector3 controllerScale);
            transform.localPosition = controllerPosition;
            transform.localRotation = controllerRotation;
            transform.localScale = controllerScale;
        }
        public void DirectRelease()
        {
            endPosition = transform.localPosition;
            endRotation = transform.localRotation;
            endScale = transform.localScale;

            new CommandMoveObjects(new List<GameObject>() { gameObject }, new List<Vector3>() { startPosition }, new List<Quaternion>() { startRotation }, new List<Vector3>() { startScale }
            , new List<Vector3>() { endPosition }, new List<Quaternion>() { endRotation }, new List<Vector3>() { endScale });
        }

        public override void OnGrab(Transform mouthpiece, bool data)
        {
            InitMatrix(mouthpiece);

            startPosition = transform.localPosition;
            startRotation = transform.localRotation;
            startScale = transform.localScale;
            cmdGroup = new CommandGroup("Add Keyframe");
        }

        public override void OnDrag(Transform mouthpiece)
        {
            if (isTPose) return;
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            Matrix4x4 transformed = InitialParentMatrixWorldToLocal *
                    transformation * InitialParentMatrix *
                    InitialTRS;
            Maths.DecomposeMatrix(transformed, out Vector3 controllerPosition, out Quaternion controllerRotation, out Vector3 controllerScale);
            transform.localPosition = ClampPosition(controllerPosition);
            transform.localRotation = ClampRotation(controllerRotation);
            transform.localScale = ClampScale(controllerScale);


            UpdateController();
        }

        private Vector3 ClampPosition(Vector3 target)
        {
            float x = Mathf.Clamp(target.x, xTranslationRange.minimum, xTranslationRange.maximum);
            float y = Mathf.Clamp(target.y, yTranslationRange.minimum, yTranslationRange.maximum);
            float z = Mathf.Clamp(target.z, zTranslationRange.minimum, zTranslationRange.maximum);
            return new Vector3(x, y, z);
        }

        private Quaternion ClampRotation(Quaternion target)
        {
            Vector3 euler = target.eulerAngles;// Maths.ThreeAxisRotation(target);
            euler.x = Mathf.Clamp(euler.x, xRotationRange.minimum, xRotationRange.maximum);
            euler.y = Mathf.Clamp(euler.y, yRotationRange.minimum, yRotationRange.maximum);
            euler.z = Mathf.Clamp(euler.z, zRotationRange.minimum, zRotationRange.maximum);

            return Quaternion.Euler(euler);
        }

        private Vector3 ClampScale(Vector3 target)
        {
            float x = Mathf.Clamp(target.x, xScaleRange.minimum, xScaleRange.maximum);
            float y = Mathf.Clamp(target.y, yScaleRange.minimum, yScaleRange.maximum);
            float z = Mathf.Clamp(target.z, zScaleRange.minimum, zScaleRange.maximum);
            return new Vector3(x, y, z);
        }

        public override void UpdateController(bool applyToPair = true, bool applyToChild = true)
        {
            if (isTPose) return;
            ApplyConstraints();
            if (pairedController != null && applyToPair && !pairedController.isTPose)
            {
                pairedController.transform.localPosition = transform.localPosition;
                pairedController.transform.localRotation = transform.localRotation;
                pairedController.transform.localScale = transform.localScale;
                pairedController.UpdateController(false, applyToChild);
            }
            if (applyToChild)
            {
                RigConstraintController[] childs = GetComponentsInChildren<RigConstraintController>();
                foreach (RigConstraintController child in childs)
                {
                    child.UpdateController(applyToPair: applyToPair, applyToChild: false);
                }
            }
        }

        public void CopiePairedController()
        {
            if (pairedController == null) return;
            transform.localPosition = pairedController.transform.localPosition;
            transform.localRotation = pairedController.transform.localRotation;
            transform.localScale = pairedController.transform.localScale;
            UpdateController(applyToPair: false, applyToChild: false);
        }

        private void ApplyConstraints()
        {
            foreach (Dada.URig.Descriptors.Constraint constraint in constraints)
            {
                switch (constraint.type)
                {
                    case Dada.URig.Descriptors.Constraint.Type.AimUpAim:
                        {
                            AimUpAimConstraint(constraint);
                            break;
                        }

                    case Dada.URig.Descriptors.Constraint.Type.AimUpRotate:
                        {
                            AimUpRotateConstraint(constraint);
                            break;
                        }

                    case Dada.URig.Descriptors.Constraint.Type.CopyLocalAttributeToBlendShapeWeight:
                        {
                            CopyLocalAttributeToBlendShapeWeightConstraint(constraint);
                            break;
                        }

                    case Dada.URig.Descriptors.Constraint.Type.CopyLocalAttributeToTransformAttribute:
                        {
                            CopyLocalAttributeToTransformAttributeConstraint(constraint);
                            break;
                        }

                    case Dada.URig.Descriptors.Constraint.Type.CopyWorldAttributeToTransformAttribute:
                        {
                            CopyWorldAttributeToTransformAttributeConstraint(constraint);
                            break;
                        }

                    case Dada.URig.Descriptors.Constraint.Type.Orient:
                        {
                            OrientConstraint(constraint);
                            break;
                        }

                    case Dada.URig.Descriptors.Constraint.Type.Parent:
                        {
                            ParentConstraint(constraint);
                            break;
                        }

                    default:
                        break;
                }
            }
        }

        #region Constraints
        private void ParentConstraint(Dada.URig.Descriptors.Constraint constraint)
        {
            Dada.URig.Descriptors.ParentConstraint constraintVariant = constraint.parent;

            Matrix4x4 localMatrix = constraint.drivenObjectTransform.parent.worldToLocalMatrix * transform.localToWorldMatrix * constraintVariant.localTargetMatrix;
            Maths.DecomposeMatrix(localMatrix, out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale);

            Vector3 worldEulerAngles = localRotation.eulerAngles;

            constraint.drivenObjectTransform.localEulerAngles = Clamp(worldEulerAngles, constraintVariant.xTranslationTarget, constraintVariant.yTranslationTarget, constraintVariant.zTranslationTarget);
            constraint.drivenObjectTransform.localPosition = Clamp(localPosition, constraintVariant.xRotationTarget, constraintVariant.yRotationTarget, constraintVariant.zRotationTarget);
            constraint.drivenObjectTransform.localScale = Clamp(localScale, constraintVariant.xScaleTarget, constraintVariant.yScaleTarget, constraintVariant.zScaleTarget);
        }

        private void OrientConstraint(Dada.URig.Descriptors.Constraint constraint)
        {
            constraint.drivenObjectTransform.SetLocalToWorldMatrix(transform.localToWorldMatrix);
        }

        private void CopyWorldAttributeToTransformAttributeConstraint(Dada.URig.Descriptors.Constraint constraint)
        {
            Dada.URig.Descriptors.CopyWorldAttributeToTransformAttributeConstraint constraintVariant = constraint.copyWorldAttributeToTransformAttribute;

            float value = constraintVariant.attributeName switch
            {
                "TX" => transform.position.x,
                "TY" => transform.position.y,
                "TZ" => transform.position.z,
                "RX" => transform.eulerAngles.x,
                "RY" => transform.eulerAngles.y,
                "RZ" => transform.eulerAngles.z,
                "SX" => transform.lossyScale.x,
                "SY" => transform.lossyScale.y,
                "SZ" => transform.lossyScale.z,
                _ => throw new InvalidAttributeNameExeption(constraintVariant.attributeName),
            };

            float clampedValue = constraintVariant.target.range.Clamp(value);

            float targetValue = clampedValue;

            Vector3 position = constraint.drivenObjectTransform.position;
            Vector3 eulerAngles = constraint.drivenObjectTransform.eulerAngles;
            Vector3 scale = constraint.drivenObjectTransform.lossyScale;

            switch (constraintVariant.target.name)
            {
                case "TX":
                    position.x = targetValue;
                    break;

                case "TY":
                    position.y = targetValue;
                    break;

                case "TZ":
                    position.z = targetValue;
                    break;

                case "RX":
                    eulerAngles.x = targetValue;
                    break;

                case "RY":
                    eulerAngles.y = targetValue;
                    break;

                case "RZ":
                    eulerAngles.z = targetValue;
                    break;

                case "SX":
                    scale.x = targetValue;
                    break;

                case "SY":
                    scale.y = targetValue;
                    break;

                case "SZ":
                    scale.z = targetValue;
                    break;

                default:
                    throw new InvalidAttributeNameExeption(constraintVariant.target.name);
            }

            constraint.drivenObjectTransform.position = position;
            constraint.drivenObjectTransform.eulerAngles = eulerAngles;
            constraint.drivenObjectTransform.localScale = scale.ElementwiseDivide(constraint.drivenObjectTransform.parent.lossyScale);
        }

        private void CopyLocalAttributeToTransformAttributeConstraint(Dada.URig.Descriptors.Constraint constraint)
        {
            Dada.URig.Descriptors.CopyLocalAttributeToTransformAttributeConstraint constraintVariant = constraint.copyLocalAttributeToTransformAttribute;

            float value = constraintVariant.attributeName switch
            {
                "TX" => transform.localPosition.x,
                "TY" => transform.localPosition.y,
                "TZ" => transform.localPosition.z,
                "RX" => transform.localEulerAngles.x,
                "RY" => transform.localEulerAngles.y,
                "RZ" => transform.localEulerAngles.z,
                "SX" => transform.localScale.x,
                "SY" => transform.localScale.y,
                "SZ" => transform.localScale.z,
                _ => throw new InvalidAttributeNameExeption(constraintVariant.attributeName),
            };

            float clampedValue = constraintVariant.target.range.Clamp(value);

            float targetValue = clampedValue;

            Vector3 localPosition = constraint.drivenObjectTransform.localPosition;
            Vector3 localEulerAngles = constraint.drivenObjectTransform.localEulerAngles;
            Vector3 localScale = constraint.drivenObjectTransform.localScale;

            switch (constraintVariant.target.name)
            {
                case "TX":
                    localPosition.x = targetValue;
                    break;

                case "TY":
                    localPosition.y = targetValue;
                    break;

                case "TZ":
                    localPosition.z = targetValue;
                    break;

                case "RX":
                    localEulerAngles.x = targetValue;
                    break;

                case "RY":
                    localEulerAngles.y = targetValue;
                    break;

                case "RZ":
                    localEulerAngles.z = targetValue;
                    break;

                case "SX":
                    localScale.x = targetValue;
                    break;

                case "SY":
                    localScale.y = targetValue;
                    break;

                case "SZ":
                    localScale.z = targetValue;
                    break;

                default:
                    throw new InvalidAttributeNameExeption(constraintVariant.target.name);
            }

            constraint.drivenObjectTransform.localPosition = localPosition;
            constraint.drivenObjectTransform.localEulerAngles = localEulerAngles;
            constraint.drivenObjectTransform.localScale = localScale;
        }

        private void CopyLocalAttributeToBlendShapeWeightConstraint(Dada.URig.Descriptors.Constraint constraint)
        {
            Dada.URig.Descriptors.CopyLocalAttributeToBlendShapeWeightConstraint constraintVariant = constraint.copyLocalAttributeToBlendShapeWeight;

            float value = constraintVariant.attributeName switch
            {
                "TX" => transform.localPosition.x,
                "TY" => transform.localPosition.y,
                "TZ" => transform.localPosition.z,
                "RX" => transform.localEulerAngles.x,
                "RY" => transform.localEulerAngles.y,
                "RZ" => transform.localEulerAngles.z,
                "SX" => transform.localScale.x,
                "SY" => transform.localScale.y,
                "SZ" => transform.localScale.z,
                _ => throw new InvalidAttributeNameExeption(constraintVariant.attributeName),
            };

            float targetValue = constraintVariant.target.Transform(value);

            constraintVariant.target.skinnedMeshRenderer.SetBlendShapeWeight(constraintVariant.target.blendShapeIndex, targetValue);
        }

        private void AimUpRotateConstraint(Dada.URig.Descriptors.Constraint constraint)
        {
            var constraintVariant = constraint.aimUpRotate;

            // TODO
        }

        private void AimUpAimConstraint(Dada.URig.Descriptors.Constraint constraint)
        {
            Dada.URig.Descriptors.AimUpConstraint constraintVariant = constraint.aimUpAim;

            Vector3 forward = Vector3.Normalize(transform.position - constraint.drivenObjectTransform.position);
            Vector3 upward = Vector3.Normalize(constraintVariant.upTransform.position - constraint.drivenObjectTransform.position);
            Vector3 side = Vector3.Cross(forward, upward).normalized;
            Vector3 actualUpward = Vector3.Cross(side, forward).normalized;
            constraint.drivenObjectTransform.rotation = Quaternion.LookRotation(forward, actualUpward);
        }

        #endregion

        public override void OnRelease()
        {
            if (cmdGroup == null) cmdGroup = new CommandGroup("Add Keyframe");
            endPosition = transform.localPosition;
            endRotation = transform.localRotation;
            endScale = transform.localScale;

            new CommandMoveControllers(this, startPosition, startRotation, startScale, endPosition, endRotation, endScale).Submit();
            if (GlobalState.Animation.autoKeyEnabled)
            {
                new CommandAddKeyframes(gameObject, true).Submit();
            }

            cmdGroup.Submit();
            cmdGroup = null;
        }

        private Vector3 acAxis;
        private Vector3 initForward;
        private float previousAngle;
        private GoalGizmo.GizmoTool gizmoTool;
        private Transform gizmoTransform;

        public override void OnGrabGizmo(Transform mouthpiece, GoalGizmo gizmo, GoalGizmo.GizmoTool tool, AnimationTool.Vector3Axis axis, bool data)
        {
            cmdGroup = new CommandGroup("Add Keyframe");
            startPosition = transform.localPosition;
            startRotation = transform.localRotation;
            startScale = transform.localScale;
            gizmoTool = tool;

            switch (axis)
            {
                case AnimationTool.Vector3Axis.X:
                    acAxis = transform.right;
                    initForward = transform.up;
                    break;
                case AnimationTool.Vector3Axis.Y:
                    acAxis = transform.up;
                    initForward = transform.forward;
                    break;
                case AnimationTool.Vector3Axis.Z:
                    acAxis = transform.forward;
                    initForward = transform.right;
                    break;
            }
            previousAngle = Vector3.SignedAngle(initForward, mouthpiece.position - gizmo.transform.position, acAxis);
            if (gizmoTool == GoalGizmo.GizmoTool.Position)
            {
                InitMatrix(mouthpiece);
                acAxis = transform.parent.InverseTransformVector(acAxis);
            }
        }
        public override void OnDragGizmo(Transform mouthpiece)
        {
            if (isTPose) return;
            if (gizmoTool == GoalGizmo.GizmoTool.Rotation)
            {
                Vector3 projection = Vector3.ProjectOnPlane(mouthpiece.position - gizmoTransform.position, acAxis);
                float currentAngle = Vector3.SignedAngle(initForward, projection, acAxis);
                float angleOffset = Mathf.DeltaAngle(previousAngle, currentAngle);
                transform.Rotate(acAxis, angleOffset, Space.World);
                previousAngle = currentAngle;
            }
            else
            {
                Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
                Matrix4x4 transformed = InitialParentMatrixWorldToLocal *
                        transformation * InitialParentMatrix *
                        InitialTRS;
                Maths.DecomposeMatrix(transformed, out Vector3 targetPosition, out Quaternion rotation, out Vector3 scale);
                Vector3 movement = targetPosition - transform.localPosition;
                Vector3 movementProj = Vector3.Project(movement, acAxis);
                targetPosition = transform.localPosition + movementProj;
                transform.localPosition = targetPosition;
            }

            UpdateController();
        }
        public override void OnReleaseGizmo()
        {
            OnRelease();
        }

        public void MoveController()
        {
            //if (isDragged) return;

            //for(int i =0; i< constraints.Length; i++)
            //{

            //}

            //relations.ForEach(x =>
            //{
            //    switch (x.type)
            //    {
            //        case ConstraintType.parent:
            //            Matrix4x4 Matrix = transform.parent.worldToLocalMatrix * x.parentRelation.target.transform.localToWorldMatrix;// * x.parentRelation.offset; //  x.parentRelation.target.transform.localToWorldMatrix * x.parentRelation.offset * transform.parent.worldToLocalMatrix;
            //            Maths.DecomposeMatrix(Matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale);
            //            transform.localPosition = position;
            //            transform.localRotation = rotation;
            //            transform.localScale = scale;
            //            return;
            //    }
            //});
        }
        private void InitMatrix(Transform mouthpiece)
        {
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            InitialParentMatrix = transform.parent.localToWorldMatrix;
            InitialParentMatrixWorldToLocal = transform.parent.worldToLocalMatrix;
            InitialTRS = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
            initialTransformMatrix = transform.localToWorldMatrix;
        }

        public static Vector3 Clamp(Vector3 vector, Range xRange, Range yRange, Range zRange)
        {
            return new Vector3(xRange.Clamp(vector.x), yRange.Clamp(vector.y), zRange.Clamp(vector.z));
        }

        public static Vector3 Clamp(Vector3 vector, Dada.URig.Descriptors.Target xTarget, Dada.URig.Descriptors.Target yTarget, Dada.URig.Descriptors.Target zTarget)
        {
            return new Vector3(xTarget.Transform(vector.x), yTarget.Transform(vector.y), zTarget.Transform(vector.z));
        }

        public override List<JointController> GetTargets()
        {
            List<JointController> jointsTargets = new List<JointController>();
            for (int i = 0; i < constraints.Length; i++)
            {
                if (constraints[i].drivenObjectTransform.TryGetComponent(out JointController joint)) jointsTargets.Add(joint);
            }
            return jointsTargets;
        }
    }
}
