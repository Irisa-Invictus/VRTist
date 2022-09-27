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
using UnityEngine.XR;

namespace VRtist
{
    public class AnimationTrigger : MonoBehaviour
    {
        [SerializeField] private AnimationTool animationTool;
        public enum HoverType { none, Acutator, Controller, Curve, Object };
        public List<HoverType> hoveredTypes;

        private List<GameObject> hoveredTargets;
        private RigObjectController hoveredController;

        private bool gripPressed;
        private bool triggerPressed;

        #region hovering
        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Controller")
            {
                if (hoveredTargets.Count > 0) StopHovering();
                StartHovering(other.gameObject, HoverType.Controller);
            }
            if (other.gameObject.tag == "Curve")
            {
                if (hoveredTargets.Count > 0) StopHovering();
                StartHovering(other.gameObject, HoverType.Curve);
            }
            if (other.gameObject.tag == "Actuator")
            {
                if (hoveredTargets.Count > 0) StopHovering();
                StartHovering(other.gameObject, HoverType.Acutator);
            }
            if (Selection.SelectedObjects.Contains(other.gameObject) && !other.TryGetComponent(out RigController skin))
            {
                if (hoveredTargets.Count > 0) StopHovering();
                StartHovering(other.gameObject, HoverType.Object);
            }
        }
        public void OnTriggerExit(Collider other)
        {
            if (other.gameObject.tag == "Controller")
            {
                RemoveFromHovered(other.gameObject);
            }
            if (other.gameObject.tag == "Curve")
            {
                RemoveFromHovered(other.gameObject);
            }
            if (other.gameObject.tag == "Actuator")
            {
                RemoveFromHovered(other.gameObject);
            }
            if (Selection.SelectedObjects.Contains(other.gameObject) && !other.TryGetComponent(out RigController skin))
            {
                RemoveFromHovered(other.gameObject);
            }
        }
        private void StartHovering(GameObject target, HoverType type)
        {
            if (hoveredTargets.Count > 0) StopHovering();
            hoveredTargets.Add(target);
            hoveredTypes.Add(type);
            switch (type)
            {
                case HoverType.Controller:
                    hoveredController = target.GetComponent<RigObjectController>();
                    hoveredController.StartHover();
                    break;
                case HoverType.Acutator:
                    animationTool.HoverGizmo(target);
                    break;
                case HoverType.Curve:
                    animationTool.HoverLine(target, transform.position);
                    break;
                case HoverType.Object:
                    animationTool.HoverObject(target);
                    break;
            }
        }
        private void StopHovering()
        {
            switch (hoveredTypes[0])
            {
                case HoverType.Controller:
                    hoveredController.EndHover();
                    hoveredController = null;
                    break;
                case HoverType.Acutator:
                    animationTool.StopHoverGizmo(hoveredTargets[0]);
                    break;
                case HoverType.Curve:
                    animationTool.StopHoverLine(hoveredTargets[0]);
                    break;
                case HoverType.Object:
                    animationTool.StopHoverObject(hoveredTargets[0]);
                    break;
            }
        }
        private void RemoveFromHovered(GameObject target)
        {
            int index = hoveredTargets.IndexOf(target);
            if (index == -1) return;
            if (index == 0) StopHovering();

            hoveredTargets.RemoveAt(index);
            hoveredTypes.RemoveAt(index);

            if (index == 0) ReplaceHover();
        }
        private void ReplaceHover()
        {
            if (hoveredTargets.Count > 0)
            {
                switch (hoveredTypes[0])
                {
                    case HoverType.Controller:
                        hoveredController = hoveredTargets[0].GetComponent<RigObjectController>();
                        hoveredController.StartHover();
                        break;
                    case HoverType.Acutator:
                        animationTool.HoverGizmo(hoveredTargets[0]);
                        break;
                    case HoverType.Curve:
                        animationTool.HoverLine(hoveredTargets[0], transform.position);
                        break;
                    case HoverType.Object:
                        animationTool.HoverObject(hoveredTargets[0]);
                        break;
                }
            }
        }
        #endregion
        public void Update()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip, OnGripPressed, OnGripRelease);
            if (gripPressed) Gripped();
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.triggerButton, OnTriggerPressed, OnTriggerRelease);
            if (triggerPressed) Triggered();
        }
        public void OnGripPressed()
        {
            gripPressed = true;
            if (hoveredTargets.Count > 0)
            {
                switch (hoveredTypes[0])
                {
                    case HoverType.Controller:
                        break;
                    case HoverType.Acutator:
                        break;
                    case HoverType.Curve:
                        break;
                    case HoverType.Object:
                        break;
                }
            }
        }
        public void Gripped()
        {

        }
        public void OnGripRelease()
        {
            gripPressed = false;
        }
        public void OnTriggerPressed()
        {
            triggerPressed = true;
        }
        public void Triggered()
        {

        }
        public void OnTriggerRelease()
        {
            triggerPressed = false;
        }



        //private bool isGrip;
        //private bool isHovering;

        //private List<GameObject> hoveredCurves = new List<GameObject>();
        //private List<GameObject> hoveredActuators = new List<GameObject>();
        //private List<GameObject> hoveredObjects = new List<GameObject>();
        //private List<RigGoalController> hoveredGoals = new List<RigGoalController>();

        //private List<GameObject> dragedObject = new List<GameObject>();

        //#region GoalLayers
        //private void AddHoveredGoal(RigGoalController controller)
        //{
        //    hoveredGoals.Add(controller);
        //    if (hoveredGoals.Count == 1 && controller.gameObject.layer == 21)
        //    {
        //        controller.gameObject.layer = 22;
        //    }
        //}
        //private void RemoveHoverGoal(RigGoalController controller)
        //{
        //    if (controller.gameObject.layer == 22) controller.gameObject.layer = 21;
        //    hoveredGoals.Remove(controller);
        //    if (hoveredGoals.Count > 0 && hoveredGoals[0].gameObject.layer == 21) hoveredGoals[0].gameObject.layer = 22;
        //}
        //private void RemoveHoverGoal(int index)
        //{
        //    if (hoveredGoals[index].gameObject.layer == 22) hoveredGoals[index].gameObject.layer = 21;
        //    hoveredGoals.RemoveAt(index);
        //    if (hoveredGoals.Count > 0 && hoveredGoals[0].gameObject.layer == 21) hoveredGoals[0].gameObject.layer = 22;
        //}

        //private void ClearHoverGoals()
        //{
        //    if (hoveredGoals.Count > 0 && hoveredGoals[0].gameObject.layer == 22) hoveredGoals[0].gameObject.layer = 21;
        //    hoveredGoals.Clear();
        //}
        //#endregion

        //public void OnTriggerEnter(Collider other)
        //{
        //    if (other.tag == "Curve" && !hoveredCurves.Contains(other.gameObject)) hoveredCurves.Add(other.gameObject);
        //    else if (other.tag == "Goal" && other.TryGetComponent<RigGoalController>(out RigGoalController controller) && !hoveredGoals.Contains(controller)) AddHoveredGoal(controller);
        //    else if (other.tag == "Actuator" && !hoveredActuators.Contains(other.gameObject)) hoveredActuators.Add(other.gameObject);
        //    else if (Selection.SelectedObjects.Contains(other.gameObject) && !hoveredObjects.Contains(other.gameObject) && !other.TryGetComponent<RigController>(out RigController skin)) hoveredObjects.Add(other.gameObject);
        //}

        //public void OnTriggerExit(Collider other)
        //{
        //    if (isGrip) return;
        //    if (other.tag == "Curve" && hoveredCurves.Contains(other.gameObject))
        //    {
        //        hoveredCurves.Remove(other.gameObject);
        //    }
        //    else if (other.tag == "Goal" && other.TryGetComponent<RigGoalController>(out RigGoalController controller) && hoveredGoals.Contains(controller))
        //    {
        //        RemoveHoverGoal(controller);
        //    }
        //    else if (other.tag == "Actuator" && hoveredActuators.Contains(other.gameObject))
        //    {
        //        hoveredActuators.Remove(other.gameObject);
        //    }
        //    else if (hoveredObjects.Contains(other.gameObject)) hoveredObjects.Remove(other.gameObject);
        //}

        //public void Update()
        //{
        //    switch (animationTool.Mode)
        //    {
        //        case AnimationTool.EditMode.Curve:
        //            CurveMode();
        //            break;
        //        case AnimationTool.EditMode.Pose:
        //            PoseMode();
        //            break;
        //    }

        //    VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.primaryButton, () => animationTool.NextGizmo());
        //}

        //public void PoseMode()
        //{
        //    VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip,
        //        () =>
        //        {
        //            if (hoveredGoals.Count > 0)
        //            {
        //                animationTool.StartPose(hoveredGoals[0], transform);
        //                isGrip = true;
        //            }
        //            if (hoveredActuators.Count > 0)
        //            {
        //                animationTool.StartAcutator(hoveredActuators[0], transform);
        //                isGrip = true;
        //            }
        //            if (hoveredObjects.Count > 0)
        //            {
        //                foreach (GameObject gobject in hoveredObjects)
        //                {
        //                    animationTool.StartDragObject(gobject, transform);
        //                    dragedObject.Add(gobject);
        //                }
        //            }
        //        },
        //        () =>
        //        {
        //            if (isGrip)
        //            {
        //                animationTool.EndPose();
        //                ClearHoverGoals();
        //                hoveredActuators.Clear();
        //                isGrip = false;
        //            }
        //            if (dragedObject.Count > 0)
        //            {
        //                animationTool.EndDragObject();
        //                dragedObject.Clear();
        //                hoveredObjects.Clear();
        //            }
        //        });
        //    if (isGrip) isGrip = animationTool.DragPose(transform);
        //    if (dragedObject.Count > 0 && !isGrip)
        //    {
        //        dragedObject.ForEach(x => animationTool.DragObject(transform));
        //    }
        //    if (hoveredGoals.Count > 0 && hoveredGoals[0] == null) RemoveHoverGoal(0);
        //    SelectGoal();
        //}

        //public void CurveMode()
        //{
        //    VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip,
        //        () =>
        //        {
        //            if (hoveredCurves.Count > 0)
        //            {
        //                animationTool.StartDrag(hoveredCurves[0], transform);
        //                isGrip = true;
        //                isHovering = true;
        //            }
        //        },
        //        () =>
        //        {
        //            if (isGrip)
        //            {
        //                animationTool.ReleaseCurve();
        //                isGrip = false;
        //            }
        //        });
        //    if (isGrip) isGrip = animationTool.DragCurve(transform);
        //    else
        //    {
        //        if (hoveredCurves.Count > 0)
        //        {
        //            if (hoveredCurves[0] == null)
        //            {
        //                hoveredCurves.RemoveAt(0);
        //            }
        //            else
        //            {
        //                animationTool.HoverLine(hoveredCurves[0], transform.position);
        //                isHovering = true;
        //            }
        //        }
        //        else if (isHovering)
        //        {
        //            animationTool.StopHovering();
        //        }
        //    }
        //}

        //public void SelectGoal()
        //{
        //    VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.triggerButton,
        //        onPress: () =>
        //    {
        //        if (hoveredGoals.Count > 0) animationTool.SelectGoal(hoveredGoals[0]);
        //        else animationTool.SelectEmpty();
        //    });
        //}

    }

}