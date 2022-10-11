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
        private Vector2 axisValues;

        public void OnTriggerEnter(Collider other)
        {
            if (!other.transform.IsChildOf(PickerTool.Picker.transform)) return;
            if (other.gameObject == PickerTool.Picker.PickerBase)
            {
                AddToHovered(other.gameObject, TargetType.Base);
            }
            if (other.gameObject == PickerTool.Picker.PickerGizmo.gameObject)
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
                case TargetType.Controller:
                    PickerTool.HoverController(hoveredTargets[0]);
                    break;
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
                case TargetType.Controller:
                    PickerTool.StopHoverController(target);
                    break;
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




        public void Update()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip, OnGripPressed, OnGripRelease);
            if (gripPressed) Gripped();
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.triggerButton, OnTriggerPressed, OnTriggerRelease);
            if (triggerPressed) Triggered();
            Joystick();
        }

        public void OnGripPressed()
        {
            gripPressed = true;
            CheckForNull();
            if (hoveredTargets.Count > 0)
            {
                interactingObject = hoveredTargets[0];
                switch (HoveredTypes[0])
                {
                    case TargetType.Actuator:
                        CurrentDragged = TargetType.Actuator;
                        PickerTool.ActuatorGrab(interactingObject, transform);
                        break;
                    case TargetType.Base:
                        CurrentDragged = TargetType.Base;
                        PickerTool.BaseGrab(transform);
                        break;
                    case TargetType.Controller:
                        CurrentDragged = TargetType.Controller;
                        PickerTool.ControllerGrab(interactingObject, transform);
                        break;
                    case TargetType.Gizmo:
                        CurrentDragged = TargetType.Gizmo;
                        PickerTool.GizmoGrab(transform);
                        break;
                }
            }
        }
        public void Gripped()
        {
            switch (CurrentDragged)
            {
                case TargetType.none: return;
                case TargetType.Actuator:
                    PickerTool.ActuatorDrag(transform);
                    break;
                case TargetType.Base:
                    PickerTool.BaseDrag(transform, axisValues);
                    break;
                case TargetType.Controller:
                    PickerTool.ControllerDrag(transform, axisValues);
                    break;
                case TargetType.Gizmo:
                    PickerTool.GizmoDrag(transform, axisValues);
                    break;
            }
        }
        public void OnGripRelease()
        {
            gripPressed = false;
            switch (CurrentDragged)
            {
                case TargetType.none: return;
                case TargetType.Actuator:
                    PickerTool.ActuatorRelease();
                    break;
                case TargetType.Base:
                    PickerTool.BaseRelease();
                    break;
                case TargetType.Controller:
                    PickerTool.ControllerRelease();
                    break;
                case TargetType.Gizmo:
                    PickerTool.GizmoRelease();
                    break;
            }
            EndHover(interactingObject, CurrentDragged);
            CurrentDragged = TargetType.none;
            interactingObject = null;
            if (hoveredTargets.Count > 0) StartHover(hoveredTargets[0], HoveredTypes[0]);
        }
        public void OnTriggerPressed()
        {
            triggerPressed = true;
            CheckForNull();
            if (HoveredTypes.Count == 0)
            {
                PickerTool.SelectEmpty();
                return;
            }
            interactingObject = hoveredTargets[0];
            switch (HoveredTypes[0])
            {
                case TargetType.Actuator:
                    CurrentSelection = TargetType.Actuator;
                    break;
                case TargetType.Base:
                    CurrentSelection = TargetType.Base;
                    break;
                case TargetType.Controller:
                    CurrentSelection = TargetType.Controller;
                    PickerTool.SelectController(hoveredTargets[0]);
                    break;
                case TargetType.Gizmo:
                    CurrentSelection = TargetType.Gizmo;
                    break;
            }
        }
        public void Triggered()
        {
        }
        public void OnTriggerRelease()
        {
            triggerPressed = false;
            EndHover(interactingObject, CurrentSelection);
            interactingObject = null;
            CurrentSelection = TargetType.none;
        }

        private void Joystick()
        {
            if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                axisValues = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                if (axisValues.magnitude > 0.5f && CurrentSelection == TargetType.none && CurrentDragged == TargetType.none) PickerTool.Picker.RotateClone(axisValues);
            }
        }

        public void OnDisable()
        {
            for (int i = 0; i < hoveredTargets.Count; i++)
            {
                EndHover(hoveredTargets[i], HoveredTypes[i]);
            }
            HoveredTypes.Clear();
            hoveredTargets.Clear();
        }

    }
}
