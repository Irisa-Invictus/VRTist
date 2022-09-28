/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 * &
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
using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Display motion trails of animated objects.
    /// </summary>
    public class Anim3DCurveManager : MonoBehaviour
    {
        private bool displaySelectedCurves = true;
        public Transform curvesParent;
        public GameObject curvePrefab;

        private readonly float lineWidth = 0.001f;

        private bool isAnimationTool;
        private float currentCurveOffset;

        private readonly Dictionary<GameObject, GameObject> curves = new Dictionary<GameObject, GameObject>();
        private Dictionary<RigController, Dictionary<JointController, GameObject>> jointCurves = new Dictionary<RigController, Dictionary<JointController, GameObject>>();

        void Start()
        {
            ToolsUIManager.Instance.OnToolChangedEvent += OnToolChanged;
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            GlobalState.Animation.onFrameEvent.AddListener(UpdateOffset);
            GlobalState.Animation.onChangeCurve.AddListener(OnCurveChanged);
            GlobalState.Animation.onAddAnimation.AddListener(OnAnimationAdded);
            GlobalState.Animation.onRemoveAnimation.AddListener(OnAnimationRemoved);
            GlobalState.ObjectMovingEvent.AddListener(OnObjectMoved);
        }

        private void OnObjectMoved(GameObject gObject)
        {
            if (gObject.TryGetComponent(out RigController rigController) && jointCurves.ContainsKey(rigController)) UpdateFromSelection();
        }

        void Update()
        {
            if (displaySelectedCurves != GlobalState.Settings.Display3DCurves)
            {
                displaySelectedCurves = GlobalState.Settings.Display3DCurves;
                if (displaySelectedCurves)
                    UpdateFromSelection();
                else
                    ClearCurves();
            }
            if (currentCurveOffset != GlobalState.Settings.CurveForwardOffset)
            {
                currentCurveOffset = GlobalState.Settings.CurveForwardOffset;
                UpdateOffsetValue();
            }
            UpdateCurvesWidth();
        }


        void UpdateCurvesWidth()
        {
            foreach (GameObject curve in curves.Values)
            {
                LineRenderer line = curve.GetComponent<LineRenderer>();
                line.startWidth = lineWidth / GlobalState.WorldScale;
                line.endWidth = line.startWidth;
            }
        }
        void OnSelectionChanged(HashSet<GameObject> previousSelectedObjects, HashSet<GameObject> selectedObjects)
        {
            if (GlobalState.Settings.Display3DCurves)
                UpdateFromSelection();
        }


        void UpdateFromSelection()
        {
            ClearCurves();
            foreach (GameObject gObject in Selection.SelectedObjects)
            {
                AddCurve(gObject);
                //if (gObject.TryGetComponent(out RigController skinController))
                //{
                //    JointController jointController = skinController.RootObject.GetComponent<JointController>();
                //    AddJointCurve(jointController, skinController);
                //}
            }
        }

        void OnCurveChanged(GameObject gObject, AnimatableProperty property)
        {
            if (gObject.TryGetComponent(out RigController rigController))
            {
                if (jointCurves.TryGetValue(rigController, out Dictionary<JointController, GameObject> curves))
                {
                    foreach (KeyValuePair<JointController, GameObject> pair in curves)
                    {
                        UpdateJointCurve(pair.Key, pair.Value);
                    }
                }
            }

            if (property != AnimatableProperty.PositionX && property != AnimatableProperty.PositionY && property != AnimatableProperty.PositionZ)
                return;

            if (!Selection.IsSelected(gObject))
                return;

            UpdateCurve(gObject);
        }


        void OnAnimationAdded(GameObject gObject)
        {
            if (!Selection.IsSelected(gObject))
                return;
            UpdateCurve(gObject);
        }

        void OnAnimationRemoved(GameObject gObject)
        {
            if (gObject.TryGetComponent<RigController>(out RigController controller))
            {
                RecursiveDeleteCurve(gObject.transform);
                if (jointCurves.ContainsKey(controller)) jointCurves.Remove(controller);
            }
            else
            {
                DeleteCurve(gObject);
            }
        }

        void OnToolChanged(object sender, ToolChangedArgs args)
        {
            bool switchToAnim = args.toolName == "Animation";
            if (switchToAnim && !isAnimationTool)
            {
                UpdateFromSelection();
            }
            if (!switchToAnim && isAnimationTool)
            {
                DeleteAllJointCurves();
            }
            isAnimationTool = switchToAnim;
        }

        void ClearCurves()
        {
            foreach (GameObject curve in curves.Values)
                Destroy(curve);
            curves.Clear();
            jointCurves.Clear();
        }

        void DeleteCurve(GameObject gObject)
        {
            if (curves.ContainsKey(gObject))
            {
                Destroy(curves[gObject]);
                curves.Remove(gObject);
            }
        }

        void RecursiveDeleteCurve(Transform target)
        {
            DeleteCurve(target.gameObject);
            foreach (Transform child in target)
            {
                RecursiveDeleteCurve(child);
            }
        }

        void DeleteAllJointCurves()
        {
            List<GameObject> removedCurves = new List<GameObject>();
            foreach (KeyValuePair<GameObject, GameObject> curve in curves)
            {
                if (curve.Key.TryGetComponent(out JointController controller) && controller.PathToRoot[0] != controller.transform)
                {
                    Destroy(curve.Value);
                    removedCurves.Add(curve.Key);
                }
            }
            jointCurves.Clear();
            removedCurves.ForEach(x => curves.Remove(x));
        }

        void UpdateCurve(GameObject gObject)
        {
            AddCurve(gObject);
        }

        private void UpdateJointCurve(JointController joint, GameObject curve)
        {
            DeleteCurve(curve);
            AddJointCurve(joint, joint.RootController);
        }


        void AddCurve(GameObject gObject)
        {
            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gObject);
            if (null == animationSet)
            {
                return;
            }

            Curve positionX = animationSet.GetCurve(AnimatableProperty.PositionX);
            Curve positionY = animationSet.GetCurve(AnimatableProperty.PositionY);
            Curve positionZ = animationSet.GetCurve(AnimatableProperty.PositionZ);

            if (null == positionX || null == positionY || null == positionZ)
                return;

            if (positionX.keys.Count == 0)
                return;

            if (positionX.keys.Count != positionY.keys.Count || positionX.keys.Count != positionZ.keys.Count)
                return;

            int frameStart = Mathf.Clamp(positionX.keys[0].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
            int frameEnd = Mathf.Clamp(positionX.keys[positionX.keys.Count - 1].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);

            Matrix4x4 matrix = curvesParent.worldToLocalMatrix * gObject.transform.parent.localToWorldMatrix;

            List<Vector3> positions = new List<Vector3>();
            for (int i = frameStart; i <= frameEnd; i++)
            {
                positionX.Evaluate(i, out float x);
                positionY.Evaluate(i, out float y);
                positionZ.Evaluate(i, out float z);
                Vector3 position = new Vector3(x, y, z);
                position = matrix.MultiplyPoint(position);

                positions.Add(position);
            }

            int count = positions.Count;
            GameObject curve3D = curves.TryGetValue(gObject, out GameObject current) ? current : Instantiate(curvePrefab, curvesParent);

            LineRenderer line = curve3D.GetComponent<LineRenderer>();
            line.positionCount = count;
            for (int index = 0; index < count; index++)
            {
                line.SetPosition(index, positions[index]);
            }
            line.startWidth = lineWidth / GlobalState.WorldScale;
            line.endWidth = line.startWidth;

            MeshCollider collider = curve3D.GetComponent<MeshCollider>();
            Mesh lineMesh = new Mesh();
            line.BakeMesh(lineMesh);
            collider.sharedMesh = lineMesh;

            curves[gObject] = curve3D;
        }

        private void AddJointCurve(JointController jointController, RigController skinController)
        {
            AnimationSet jointAnimation = GlobalState.Animation.GetObjectAnimation(jointController.gameObject);
            if (null == jointAnimation) return;

            Curve rotationX = jointAnimation.GetCurve(AnimatableProperty.RotationX);
            if (rotationX.keys.Count == 0) return;

            int frameStart = Mathf.Clamp(rotationX.keys[0].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
            int frameEnd = Mathf.Clamp(rotationX.keys[rotationX.keys.Count - 1].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);

            List<Vector3> positions = new List<Vector3>();
            GameObject curve3D = curves.TryGetValue(jointController.gameObject, out GameObject current) ? current : Instantiate(curvePrefab, curvesParent);

            Vector3 forwardOffset = (skinController.transform.forward * skinController.transform.localScale.x) * currentCurveOffset;

            jointController.CheckAnimations();
            for (int i = frameStart; i <= frameEnd; i++)
            {
                Vector3 position = curve3D.transform.InverseTransformDirection(jointController.FramePosition(i) - (forwardOffset * i));
                positions.Add(position);
            }
            LineRenderer line = curve3D.GetComponent<LineRenderer>();
            line.positionCount = positions.Count;
            line.SetPositions(positions.ToArray());

            line.material.color = jointController.color;

            line.startWidth = lineWidth / GlobalState.WorldScale;
            line.endWidth = line.startWidth;

            curve3D.transform.position = forwardOffset * GlobalState.Animation.CurrentFrame;

            MeshCollider collider = curve3D.GetComponent<MeshCollider>();
            Mesh lineMesh = new Mesh();
            line.BakeMesh(lineMesh);
            collider.sharedMesh = lineMesh;
            curves[jointController.gameObject] = curve3D;

            if (jointCurves.ContainsKey(skinController))
            {
                if (jointCurves[skinController].TryGetValue(jointController, out GameObject oldCurve) && oldCurve != null) DeleteCurve(oldCurve);
                jointCurves[skinController][jointController] = curve3D;
            }
            else
            {
                jointCurves[skinController] = new Dictionary<JointController, GameObject>();
                jointCurves[skinController][jointController] = curve3D;
            }
        }

        public GameObject GetObjectFromCurve(GameObject curve)
        {
            foreach (KeyValuePair<GameObject, GameObject> pair in curves)
            {
                if (pair.Value == curve) return pair.Key;
            }
            return null;
        }

        public bool TryGetLine(GameObject gobject, out LineRenderer line)
        {
            if (!curves.TryGetValue(gobject, out GameObject value))
            {
                line = null;
                return false;
            }
            return (value.TryGetComponent<LineRenderer>(out line));
        }

        private void UpdateOffsetValue()
        {
            foreach (KeyValuePair<RigController, Dictionary<JointController, GameObject>> curves in jointCurves)
            {
                foreach (KeyValuePair<JointController, GameObject> pair in curves.Value)
                {
                    DeleteCurve(pair.Value);
                    AddJointCurve(pair.Key, pair.Key.RootController);
                }
            }
        }

        private void UpdateOffset(int frame)
        {
            foreach (KeyValuePair<RigController, Dictionary<JointController, GameObject>> curves in jointCurves)
            {
                Vector3 forwardVector = (curves.Key.transform.forward * curves.Key.transform.localScale.x) * currentCurveOffset;
                foreach (KeyValuePair<JointController, GameObject> pair in curves.Value)
                {
                    pair.Value.transform.position = forwardVector * frame;
                }
            }
        }

        public void SelectJoint(JointController joint)
        {
            AddJointCurve(joint, joint.RootController);
        }
        public void UnSelectJoint(JointController joint)
        {
            if (jointCurves.TryGetValue(joint.RootController, out Dictionary<JointController, GameObject> jointCurve))
            {
                if (jointCurve.TryGetValue(joint, out GameObject curve))
                {
                    DeleteCurve(joint.gameObject);
                    jointCurve.Remove(joint);
                }
                if (jointCurves[joint.RootController].Count == 0) jointCurves.Remove(joint.RootController);
            }
        }

        internal void HoverCurve(GameObject gameObject, Transform mouthpiece)
        {
            if (gameObject.TryGetComponent(out LineRenderer line))
            {
                line.material.color = Color.red;
            }
        }

        internal void UpdateHoverCurve(GameObject gameObject, Transform transform)
        {
        }

        internal void StopHover(GameObject curve)
        {
            if (curve.TryGetComponent(out LineRenderer line))
            {
                GameObject target = GetObjectFromCurve(curve);
                if (target.TryGetComponent(out JointController joint))
                {
                    line.material.color = joint.color;
                }
                else
                {
                    line.material.color = new Color(1, 0.7f, 0.3f);
                }
            }
        }

        internal int GetFrameFromPoint(GameObject gameObject, Vector3 pointPosition)
        {
            if (!TryGetLine(gameObject, out LineRenderer line)) return -1;
            AnimationSet anim = GlobalState.Animation.GetObjectAnimation(gameObject);
            Vector3 localPointPosition = line.transform.InverseTransformPoint(pointPosition);
            Vector3[] positions = new Vector3[line.positionCount];
            line.GetPositions(positions);
            int closestPoint = 0;
            float closestDistance = Vector3.Distance(positions[0], localPointPosition);
            for (int i = 1; i < line.positionCount; i++)
            {
                float dist = Vector3.Distance(positions[i], localPointPosition);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPoint = i;
                }
            }

            int firstAnimFrame = anim.GetCurve(AnimatableProperty.RotationX).keys[0].frame + GlobalState.Animation.StartFrame - 1;
            return closestPoint + firstAnimFrame;
        }

    }
}
