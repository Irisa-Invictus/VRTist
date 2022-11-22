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
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    /// <summary>
    /// Display motion trails of animated objects.
    /// </summary>
    public class Anim3DCurveManager : MonoBehaviour
    {
        public Transform RightHanded;
        private bool displaySelectedCurves = true;
        public Transform curvesParent;
        public GameObject curvePrefab;

        private readonly float lineWidth = 0.001f;

        private bool isAnimationTool;
        private float currentCurveOffset;

        private readonly Dictionary<GameObject, GameObject> curves = new Dictionary<GameObject, GameObject>();

        private Texture2D selectionTexture;
        private Color objectColor = new Color(1, 0.7f, 0.3f);

        void Start()
        {
            ToolsUIManager.Instance.OnToolChangedEvent += OnToolChanged;
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            Selection.OnControllerSelected.AddListener(OnSelectController);
            Selection.OnControllerUnselected.AddListener(OnUnselectController);
            GlobalState.Animation.onFrameEvent.AddListener(UpdateOffset);
            GlobalState.Animation.onChangeCurve.AddListener(OnCurveChanged);
            GlobalState.Animation.onAddAnimation.AddListener(OnAnimationAdded);
            GlobalState.Animation.onRemoveAnimation.AddListener(OnAnimationRemoved);
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

        private void OnSelectController(RigObjectController controller)
        {
            if (GlobalState.Settings.Display3DCurves)
                UpdateFromSelection();
        }

        private void OnUnselectController(RigObjectController controller)
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
            }
            foreach (RigObjectController controller in Selection.SelectedControllers)
            {
                AddCurve(controller.gameObject);
            }
        }

        void OnCurveChanged(GameObject gObject, AnimatableProperty property)
        {
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
            DeleteCurve(gObject);
        }

        void OnToolChanged(object sender, ToolChangedArgs args)
        {
            UpdateFromSelection();
        }

        void ClearCurves()
        {
            foreach (GameObject curve in curves.Values)
                Destroy(curve);
            curves.Clear();
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

        void UpdateCurve(GameObject gObject)
        {
            AddCurve(gObject);
        }


        public delegate Matrix4x4 ParentTrs(int frame);

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

            List<ParentTrs> parentTrs = new List<ParentTrs>();
            Transform parent = gObject.transform.parent;
            while (parent != null && parent != RightHanded)
            {
                AnimationSet parentSet = GlobalState.Animation.GetObjectAnimation(parent.gameObject);
                if (parentSet != null) parentTrs.Insert(0, (frame) => parentSet.GetTRSMatrix(frame));
                else
                {
                    Matrix4x4 trs = Matrix4x4.TRS(parent.localPosition, parent.localRotation, parent.localScale);
                    parentTrs.Insert(0, (frame) => { return trs; });
                }
                parent = parent.parent;
            }

            List<Vector3> positions = new List<Vector3>();
            for (int i = frameStart; i <= frameEnd; i++)
            {
                positionX.Evaluate(i, out float x);
                positionY.Evaluate(i, out float y);
                positionZ.Evaluate(i, out float z);
                Vector3 position = new Vector3(x, y, z);
                Matrix4x4 matrix = RightHanded.localToWorldMatrix;
                parentTrs.ForEach(x =>
                {
                    matrix = matrix * x.Invoke(i);
                });
                //matrix = matrix * curvesParent.worldToLocalMatrix;
                position = matrix.MultiplyPoint(position);
                positions.Add(position);
            }

            int count = positions.Count;
            GameObject curve3D = curves.TryGetValue(gObject, out GameObject current) ? current : Instantiate(curvePrefab, curvesParent);

            LineRenderer line = curve3D.GetComponent<LineRenderer>();
            line.positionCount = count;
            for (int index = 0; index < count; index++)
            {
                line.SetPosition(index, curve3D.transform.InverseTransformPoint(positions[index]));
            }
            line.startWidth = lineWidth / GlobalState.WorldScale;
            line.endWidth = line.startWidth;

            MeshCollider collider = curve3D.GetComponent<MeshCollider>();
            Mesh lineMesh = new Mesh();
            line.BakeMesh(lineMesh);
            collider.sharedMesh = lineMesh;

            curves[gObject] = curve3D;
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

        }

        private void UpdateOffset(int frame)
        {

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
            if (curve == null) return;
            if (curve.TryGetComponent(out LineRenderer line))
            {
                GameObject target = GetObjectFromCurve(curve);
                if (target.TryGetComponent(out JointController joint))
                {
                    line.material.color = joint.color;
                }
                else
                {
                    line.material.color = objectColor;
                }
            }
        }

        /// <summary>
        /// Reset unselected zone color when curve is not hovered anymore
        /// </summary>
        internal void SelectionStopHover(GameObject curve, int start, int end)
        {
            Color outColor = objectColor;
            GameObject target = GetObjectFromCurve(curve);
            if (target.TryGetComponent(out JointController joint))
            {
                outColor = joint.color;
            }
            DrawLineZone(start, end, outColor);
        }

        public void StartSelection(GameObject curve)
        {
            LineRenderer line = curve.GetComponent<LineRenderer>();
            selectionTexture = (Texture2D)line.material.mainTexture;
            if (null == selectionTexture)
            {
                selectionTexture = new Texture2D(line.positionCount, 1, TextureFormat.RGBA32, false);
                line.material.mainTexture = selectionTexture;
            }
            line.material.color = Color.white;
        }

        public void DrawLineZone(int start, int end, Color outColor)
        {
            NativeArray<Color32> pixels = selectionTexture.GetRawTextureData<Color32>();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (i < start || i > end) pixels[i] = outColor;
                else pixels[i] = Color.black;
            }
            selectionTexture.Apply();
        }

        public void RemoveSelection(GameObject curve)
        {
            LineRenderer line = curve.GetComponent<LineRenderer>();
            if (GetObjectFromCurve(curve).TryGetComponent(out JointController joint))
            {
                line.material.color = joint.color;
            }
            else
            {
                line.material.color = objectColor;
            }
            line.material.mainTexture = null;
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
