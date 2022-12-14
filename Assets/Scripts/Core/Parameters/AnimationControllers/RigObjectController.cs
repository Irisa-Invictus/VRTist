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

namespace VRtist
{
    public abstract class RigObjectController : MonoBehaviour
    {
        public RigObjectController pairedController;
        public bool isPickerController;
        public bool isTPose;

        internal MeshRenderer meshRenderer;
        internal bool isHovered;
        internal bool isSelected;
        internal int startLayer;

        private Matrix4x4 initialMatrix;
        [SerializeField]
        private Vector3 initialLocalPosition;
        [SerializeField]
        private Quaternion initialLocalRotation;
        [SerializeField]
        private Vector3 initialLocalScale;

        public void Start()
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            startLayer = gameObject.layer;
        }

        public virtual void ResetPosition(bool applyToPair = true, bool applyToChild = true)
        {
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            transform.localScale = initialLocalScale;
            UpdateController(applyToPair);
        }

        public virtual void SetStartPosition()
        {
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
            initialLocalScale = transform.localScale;
        }

        public void CopiePairedController()
        {
            if (pairedController == null) return;
            transform.localPosition = pairedController.transform.localPosition;
            transform.localRotation = pairedController.transform.localRotation;
            transform.localScale = pairedController.transform.localScale;
            UpdateController(applyToPair: false, applyToChild: false);
        }

        public abstract void OnSelect();
        public abstract void OnDeselect();
        public abstract void OnGrab(Transform mouthpiece, bool data);
        public abstract void OnDrag(Transform mouthpiece);
        public abstract void OnRelease();

        public abstract void OnGrabGizmo(Transform mouthpiece, GoalGizmo gizmo, GoalGizmo.GizmoTool tool, AnimationTool.Vector3Axis axis, bool data);

        public abstract void OnDragGizmo(Transform mouthpiece);
        public abstract void OnReleaseGizmo();

        public abstract void StartHover();
        public abstract void EndHover();

        public abstract void UpdateController(bool applyToPair = true, bool applyToChild = true);

        public abstract List<JointController> GetTargets();
    }
}