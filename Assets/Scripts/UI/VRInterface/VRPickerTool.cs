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
using UnityEngine.Events;

namespace VRtist
{

    public class VRPickerTool : ToolBase
    {

        public VRPicker Picker;

        private Matrix4x4 initialObjectTRS;
        private Matrix4x4 initialMouthWorldToLocal;
        private float scaleDeadzone = 0.3f;
        private float ScaleValue;
        private Vector3 initialScale;

        private List<RigObjectController> selectedControllers = new List<RigObjectController>();
        private PoseManipulation poseManip;

        private RigObjectController draggedController;

        protected override void DoUpdate()
        {
        }


        public void SelectController(GameObject target)
        {
            RigObjectController controller = target.GetComponent<RigObjectController>();

            if (selectedControllers.Contains(controller))
            {
                UnselectController(controller);
                selectedControllers.Remove(controller);
                return;
            }
            //Gizmo.gameObject.SetActive(true);
            //Gizmo.Init(controller, true);
            //Gizmo.ChangeGizmo(GoalGizmo.GizmoTool.Position);
            controller.OnSelect();
            selectedControllers.Add(controller);

            GameObject cloneTarget = Picker.CloneToTarget[controller.gameObject];
            if (cloneTarget.TryGetComponent<RigObjectController>(out RigObjectController ctrl))
            {
                if (ctrl.TryGetComponent(out MeshRenderer renderer)) renderer.enabled = true;
                if (ctrl.TryGetComponent(out MeshCollider collider)) collider.enabled = true;
            }
            Picker.PickerGizmo.gameObject.SetActive(true);
            Picker.PickerGizmo.FixedInitialize(controller);

            //controller.OnSelect(Gizmo);
            //SelectedGoal.Invoke(controller);
        }

        public void UnselectController(RigObjectController controller)
        {
            //Gizmo.gameObject.SetActive(false);
            //UnselectedGoal.Invoke(controller.TargetController);
            controller.OnDeselect();

            if (Picker.CloneToTarget.TryGetValue(controller.gameObject, out GameObject cloneTarget))
            {
                if (cloneTarget.TryGetComponent(out RigObjectController ctrl))
                {
                    if (ctrl.TryGetComponent(out MeshRenderer renderer)) renderer.enabled = false;
                    if (ctrl.TryGetComponent(out MeshCollider collider)) collider.enabled = false;
                }
                if (Picker.PickerGizmo.isActiveAndEnabled && controller == Picker.PickerGizmo.Controller) Picker.PickerGizmo.gameObject.SetActive(false);
            }
        }

        public void SelectEmpty()
        {
            selectedControllers.ForEach(x => UnselectController(x));
            selectedControllers.Clear();
        }

        public void ResetControllers()
        {
            List<Vector3> startPositions = new List<Vector3>();
            List<Vector3> endPositions = new List<Vector3>();
            List<Quaternion> startRotations = new List<Quaternion>();
            List<Quaternion> endRotations = new List<Quaternion>();
            List<Vector3> startScales = new List<Vector3>();
            List<Vector3> endScales = new List<Vector3>();
            List<RigObjectController> originalControllers = new List<RigObjectController>();
            if (selectedControllers.Count > 0)
            {
                selectedControllers.ForEach(x =>
                {
                    RigObjectController original = x.pairedController;
                    originalControllers.Add(original);
                    startPositions.Add(original.transform.localPosition);
                    startRotations.Add(original.transform.localRotation);
                    startScales.Add(original.transform.localScale);
                    x.ResetPosition();
                    endPositions.Add(original.transform.localPosition);
                    endRotations.Add(original.transform.localRotation);
                    endScales.Add(original.transform.localScale);
                });
            }
            else
            {
                Picker.controllers.ForEach(x =>
                {
                    originalControllers.Add(x.pairedController);
                    startPositions.Add(x.transform.localPosition);
                    startRotations.Add(x.transform.localRotation);
                    startScales.Add(x.transform.localScale);
                    x.ResetPosition();
                    endPositions.Add(x.transform.localPosition);
                    endRotations.Add(x.transform.localRotation);
                    endScales.Add(x.transform.localScale);
                });
            }
            new CommandMoveControllers(originalControllers, startPositions, startRotations, startScales, endPositions, endRotations, endScales).Submit();
        }

        #region Actuator

        public void HoverActuator(GameObject actuator)
        {
            if (Picker.PickerGizmo.isActiveAndEnabled)
            {
                Picker.PickerGizmo.StartHover(actuator);
            }
        }
        public void HoverActuatorEnd(GameObject actuator)
        {
            if (Picker.PickerGizmo.isActiveAndEnabled)
            {
                Picker.PickerGizmo.EndHover(actuator);
            }
        }

        public void ActuatorGrab(GameObject actuator, Transform mouthpiece)
        {
            Picker.PickerGizmo.GrabGizmo(mouthpiece, actuator.transform);
            //Gizmo.GrabGizmo(actuator, mouthpiece, false);
            //draggedController = Gizmo.Controller;
            Picker.Lock = true;
        }
        public void ActuatorDrag(Transform mouthpiece)
        {
            Picker.PickerGizmo.DragGizmo(mouthpiece);
            //draggedController.OnDragGizmo(mouthpiece);
        }

        public void ActuatorRelease()
        {
            Picker.PickerGizmo.ReleaseGizmo();
            //draggedController.OnReleaseGizmo();
            //Gizmo.UnSelectAxis();
            //draggedController = null;
            Picker.Lock = false;
        }
        #endregion

        #region Base
        public void HoverBase()
        {
            Picker.PickerBase.GetComponent<MeshRenderer>().material.SetColor("_BASE_COLOR", Color.red);
        }
        public void HoverBaseEnd()
        {
            Picker.PickerBase.GetComponent<MeshRenderer>().material.SetColor("_BASE_COLOR", Color.black);
        }

        public void BaseGrab(Transform mouthpiece)
        {
            initialObjectTRS = Picker.transform.localToWorldMatrix;
            initialMouthWorldToLocal = mouthpiece.worldToLocalMatrix;
            Picker.Lock = true;
            ScaleValue = 1f;
            initialScale = Picker.transform.localScale;
        }

        public void BaseDrag(Transform mouthpiece, Vector2 axis)
        {
            Matrix4x4 transform = mouthpiece.localToWorldMatrix * initialMouthWorldToLocal;
            transform = transform * initialObjectTRS;
            Maths.DecomposeMatrix(transform, out Vector3 position, out Quaternion rotation, out Vector3 scale);


            float scaleFactor = 1f + GlobalState.Settings.scaleSpeed / 1000f;
            float scaleIndice = 1f;

            if (axis.y > scaleDeadzone) scaleIndice *= scaleFactor;
            if (axis.y < -scaleDeadzone) scaleIndice /= scaleFactor;
            ScaleValue = Mathf.Clamp(ScaleValue * scaleIndice, 0.001f, 100f);
            Picker.transform.localScale = initialScale * ScaleValue;
            Picker.transform.position = position;
            Picker.transform.rotation = rotation;
        }

        public void BaseRelease()
        {
            Picker.Lock = false;
        }
        #endregion
        #region Controller
        public void HoverController(GameObject target)
        {
            RigObjectController controller = target.GetComponent<RigObjectController>();
            controller.StartHover();
        }
        public void StopHoverController(GameObject target)
        {
            RigObjectController controller = target.GetComponent<RigObjectController>();
            controller.EndHover();
        }

        public void ControllerGrab(GameObject target, Transform mouthpiece)
        {
            RigObjectController controller = target.GetComponent<RigObjectController>();
            Picker.Lock = true;
            draggedController = controller;
            controller.OnGrab(mouthpiece, true);
        }

        public void ControllerDrag(Transform mouthpiece, Vector2 axis)
        {
            if (null == draggedController) return;
            draggedController.OnDrag(mouthpiece);
        }

        public void ControllerRelease()
        {
            if (draggedController == null) return;
            draggedController.OnRelease();
            Picker.Lock = false;
        }
        #endregion
        public void GizmoGrab(Transform mouthpiece)
        {
            initialObjectTRS = Picker.PickerGizmo.transform.localToWorldMatrix;
            initialMouthWorldToLocal = mouthpiece.worldToLocalMatrix;
            Picker.Lock = true;
            ScaleValue = 1f;
            initialScale = Picker.PickerGizmo.transform.localScale;
        }

        public void GizmoDrag(Transform mouthpiece, Vector2 axis)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthWorldToLocal;
            transformation = transformation * initialObjectTRS;
            Maths.DecomposeMatrix(transformation, out Vector3 position, out Quaternion rotation, out Vector3 scale);


            float scaleFactor = 1f + GlobalState.Settings.scaleSpeed / 1000f;
            float scaleIndice = 1f;

            if (axis.y > scaleDeadzone) scaleIndice *= scaleFactor;
            if (axis.y < -scaleDeadzone) scaleIndice /= scaleFactor;
            ScaleValue = Mathf.Clamp(ScaleValue * scaleIndice, 0.001f, 100f);
            Picker.PickerGizmo.transform.localScale = initialScale * ScaleValue;
            Picker.PickerGizmo.transform.position = position;
            //Gizmo.transform.rotation = rotation;
        }
        public void GizmoRelease()
        {
            Picker.Lock = false;
        }



    }

}