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
        public enum TargetType { none, Actuator, Controller, Curve, Object };
        public List<TargetType> HoveredTypes;
        public TargetType CurrentDragged = TargetType.none;
        public TargetType CurrentSelection = TargetType.none;

        private GameObject interactingObject;
        private List<GameObject> hoveredTargets = new List<GameObject>();

        private bool gripPressed;
        private bool triggerPressed;

        #region hovering
        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Controller")
            {
                AddToHovered(other.gameObject, TargetType.Controller);
            }
            if (other.gameObject.tag == "Curve")
            {
                AddToHovered(other.gameObject, TargetType.Curve);
            }
            if (other.gameObject.tag == "Actuator")
            {
                AddToHovered(other.gameObject, TargetType.Actuator);
            }
            if (Selection.SelectedObjects.Contains(other.gameObject) && !other.TryGetComponent(out RigController skin))
            {
                AddToHovered(other.gameObject, TargetType.Object);
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

        /// <summary>
        /// Remove target from hovered objects
        /// </summary>
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
                case TargetType.Controller:
                    animationTool.HoverController(hoveredTargets[0]);

                    break;
                case TargetType.Actuator:
                    animationTool.HoverActuator(hoveredTargets[0]);
                    break;
                case TargetType.Curve:
                    animationTool.HoverCurve(hoveredTargets[0], transform);
                    break;
                case TargetType.Object:
                    animationTool.HoverObject(hoveredTargets[0]);
                    break;
            }
        }
        private void EndHover(GameObject target, TargetType type)
        {
            switch (type)
            {
                case TargetType.Controller:
                    animationTool.StopHoverController(target);
                    break;
                case TargetType.Actuator:
                    animationTool.StopHoverActuator(target);
                    break;
                case TargetType.Curve:
                    animationTool.StopHoverCurve(target);
                    break;
                case TargetType.Object:
                    animationTool.StopHoverObject(target);
                    break;
            }
        }


        #endregion

        public void Update()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip, OnGripPressed, OnGripRelease);
            if (gripPressed) Gripped();
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.triggerButton, OnTriggerPressed, OnTriggerRelease);
            if (triggerPressed) Triggered();
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.primaryButton, () => animationTool.NextGizmo());
            if (HoveredTypes.Count > 0 && HoveredTypes[0] == TargetType.Curve) animationTool.UpdateHoverCurve(hoveredTargets[0], transform);
        }

        #region Grip
        public void OnGripPressed()
        {
            gripPressed = true;
            CheckForNull();
            if (hoveredTargets.Count > 0)
            {
                interactingObject = hoveredTargets[0];
                switch (HoveredTypes[0])
                {
                    case TargetType.Controller:
                        animationTool.GrabController(hoveredTargets[0], transform);
                        CurrentDragged = TargetType.Controller;
                        break;
                    case TargetType.Actuator:
                        animationTool.GrabActuator(hoveredTargets[0], transform);
                        CurrentDragged = TargetType.Actuator;
                        break;
                    case TargetType.Curve:
                        animationTool.GrabCurve(hoveredTargets[0], transform);
                        CurrentDragged = TargetType.Curve;
                        break;
                    case TargetType.Object:
                        animationTool.GrabObject(hoveredTargets[0], transform);
                        CurrentDragged = TargetType.Object;
                        break;
                }
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

        public void Gripped()
        {
            switch (CurrentDragged)
            {
                case TargetType.none: return;
                case TargetType.Controller:
                    animationTool.DragController(transform);
                    break;
                case TargetType.Actuator:
                    animationTool.DragActuator(transform);
                    break;
                case TargetType.Curve:
                    animationTool.DragCurve(transform);
                    break;
                case TargetType.Object:
                    animationTool.DragObject(transform);
                    break;
            }
        }
        public void OnGripRelease()
        {
            gripPressed = false;
            switch (CurrentDragged)
            {
                case TargetType.none: return;
                case TargetType.Controller:
                    animationTool.ReleaseController();
                    break;
                case TargetType.Actuator:
                    animationTool.ReleaseActuator();
                    break;
                case TargetType.Curve:
                    animationTool.ReleaseCurve();
                    break;
                case TargetType.Object:
                    animationTool.ReleaseObject();
                    break;
            }
            EndHover(interactingObject, CurrentDragged);
            CurrentDragged = TargetType.none;
            interactingObject = null;
            if (hoveredTargets.Count > 0) StartHover(hoveredTargets[0], HoveredTypes[0]);
        }
        #endregion

        #region Trigger
        public void OnTriggerPressed()
        {
            triggerPressed = true;
            CheckForNull();
            if (HoveredTypes.Count == 0)
            {
                animationTool.SelectEmpty();
                return;
            }
            interactingObject = hoveredTargets[0];
            switch (HoveredTypes[0])
            {
                case TargetType.Controller:
                    animationTool.SelectController(hoveredTargets[0]);
                    CurrentSelection = TargetType.Controller;
                    break;
                case TargetType.Actuator:
                    animationTool.SelectActuator(hoveredTargets[0]);
                    CurrentSelection = TargetType.Actuator;
                    break;
                case TargetType.Curve:
                    animationTool.SelectCurve(hoveredTargets[0], transform);
                    CurrentSelection = TargetType.Curve;
                    break;
                case TargetType.Object:
                    animationTool.SelectObject(hoveredTargets[0]);
                    CurrentSelection = TargetType.Object;
                    break;
            }
        }
        public void Triggered()
        {
            switch (CurrentSelection)
            {
                case TargetType.Curve:
                    animationTool.SelectingCurve(transform);
                    break;
                default: return;
            }
        }
        public void OnTriggerRelease()
        {
            triggerPressed = false;
            switch (CurrentSelection)
            {
                case TargetType.Curve:
                    animationTool.EndSelectionCurve();
                    break;
                default: break;
            }
            EndHover(interactingObject, CurrentSelection);
            interactingObject = null;
            CurrentSelection = TargetType.none;
        }
        #endregion
    }

}