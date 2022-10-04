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

        public void OnTriggerEnter(Collider other)
        {
            if (!other.transform.IsChildOf(PickerTool.Picker.transform)) return;
            if (!HoverBase) HoverBase = other.gameObject == PickerTool.Picker.PickerBase;
            //hoverGizmo = other.gameObject == PickerTool.Gizmo.gameObject;
            if (other.gameObject.CompareTag("Controller")) HoveredController(other.GetComponent<RigObjectController>());
            //if (other.tag == "Goal" && other.TryGetComponent<RigGoalController>(out RigGoalController controller) && !hoveredGoals.Contains(controller)) AddHoveredGoal(controller);
            if (other.tag == "Actuator" && hoveredActuator != other.gameObject)
            {
                hoveredActuator = other.gameObject;
                HoverActuator = true;
            }
            // if (other.tag == "Goal")
        }

        public void OnTriggerExit(Collider other)
        {
            if (!other.transform.IsChildOf(PickerTool.Picker.transform)) return;
            if (other.gameObject == PickerTool.Picker.PickerBase) HoverBase = false;
            //if (other.gameObject == PickerTool.Gizmo.gameObject) hoverGizmo = false;
            if (other.gameObject.CompareTag("Controller") && HoverdController == other.GetComponent<RigObjectController>()) RemoveHoveredController();
            if (other.tag == "Actuator" && hoveredActuator == other.gameObject)
            {
                hoveredActuator = null;
                HoverActuator = false;
            }
        }

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
