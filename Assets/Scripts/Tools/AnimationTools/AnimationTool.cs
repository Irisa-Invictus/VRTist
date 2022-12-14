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
        [SerializeField] private NavigationOptions navigation;
        public Anim3DCurveManager CurveManager;
        public VRPicker Picker;
        public Transform pickerMouthpiece;

        public Transform controlPanel;

        public GameObject GizmoPrefab;
        public List<GoalGizmo> gizmos = new List<GoalGizmo>();
        public GoalGizmo draggedGizmo;

        private Transform FKModeButton;
        private Transform IKModeButton;

        private bool worldGrip;
        private float deadzone = 0.3f;

        public enum Vector3Axis { X, Y, Z, None }

        public enum PoseEditMode { FK, IK }
        private PoseEditMode poseMode;
        public PoseEditMode PoseMode
        {
            get { return poseMode; }
            set
            {
                GetPoseModeButton(poseMode).Checked = false;
                poseMode = value;
                GetPoseModeButton(poseMode).Checked = true;
            }
        }

        public GameObject interactingObject;

        public class DragObjectData
        {
            public GameObject target;
            public Matrix4x4 initialMouthpieceWorldToLocal, initialParentMatrixLocalToWorld, initialParentMatrixWorldToLocal;
            public Vector3 initialPosition, initialScale;
            public Quaternion initialRotation;
        }
        private DragObjectData draggedObject;

        private RigObjectController grabbedController;

        public class CurveData
        {
            public GameObject target;
            public GameObject curve;
            public bool hasSelection;
            public int StartSelection;
            public int EndSelection;
            public CurveManipulation manipulation;
        }

        private CurveData selectedCurve;
        private CurveData draggedCurve;
        private bool isSelectingCurve;
        private GameObject hoveredCurve;

        public bool inPickerTool;
        private bool isPickerActive;

        public bool IsAddingConstraint;
        public UIButton ConstraintButton;

        private bool showDirect = true;
        public bool ShowDirect
        {
            get { return showDirect; }
            set { showDirect = value; ShowDirectControllers(showDirect); }
        }

        private UIButton GetPoseModeButton(PoseEditMode mode)
        {
            switch (mode)
            {
                case PoseEditMode.FK: return FKModeButton.GetComponent<UIButton>();
                case PoseEditMode.IK: return IKModeButton.GetComponent<UIButton>();
                default: return null;
            }
        }
        public void SetFKMode()
        {
            PoseMode = PoseEditMode.FK;
        }

        public void SetIKMode()
        {
            PoseMode = PoseEditMode.IK;
        }

        public void ShowPicker(bool state)
        {
            if (!state) Picker.PickerTool.SelectEmpty();
            Picker.gameObject.SetActive(state);
            isPickerActive = state;
        }

        public void MovePicker()
        {
            SelectionHelper selector = FindObjectOfType<SelectionHelper>();
            if (selector != null) Picker.transform.position = selector.transform.position;
        }

        public void ResetPosition()
        {
            List<Vector3> startPosition = new List<Vector3>();
            List<Quaternion> startRotation = new List<Quaternion>();
            List<Vector3> startScale = new List<Vector3>();
            List<Vector3> endPosition = new List<Vector3>();
            List<Quaternion> endRotation = new List<Quaternion>();
            List<Vector3> endScale = new List<Vector3>();
            if (Selection.SelectedControllers.Count > 0)
            {
                Selection.SelectedControllers.ForEach(x =>
                {
                    startPosition.Add(x.transform.localPosition);
                    startRotation.Add(x.transform.localRotation);
                    startScale.Add(x.transform.localScale);
                    x.ResetPosition();
                    endPosition.Add(x.transform.localPosition);
                    endRotation.Add(x.transform.localRotation);
                    endScale.Add(x.transform.localScale);
                });
                new CommandMoveControllers(Selection.SelectedControllers, startPosition, startRotation, startScale, endPosition, endRotation, endScale).Submit();
            }
            else
            {
                List<RigObjectController> movedControllers = new List<RigObjectController>();
                foreach (GameObject select in Selection.SelectedObjects)
                {
                    if (select.TryGetComponent<RigController>(out RigController controller))
                    {
                        RigObjectController[] rigControllers = controller.GetComponentsInChildren<RigObjectController>();
                        //some transforms can have two+ RigObjectController
                        for (int i = 0; i < rigControllers.Length; i++)
                        {
                            movedControllers.Add(rigControllers[i]);
                            startPosition.Add(rigControllers[i].transform.localPosition);
                            startRotation.Add(rigControllers[i].transform.localRotation);
                            startScale.Add(rigControllers[i].transform.localScale);
                        }
                        for (int i = 0; i < rigControllers.Length; i++)
                        {
                            rigControllers[i].ResetPosition(applyToChild: false);
                            endPosition.Add(rigControllers[i].transform.localPosition);
                            endRotation.Add(rigControllers[i].transform.localRotation);
                            endScale.Add(rigControllers[i].transform.localScale);
                        }
                    }
                }
                new CommandMoveControllers(movedControllers, startPosition, startRotation, startScale, endPosition, endRotation, endScale).Submit();
            }
        }

        protected override void Awake()
        {
            base.Awake();

            FKModeButton = controlPanel.Find("FK");
            IKModeButton = controlPanel.Find("IK");

            PoseMode = PoseEditMode.FK;
        }



        protected override void OnEnable()
        {
            base.OnEnable();
            ShowDirectControllers(ShowDirect);
            GlobalState.Instance.onGripWorldEvent.AddListener(OnGripWorld);
            //Selection.OnControllerSelected.AddListener(OnControllerSelected);
            //Selection.OnControllerUnselected.AddListener(OnControllerUnselected);
            if (isPickerActive)
            {
                if (inPickerTool) inPickerTool = false;
                else Picker.gameObject.SetActive(true);
            }

            //Selection.SelectedControllers.ForEach(x => OnControllerSelected(x));
        }

        private void ShowDirectControllers(bool state)
        {
            foreach (GameObject select in Selection.SelectedObjects)
            {
                if (select.TryGetComponent(out RigController controller))
                {
                    DirectController[] directController = controller.GetComponentsInChildren<DirectController>();
                    for (int i = 0; i < directController.Length; i++)
                    {
                        directController[i].UseController(state);
                    }
                }
            }
            if (!state)
            {
                gizmos.ForEach(x => Destroy(x));
                gizmos.Clear();
            }
        }

        public void SwitchDirectController()
        {
            ShowDirect = !ShowDirect;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GlobalState.Instance.onGripWorldEvent.RemoveListener(OnGripWorld);
            Selection.OnControllerSelected.RemoveListener(OnControllerSelected);
            Selection.OnControllerUnselected.RemoveListener(OnControllerUnselected);

            IsAddingConstraint = false;
            ConstraintButton.Checked = false;

            if (worldGrip || ToolsManager.Instance.IsInWindowTool || inPickerTool) return;

            UnSelectControllers();

            //UnSelectControllers();
            foreach (GameObject select in Selection.SelectedObjects)
            {
                if (select == null) continue;
                if (select.TryGetComponent<RigController>(out RigController controller))
                {
                    DirectController[] directController = controller.GetComponentsInChildren<DirectController>();
                    for (int i = 0; i < directController.Length; i++)
                    {
                        directController[i].UseController(false);
                    }
                }
            }
            Picker.PickerTool.SelectEmpty();
            Picker.gameObject.SetActive(false);
        }

        public void OnGripWorld(bool state)
        {
            worldGrip = state;
            if (state && null != grabbedController) ReleaseController();
            //if (state && null != selectedCurve) ReleaseCurve();
            if (state && null != draggedObject) ReleaseObject();
        }

        protected override void DoUpdate()
        {
            if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                Vector2 AxisValue = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                if (AxisValue != Vector2.zero)
                {
                    float scaleFactor = 1f + GlobalState.Settings.scaleSpeed / 1000f;
                    float selectorRadius = mouthpiece.localScale.x;
                    if (AxisValue.y > deadzone) selectorRadius *= scaleFactor;
                    if (AxisValue.y < deadzone) selectorRadius /= scaleFactor;
                    selectorRadius = Mathf.Clamp(selectorRadius, 0.001f, 0.5f);
                    mouthpiece.localScale = Vector3.one * selectorRadius;
                    pickerMouthpiece.localScale = Vector3.one * selectorRadius;
                }
            }
        }

        internal void SelectEmpty()
        {
            UnSelectControllers();
        }

        public void SelectConstraint()
        {
            IsAddingConstraint = true;
            ConstraintButton.Checked = true;
        }
        public void AddConstraint(GameObject target)
        {
            IsAddingConstraint = false;
            ConstraintButton.Checked = false;
            if (Selection.SelectedControllers.Count > 0)
            {
                new CommandAddConstraint(ConstraintType.Parent, target, Selection.SelectedControllers[0].gameObject).Submit();
            }
        }

        #region Gizmo/Actuator
        internal void CreateGizmo(RigObjectController controller)
        {
            GameObject newGizmo = Instantiate(GizmoPrefab);
            GoalGizmo gizmoScript = newGizmo.GetComponent<GoalGizmo>();
            gizmos.Add(gizmoScript);
            gizmoScript.Initialize(controller);
        }

        internal void RemoveGizmo(RigObjectController controller)
        {
            GoalGizmo gizmo = gizmos.Find(x => x.Controller == controller);
            if (gizmo != null)
            {
                gizmos.Remove(gizmo);
                Destroy(gizmo.gameObject);
            }
        }

        internal void NextGizmo()
        {
            gizmos.ForEach(x => x.NextGizmo());
        }
        internal void HoverActuator(GameObject gameObject)
        {
            if (gameObject.transform.parent.TryGetComponent<GoalGizmo>(out GoalGizmo gizmo))
            {
                gizmo.StartHover(gameObject);
            }
        }

        internal void StopHoverActuator(GameObject gameObject)
        {
            if (gameObject.transform.parent.TryGetComponent<GoalGizmo>(out GoalGizmo gizmo))
            {
                gizmo.EndHover(gameObject);
            }
        }

        internal void GrabActuator(GameObject gameObject, Transform mouthpiece)
        {
            if (gameObject.transform.parent.TryGetComponent(out GoalGizmo gizmo))
            {
                gizmo.GrabGizmo(mouthpiece, gameObject.transform);
                draggedGizmo = gizmo;
            }
        }

        internal void DragActuator(Transform transform)
        {
            if (draggedGizmo != null)
            {
                draggedGizmo.DragGizmo(mouthpiece);
            }
        }
        internal void ReleaseActuator()
        {
            if (draggedGizmo != null)
            {
                draggedGizmo.ReleaseGizmo();
                draggedGizmo = null;
            }
        }

        internal void SelectActuator(GameObject gameObject)
        {
        }
        #endregion

        #region curve
        internal void HoverCurve(GameObject gameObject, Transform mouthpiece)
        {
            if (interactingObject != null && interactingObject != gameObject) return;
            if (selectedCurve != null && selectedCurve.curve == gameObject)
            {
                CurveManager.DrawLineZone(selectedCurve.StartSelection, selectedCurve.EndSelection, Color.red);
            }
            else CurveManager.HoverCurve(gameObject, mouthpiece);
            hoveredCurve = gameObject;
        }

        internal void UpdateHoverCurve(GameObject gameObject, Transform transform)
        {
            CurveManager.UpdateHoverCurve(gameObject, transform);
        }

        internal void StopHoverCurve(GameObject gameObject)
        {
            if (interactingObject != null && interactingObject != gameObject) return;
            hoveredCurve = null;
            if (isSelectingCurve || (selectedCurve != null && gameObject == selectedCurve.curve && selectedCurve.hasSelection)) CurveManager.SelectionStopHover(selectedCurve.curve, selectedCurve.StartSelection, selectedCurve.EndSelection);
            else CurveManager.StopHover(gameObject);
        }
        internal void GrabCurve(GameObject gameObject, Transform transform)
        {
            GameObject target = CurveManager.GetObjectFromCurve(gameObject);
            int frame = CurveManager.GetFrameFromPoint(target, transform.position);
            if (selectedCurve == null || gameObject != selectedCurve.curve)
            {
                draggedCurve = new CurveData()
                {
                    target = target,
                    curve = gameObject,
                    hasSelection = false,
                    manipulation = new CurveManipulation(target, frame, mouthpiece, PoseMode)
                };
            }
            else
            {
                draggedCurve = selectedCurve;
                if (selectedCurve.hasSelection && frame > selectedCurve.StartSelection && frame < selectedCurve.EndSelection)
                {
                    draggedCurve.manipulation = new CurveManipulation(target, frame, selectedCurve.StartSelection, selectedCurve.EndSelection, mouthpiece, PoseMode);

                }
                else
                {
                    draggedCurve.manipulation = new CurveManipulation(target, frame, mouthpiece, PoseMode);
                }
            }
            interactingObject = gameObject;
        }
        internal void DragCurve(Transform mouthpiece)
        {
            if (draggedCurve == null || draggedCurve.manipulation == null) return;
            draggedCurve.manipulation.DragCurve(mouthpiece);
        }
        internal void ReleaseCurve()
        {
            if (draggedCurve == null || draggedCurve.manipulation == null) return;
            draggedCurve.manipulation.ReleaseCurve();
            interactingObject = null;
        }
        internal void SelectCurve(GameObject gameObject, Transform transform)
        {
            if (selectedCurve != null) UnSelectCurve();
            GameObject target = CurveManager.GetObjectFromCurve(gameObject);
            int selectedFrame = CurveManager.GetFrameFromPoint(target, transform.position);
            selectedCurve = new CurveData()
            {
                target = target,
                curve = gameObject,
                hasSelection = false,
                StartSelection = selectedFrame,
                EndSelection = selectedFrame
            };
            CurveManager.StartSelection(gameObject);
            isSelectingCurve = true;
            interactingObject = gameObject;
        }
        internal void SelectingCurve(Transform transform)
        {
            if (selectedCurve == null) return;
            if (hoveredCurve == null || hoveredCurve != selectedCurve.curve) return;
            int currentFrame = CurveManager.GetFrameFromPoint(selectedCurve.target, transform.position);
            selectedCurve.StartSelection = Mathf.Min(selectedCurve.StartSelection, currentFrame);
            selectedCurve.EndSelection = Mathf.Max(selectedCurve.EndSelection, currentFrame);
            CurveManager.DrawLineZone(selectedCurve.StartSelection, selectedCurve.EndSelection, Color.red);
        }

        internal void EndSelectionCurve()
        {
            isSelectingCurve = false;
            //TODO: freeze selection if not on curve anymore
            if (selectedCurve == null) return;
            if (selectedCurve.EndSelection - selectedCurve.StartSelection > 3) selectedCurve.hasSelection = true;
            if (!selectedCurve.hasSelection) UnSelectCurve();
            interactingObject = null;
        }

        internal void UnSelectCurve()
        {
            CurveManager.RemoveSelection(selectedCurve.curve);
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
        internal void HoverController(GameObject gameObject)
        {
            if (interactingObject != null) return;
            RigObjectController hoveredController = gameObject.GetComponent<RigObjectController>();
            hoveredController.StartHover();
        }
        internal void StopHoverController(GameObject gameObject)
        {
            if (interactingObject != null) return;
            RigObjectController controller = gameObject.GetComponent<RigObjectController>();
            controller.EndHover();
        }
        internal void GrabController(GameObject controller, Transform mouthpiece)
        {
            grabbedController = controller.GetComponent<RigObjectController>();
            grabbedController.OnGrab(mouthpiece, PoseMode == PoseEditMode.FK);
            interactingObject = controller;
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
            interactingObject = null;
        }
        internal void SelectController(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out DirectController directController))
            {
                if (Selection.SelectedControllers.Contains(directController))
                {
                    Selection.SelectedControllers.Remove(directController);
                    Selection.OnControllerUnselected.Invoke(directController);
                    OnControllerUnselected(directController);
                }
                else
                {
                    Selection.SelectedControllers.Add(directController);
                    Selection.OnControllerUnselected.Invoke(directController);
                    OnControllerSelected(directController);
                }
            }
        }

        private void UnSelectControllers()
        {
            selectedCurve = null;
            foreach (RigObjectController controller in Selection.SelectedControllers)
            {
                if (controller is RigConstraintController)
                {
                    if (controller.TryGetComponent(out MeshRenderer renderer)) renderer.enabled = false;
                    if (controller.TryGetComponent(out MeshCollider collider)) collider.enabled = false;
                    if (controller.pairedController != null) controller.pairedController.OnDeselect();
                }
                OnControllerUnselected(controller);
                //Selection.OnControllerUnselected.Invoke(controller);
                //controller.GetTargets().ForEach(x => CurveManager.UnSelectJoint(x));
                //controller.OnDeselect();
                //RemoveGizmo(controller);
            }
            Selection.SelectedControllers.Clear();
            Selection.OnControllerUnselected.Invoke(null);
            //Selection.OnControllerSelection.Invoke();
        }

        private void OnControllerSelected(RigObjectController controller)
        {
            controller.OnSelect();
            CreateGizmo(controller);
        }

        private void OnControllerUnselected(RigObjectController controller)
        {
            controller.OnDeselect();
            RemoveGizmo(controller);
        }

        #endregion
    }

}