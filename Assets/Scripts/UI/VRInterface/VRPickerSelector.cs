using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class VRPickerSelector : MonoBehaviour
    {
        public VRPickerTool PickerTool;
        public NavigationOptions navigation;

        public enum TargetType { none, Base, Controller, Gizmo, Actuator }
        public List<TargetType> HoveredTypes;
        public TargetType CurrentDragged = TargetType.none;
        public TargetType CurrentSelection = TargetType.none;

        private GameObject interactingObject;
        private List<GameObject> hoveredTargets = new List<GameObject>();

        private bool gripPressed;
        private bool triggerPressed;

        public void OnTriggerEnter(Collider other)
        {
            if (!other.transform.IsChildOf(PickerTool.Picker.transform)) return;
            if (other.gameObject == PickerTool.Picker.PickerBase)
            {
                AddToHovered(other.gameObject, TargetType.Base);
            }
            if (other.gameObject == PickerTool.Picker.PickerGizmo)
            {
                AddToHovered(other.gameObject, TargetType.Gizmo);
            }
            if (other.gameObject.CompareTag("Controller"))
            {
                AddToHovered(other.gameObject, TargetType.Controller);
            }
            if (other.gameObject.CompareTag("Actuator"))
            {
                AddToHovered(other.gameObject, TargetType.Actuator);
            }
        }
        public void OnTriggerExit(Collider other)
        {
            if (!other.transform.IsChildOf(PickerTool.Picker.transform)) return;
            if (other.gameObject == PickerTool.Picker.PickerBase)
            {
                RemoveFromHovered(other.gameObject);
            }
            if (other.gameObject == PickerTool.Picker.PickerGizmo)
            {
                RemoveFromHovered(other.gameObject);
            }
            if (other.gameObject.CompareTag("Controller"))
            {
                RemoveFromHovered(other.gameObject);
            }
            if (other.gameObject.CompareTag("Actuator"))
            {
                RemoveFromHovered(other.gameObject);
            }
        }

        private void AddToHovered(GameObject target, TargetType type)
        {
            if (hoveredTargets.Contains(target)) return;
            if (HoveredTypes.Count > 0)
            {
                EndHover(hoveredTargets[0], HoveredTypes[0]);
            }
            hoveredTargets.Insert(0, target);
            HoveredTypes.Insert(0, type);
            StartHover(target, type);
        }

        private void RemoveFromHovered(GameObject target)
        {
            int index = hoveredTargets.IndexOf(target);
            if (index == -1) return;
            if (index == 0)
            {
                EndHover(hoveredTargets[0], HoveredTypes[0]);
            }
            hoveredTargets.RemoveAt(index);
            HoveredTypes.RemoveAt(index);
            if (index == 0 && HoveredTypes.Count > 0)
            {
                CheckForNull();
                StartHover(hoveredTargets[0], HoveredTypes[0]);
            }
        }

        private void StartHover(GameObject target, TargetType type)
        {
            switch (type)
            {
                case TargetType.Actuator:
                    PickerTool.HoverActuator(hoveredTargets[0]);
                    break;
                case TargetType.Base:
                    PickerTool.HoverBase();
                    break;
                case TargetType.Controller: break;
                case TargetType.Gizmo: break;
            }
        }

        private void EndHover(GameObject target, TargetType type)
        {
            switch (type)
            {
                case TargetType.Actuator:
                    PickerTool.HoverActuatorEnd(target);
                    break;
                case TargetType.Base:
                    PickerTool.HoverBaseEnd();
                    break;
                case TargetType.Controller: break;
                case TargetType.Gizmo: break;
            }
        }

        private void CheckForNull()
        {
            if (hoveredTargets.Count > 0 && hoveredTargets[0] == null)
            {
                hoveredTargets.RemoveAt(0);
                HoveredTypes.RemoveAt(0);
                CheckForNull();
            }
        }

        private bool hoverBase;
        private bool HoverBase
        {
            get { return hoverBase; }
            set
            {
                hoverBase = value;
                PickerTool.Picker.PickerBase.GetComponent<MeshRenderer>().material.SetColor("_BASE_COLOR", value ? Color.red : Color.black);
            }
        }
        private bool dragBase;

        private bool hoverGizmo;
        private bool dragGizmo;


        private bool HoverController;
        private bool dragController;

        private bool HoverActuator;
        private bool dragActuator;
        private GameObject hoveredActuator;

        private Vector2 axisValues;

        //private List<RigGoalController> hoveredGoals = new List<RigGoalController>();
        private RigObjectController HoverdController;

        public void Update()
        {
            Grip();
            Trigger();
            Joystick();
        }

        private void Joystick()
        {
            if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                axisValues = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                if (axisValues.magnitude > 0.5f && !dragBase && !dragGizmo) PickerTool.Picker.RotateClone(axisValues);
            }
        }

        private void Trigger()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.trigger,
                () =>
                {
                    if (HoverController)
                    {
                        PickerTool.SelectController(HoverdController);
                    }

                    else
                    {
                        PickerTool.SelectEmpty();
                    }
                },
                () =>
                {
                });
        }

        private void Grip()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip,
                () =>
                {
                    if (HoverBase)
                    {
                        PickerTool.BaseGrab(transform);
                        dragBase = true;
                        return;
                    }
                    if (hoverGizmo)
                    {
                        PickerTool.GizmoGrab(transform);
                        dragGizmo = true;
                    }
                    if (HoverActuator)
                    {
                        PickerTool.ActuatorGrab(hoveredActuator, transform);
                        dragActuator = true;
                    }

                    if (HoverController)
                    {
                        PickerTool.ControllerGrab(transform, HoverdController);
                        dragController = true;
                    }
                },
                () =>
                {
                    if (dragBase)
                    {
                        PickerTool.BaseRelease();
                        dragBase = false;
                        return;
                    }
                    if (dragGizmo)
                    {
                        PickerTool.GizmoRelease();
                        dragGizmo = false;
                    }
                    if (dragActuator)
                    {
                        PickerTool.ActuatorRelease();
                        dragActuator = false;
                    }

                    if (dragController)
                    {
                        PickerTool.ControllerRelease();
                        dragController = false;
                    }
                });
            if (dragBase) PickerTool.BaseDrag(transform, axisValues);
            if (dragController) PickerTool.ControllerDrag(transform, axisValues);
            if (dragActuator) PickerTool.ActuatorDrag(transform);
            if (dragGizmo) PickerTool.GizmoDrag(transform, axisValues);
            //if (DragLanguette)PickerTool.LanguetteDrag(transform,Vector3.zero);
        }

        //public void OnTriggerEnter(Collider other)
        //{
        //    if (!other.transform.IsChildOf(PickerTool.Picker.transform)) return;
        //    if (!HoverBase) HoverBase = other.gameObject == PickerTool.Picker.PickerBase;
        //    if (!hoverGizmo) hoverGizmo = other.gameObject == PickerTool.Picker.PickerGizmo.gameObject;
        //    if (other.gameObject.CompareTag("Controller")) HoveredController(other.GetComponent<RigObjectController>());
        //    //if (other.tag == "Goal" && other.TryGetComponent<RigGoalController>(out RigGoalController controller) && !hoveredGoals.Contains(controller)) AddHoveredGoal(controller);
        //    if (other.gameObject.CompareTag("Actuator") && hoveredActuator != other.gameObject)
        //    {
        //        hoveredActuator = other.gameObject;
        //        HoverActuator = true;
        //    }
        //    // if (other.tag == "Goal")
        //}

        //public void OnTriggerExit(Collider other)
        //{
        //    if (!other.transform.IsChildOf(PickerTool.Picker.transform)) return;
        //    if (other.gameObject == PickerTool.Picker.PickerBase) HoverBase = false;
        //    if (other.gameObject == PickerTool.Picker.PickerGizmo.gameObject) hoverGizmo = false;
        //    if (other.gameObject.CompareTag("Controller") && HoverdController == other.GetComponent<RigObjectController>()) RemoveHoveredController();
        //    if (other.tag == "Actuator" && hoveredActuator == other.gameObject)
        //    {
        //        hoveredActuator = null;
        //        HoverActuator = false;
        //    }
        //}

        private void HoveredController(RigObjectController controller)
        {
            if (HoverdController != null) RemoveHoveredController();
            HoverController = true;
            HoverdController = controller;
            controller.StartHover();
        }

        private void RemoveHoveredController()
        {
            HoverdController.EndHover();
            HoverdController = null;
            HoverController = false;

        }

    }
}
