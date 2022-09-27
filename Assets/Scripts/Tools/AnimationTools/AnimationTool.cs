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
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{

    public class AnimationTool : ToolBase
    {

        public Anim3DCurveManager CurveManager;

        public enum Vector3Axis { X, Y, Z, None }
        private Vector3Axis curveDisplayAxe = Vector3Axis.None;

        public enum PoseEditMode { FK, IK }
        private PoseEditMode poseMode;
        public PoseEditMode PoseMode { get; set; }

        public class DragObjectData
        {
            public GameObject target;
            public Matrix4x4 initialMouthpieceWorldToLocal, initialParentMatrixLocalToWorld, initialParentMatrixWorldToLocal;
            public Vector3 initialPosition, initialScale;
            public Quaternion initialRotation;
        }
        private DragObjectData draggedObject;

        private RigObjectController grabbedController;

        public class SelectedCurveData
        {
            public GameObject target;
            public bool hasSelection;
            public int StartSelection;
            public int EndSelection;
            public CurveManipulation manipulation;
        }

        private SelectedCurveData selectedCurve;

        protected override void DoUpdate()
        {
            throw new NotImplementedException();
        }

        internal void SelectEmpty()
        {

        }

        #region Gizmo/Actuator
        internal void NextGizmo()
        {
        }
        internal void HoverActuator(GameObject gameObject)
        {
        }

        internal void StopHoverActuator(GameObject gameObject)
        {
        }
        internal void GrabActuator(GameObject gameObject, Transform transform)
        {
        }
        internal void DragActuator(Transform transform)
        {
        }
        internal void ReleaseActuator()
        {
        }
        internal void SelectActuator(GameObject gameObject)
        {
        }
        #endregion

        #region curve
        internal void HoverCurve(GameObject gameObject, Transform mouthpiece)
        {
            CurveManager.HoverCurve(gameObject, mouthpiece);
        }

        internal void UpdateHoverCurve(GameObject gameObject, Transform transform)
        {
            CurveManager.UpdateHoverCurve(gameObject, transform);
        }

        internal void StopHoverCurve(GameObject gameObject)
        {
            CurveManager.StopHover(gameObject);
        }

        internal void GrabCurve(GameObject gameObject, Transform transform)
        {
            GameObject target = CurveManager.GetObjectFromCurve(gameObject);
            int frame = CurveManager.GetFrameFromPoint(target, transform.position);
            if (selectedCurve == null)
            {
                selectedCurve = new SelectedCurveData()
                {
                    target = target,
                    hasSelection = false,
                    manipulation = new CurveManipulation(target, frame, mouthpiece, PoseMode)
                };
            }
            else
            {
                if (selectedCurve.hasSelection)
                {
                    selectedCurve.manipulation = new CurveManipulation(target, frame, selectedCurve.StartSelection, selectedCurve.EndSelection, mouthpiece, PoseMode);

                }
                else
                {
                    selectedCurve.manipulation = new CurveManipulation(target, frame, mouthpiece, PoseMode);
                }
            }
        }
        internal void DragCurve(Transform mouthpiece)
        {
            if (selectedCurve == null || selectedCurve.manipulation == null) return;
            selectedCurve.manipulation.DragCurve(mouthpiece);
        }
        internal void ReleaseCurve()
        {
            if (selectedCurve == null || selectedCurve.manipulation == null) return;
            selectedCurve.manipulation.ReleaseCurve();
        }
        internal void SelectCurve(GameObject gameObject, Transform transform)
        {
            if (selectedCurve != null) UnSelectCurve();
            GameObject target = CurveManager.GetObjectFromCurve(gameObject);
            int selectedFrame = CurveManager.GetFrameFromPoint(target, transform.position);
            selectedCurve = new SelectedCurveData()
            {
                target = target,
                hasSelection = false,
                StartSelection = selectedFrame,
                EndSelection = selectedFrame
            };
        }
        internal void SelectingCurve(Transform transform)
        {
            if (selectedCurve == null) return;
            int currentFrame = CurveManager.GetFrameFromPoint(selectedCurve.target, transform.position);
            selectedCurve.StartSelection = Mathf.Min(selectedCurve.StartSelection, currentFrame);
            selectedCurve.EndSelection = Mathf.Max(selectedCurve.EndSelection, currentFrame);
        }

        internal void EndSelectionCurve()
        {
            if (selectedCurve == null) return;
            if (selectedCurve.EndSelection - selectedCurve.StartSelection > 3) selectedCurve.hasSelection = true;
        }

        internal void UnSelectCurve()
        {

        }
        #endregion

        #region object
        internal void HoverObject(GameObject gameObject)
        {
            Selection.HoveredObject = gameObject;
        }

        internal void StopHoverObject(GameObject gameObject)
        {
            Selection.HoveredObject = null;
        }

        internal void GrabObject(GameObject gameObject, Transform mouthpiece)
        {
            draggedObject = new DragObjectData()
            {
                initialMouthpieceWorldToLocal = mouthpiece.worldToLocalMatrix,
                initialParentMatrixLocalToWorld = gameObject.transform.parent.localToWorldMatrix,
                initialParentMatrixWorldToLocal = gameObject.transform.parent.worldToLocalMatrix,
                initialPosition = gameObject.transform.localPosition,
                initialRotation = gameObject.transform.localRotation,
                initialScale = gameObject.transform.localScale,
                target = gameObject
            };
        }
        internal void DragObject(Transform transform)
        {
            if (draggedObject == null) return;
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * draggedObject.initialMouthpieceWorldToLocal;
            Matrix4x4 transformed = draggedObject.initialParentMatrixWorldToLocal * transformation * draggedObject.initialParentMatrixLocalToWorld * Matrix4x4.TRS(draggedObject.initialPosition, draggedObject.initialRotation, draggedObject.initialScale);
            Maths.DecomposeMatrix(transformed, out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale);
            draggedObject.target.transform.localPosition = localPosition;
            draggedObject.target.transform.localRotation = localRotation;
            draggedObject.target.transform.localScale = localScale;
        }
        internal void ReleaseObject()
        {
            if (draggedObject == null) return;
            List<Vector3> lastPosition = new List<Vector3>() { draggedObject.target.transform.localPosition };
            List<Quaternion> lastRotation = new List<Quaternion>() { draggedObject.target.transform.localRotation };
            List<Vector3> lastScale = new List<Vector3>() { draggedObject.target.transform.localScale };

            if (GlobalState.Animation.autoKeyEnabled) new CommandAddKeyframes(draggedObject.target).Submit();

            new CommandMoveObjects(new List<GameObject>() { draggedObject.target }, new List<Vector3>() { draggedObject.initialPosition }, new List<Quaternion>() { draggedObject.initialRotation }, new List<Vector3>() { draggedObject.initialScale },
                lastPosition, lastRotation, lastScale).Submit();
            draggedObject = null;
        }



        internal void SelectObject(GameObject gameObject)
        {
            ///TODO: Allow multiple object dragging?
        }

        #endregion

        #region controller
        internal void GrabController(RigObjectController controller, Transform mouthpiece)
        {
            grabbedController = controller;
            grabbedController.OnGrab(mouthpiece, PoseMode == PoseEditMode.FK);
        }
        internal void DragController(Transform mouthpiece)
        {
            if (null == grabbedController) return;
            grabbedController.OnDrag(mouthpiece);
        }
        internal void ReleaseController()
        {
            if (null == grabbedController) return;
            grabbedController.OnRelease();
            grabbedController = null;
        }
        internal void SelectController(RigObjectController hoveredController)
        {
            //TODO: show controller gizmo
        }

        #endregion
    }

}