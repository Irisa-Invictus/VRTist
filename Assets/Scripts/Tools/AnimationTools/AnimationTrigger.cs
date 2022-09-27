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
                StartHovering(other.gameObject, TargetType.Controller);
            }
            if (other.gameObject.tag == "Curve")
            {
                if (hoveredTargets.Count > 0) StopHovering();
                StartHovering(other.gameObject, TargetType.Curve);
            }
            if (other.gameObject.tag == "Actuator")
            {
                if (hoveredTargets.Count > 0) StopHovering();
                StartHovering(other.gameObject, TargetType.Actuator);
            }
            if (Selection.SelectedObjects.Contains(other.gameObject) && !other.TryGetComponent(out RigController skin))
            {
                if (hoveredTargets.Count > 0) StopHovering();
                StartHovering(other.gameObject, TargetType.Object);
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
        /// <summary>
        /// Add target to hovered targets, and set as hovered
        /// </summary>
        private void StartHovering(GameObject target, TargetType type)
        {
            if (hoveredTargets.Count > 0) StopHovering();
            hoveredTargets.Add(target);
            HoveredTypes.Add(type);
            if (CurrentDragged != TargetType.none || CurrentSelection != TargetType.none) return;
            ReplaceHover();
        }
        /// <summary>
        /// Set hoveredObject 0 as not hovered
        /// </summary>
        private void StopHovering()
        {
            if (CurrentDragged != TargetType.none || CurrentSelection != TargetType.none) return;
            switch (HoveredTypes[0])
            {
                case TargetType.Controller:
                    hoveredController.EndHover();
                    hoveredController = null;
                    break;
                case TargetType.Actuator:
                    animationTool.StopHoverActuator(hoveredTargets[0]);
                    break;
                case TargetType.Curve:
                    animationTool.StopHoverCurve(hoveredTargets[0]);
                    break;
                case TargetType.Object:
                    animationTool.StopHoverObject(hoveredTargets[0]);
                    break;
            }
        }

        /// <summary>
        /// Remove target from hovered objects
        /// </summary>
        private void RemoveFromHovered(GameObject target)
        {
            int index = hoveredTargets.IndexOf(target);
            if (index == -1) return;
            if (index == 0) StopHovering();

            hoveredTargets.RemoveAt(index);
            HoveredTypes.RemoveAt(index);

            if (index == 0) ReplaceHover();
        }
        /// <summary>
        /// Set hoveredObject 0 as hovered
        /// </summary>
        private void ReplaceHover()
        {
            if (hoveredTargets.Count > 0)
            {
                switch (HoveredTypes[0])
                {
                    case TargetType.Controller:
                        hoveredController = hoveredTargets[0].GetComponent<RigObjectController>();
                        hoveredController.StartHover();
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
            if (hoveredTargets.Count > 0)
            {
                switch (HoveredTypes[0])
                {
                    case TargetType.Controller:
                        animationTool.GrabController(hoveredController, transform);
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
            CurrentDragged = TargetType.none;
            StopHovering();
            ReplaceHover();
        }
        #endregion

        #region Trigger
        public void OnTriggerPressed()
        {
            triggerPressed = true;
            switch (HoveredTypes[0])
            {
                case TargetType.none:
                    animationTool.SelectEmpty();
                    break;
                case TargetType.Controller:
                    animationTool.SelectController(hoveredController);
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
            CurrentSelection = TargetType.none;
            StopHovering();
            ReplaceHover();
        }
        #endregion
    }

}