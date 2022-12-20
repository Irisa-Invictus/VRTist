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
namespace VRtist
{

    public class GoalGizmo : MonoBehaviour
    {

        public enum GizmoTool { Rotation, Position }
        private GizmoTool currentGizmo = GizmoTool.Rotation;
        public GizmoTool CurrentGizmo
        {
            get { return currentGizmo; }
            set
            {
                currentGizmo = value;
            }
        }

        public GameObject xRotation;
        public GameObject yRotation;
        public GameObject zRotation;

        public GameObject xPosition;
        public GameObject yPosition;
        public GameObject zPosition;

        public RigObjectController Controller;
        public List<RigObjectController> Selections;
        private bool freePosition = false;


        CommandGroup cmdGroup;

        public void OnEnable()
        {
            GlobalState.ObjectMovingEvent.AddListener(UpdateGizmo);
        }

        private void UpdateGizmo(GameObject obj)
        {
            if (Controller != null && Controller.gameObject == obj)
            {
                UpdateGizmo();
            }
        }

        public void OnDisable()
        {
            GlobalState.ObjectMovingEvent.RemoveListener(UpdateGizmo);
        }

        public void Initialize(RigObjectController controller)
        {
            Controller = controller;
            transform.parent = controller.transform.parent;
            transform.localPosition = controller.transform.localPosition;
            transform.localRotation = controller.transform.localRotation;
            transform.localScale = controller.transform.localScale * 0.01f;
            generatePositionCurves();
            generateRotationCurves();
            ChangeGizmo(GizmoTool.Rotation);
        }

        public void FixedInitialize(RigObjectController controller)
        {
            Controller = controller;
            freePosition = true;
            transform.rotation = controller.transform.rotation;
            generatePositionCurves();
            generateRotationCurves();
            ChangeGizmo(GizmoTool.Rotation);
        }

        public void AddSelected(RigObjectController controller)
        {
            if (Controller != null) Selections.Add(Controller);
            FixedInitialize(controller);
        }

        public void RemoveSelected(RigObjectController controller)
        {
            if (Controller = controller)
            {
                if (Selections.Count > 0)
                {
                    FixedInitialize(Selections[0]);
                    Selections.RemoveAt(0);
                }
                else Controller = null;
            }
            else if (Selections.Contains(controller))
            {
                Selections.Remove(controller);
            }

            if (Selections.Count == 0) gameObject.SetActive(false);
        }

        public void NextGizmo()
        {
            if (CurrentGizmo == GizmoTool.Position) ChangeGizmo(GizmoTool.Rotation);
            else ChangeGizmo(GizmoTool.Position);
        }

        public void UpdateGizmo()
        {
            transform.rotation = Controller.transform.rotation;
            if (!freePosition) transform.localPosition = Controller.transform.localPosition;
        }

        public void RemoveGizmo()
        {
            Selections.Clear();
        }

        public void StartHover(GameObject actuator)
        {
            if (actuator.TryGetComponent(out LineRenderer line))
            {
                line.startWidth = 3f;
                line.endWidth = 3f;
            }
        }
        public void EndHover(GameObject actuator)
        {
            if (actuator.TryGetComponent(out LineRenderer line))
            {
                line.startWidth = 1f;
                line.endWidth = 1f;
            }
        }

        public void GrabGizmo(Transform mouthpiece, Transform actuator)
        {
            cmdGroup = new CommandGroup("Add keyframe");
            AnimationTool.Vector3Axis axis = GetAcutatorAxis(actuator.gameObject);
            SelectedAxis(axis);
            if (currentGizmo == GizmoTool.Rotation)
            {
                Controller.OnGrabGizmo(mouthpiece, this, CurrentGizmo, axis, false);
                Selections.ForEach(x => x.OnGrabGizmo(mouthpiece, this, CurrentGizmo, axis, false));
            }
            else
            {
                Controller.OnGrabGizmo(mouthpiece, this, currentGizmo, axis, true);
                Selections.ForEach(x => x.OnGrabGizmo(mouthpiece, this, CurrentGizmo, axis, true));
            }
        }

        public void DragGizmo(Transform mouthpiece)
        {
            Controller.OnDragGizmo(mouthpiece);
            Selections.ForEach(x => x.OnDragGizmo(mouthpiece));
            transform.rotation = Controller.transform.rotation;
            if (!freePosition) transform.localPosition = Controller.transform.localPosition;
        }

        public void ReleaseGizmo()
        {
            Controller.OnReleaseGizmo();
            Selections.ForEach(x => x.OnReleaseGizmo());
            cmdGroup.Submit();
            cmdGroup = null;
            UnSelectAxis();
        }

        public AnimationTool.Vector3Axis GetAcutatorAxis(GameObject Gizmo)
        {
            if (Gizmo == xPosition || Gizmo == xRotation) return AnimationTool.Vector3Axis.X;
            if (Gizmo == yPosition || Gizmo == yRotation) return AnimationTool.Vector3Axis.Y;
            if (Gizmo == zPosition || Gizmo == zRotation) return AnimationTool.Vector3Axis.Z;
            return AnimationTool.Vector3Axis.X;
        }

        public void SelectedAxis(AnimationTool.Vector3Axis axis)
        {
            bool isRotationMode = CurrentGizmo == GizmoTool.Rotation;
            if (axis == AnimationTool.Vector3Axis.None)
            {
                xRotation.SetActive(isRotationMode);
                yRotation.SetActive(isRotationMode);
                zRotation.SetActive(isRotationMode);
                xPosition.SetActive(!isRotationMode);
                yPosition.SetActive(!isRotationMode);
                zPosition.SetActive(!isRotationMode);
                return;
            }
            if (isRotationMode && axis != AnimationTool.Vector3Axis.X) xRotation.SetActive(false);
            if (isRotationMode && axis != AnimationTool.Vector3Axis.Y) yRotation.SetActive(false);
            if (isRotationMode && axis != AnimationTool.Vector3Axis.Z) zRotation.SetActive(false);
            if (!isRotationMode && axis != AnimationTool.Vector3Axis.X) xPosition.SetActive(false);
            if (!isRotationMode && axis != AnimationTool.Vector3Axis.Y) yPosition.SetActive(false);
            if (!isRotationMode && axis != AnimationTool.Vector3Axis.Z) zPosition.SetActive(false);
        }

        public void UnSelectAxis()
        {
            if (currentGizmo == GizmoTool.Rotation)
            {
                xRotation.SetActive(true);
                yRotation.SetActive(true);
                zRotation.SetActive(true);
            }
            else
            {
                xPosition.SetActive(true);
                yPosition.SetActive(true);
                zPosition.SetActive(true);
            }
        }

        public void ChangeGizmo(GizmoTool newTool)
        {
            switch (newTool)
            {
                case GizmoTool.Position:
                    SetRotationGizmo(false);
                    SetPositionGizmo(true);
                    currentGizmo = GizmoTool.Position;
                    break;
                case GizmoTool.Rotation:
                    SetRotationGizmo(true);
                    SetPositionGizmo(false);
                    currentGizmo = GizmoTool.Rotation;
                    break;
            }
        }

        private void SetRotationGizmo(bool state)
        {
            xRotation.SetActive(state);
            yRotation.SetActive(state);
            zRotation.SetActive(state);
        }
        private void SetPositionGizmo(bool state)
        {
            xPosition.SetActive(state);
            yPosition.SetActive(state);
            zPosition.SetActive(state);
        }


        /// <summary>
        /// Editor call to create the gizmos curves.
        /// </summary>
        [ContextMenu("generate Rotation")]
        public void generateRotationCurves()
        {
            List<float> cos = new List<float>();
            List<float> sin = new List<float>();

            for (float i = 0; i < 2 * Mathf.PI; i += 0.1f)
            {
                cos.Add(Mathf.Cos(i) * 10);
                sin.Add(Mathf.Sin(i) * 10);
            }

            List<Vector3> curvesX = new List<Vector3>();
            List<Vector3> curvesY = new List<Vector3>();
            List<Vector3> curvesZ = new List<Vector3>();

            for (int j = 0; j < cos.Count; j++)
            {
                curvesX.Add(new Vector3(0, sin[j], cos[j]));
                curvesY.Add(new Vector3(sin[j], 0, cos[j]));
                curvesZ.Add(new Vector3(sin[j], cos[j], 0));
            }

            LineRenderer lineX = xRotation.GetComponent<LineRenderer>();
            LineRenderer lineY = yRotation.GetComponent<LineRenderer>();
            LineRenderer lineZ = zRotation.GetComponent<LineRenderer>();

            lineX.SetPositions(curvesX.ToArray());
            lineX.positionCount = curvesX.Count;

            lineY.SetPositions(curvesY.ToArray());
            lineY.positionCount = curvesY.Count;

            lineZ.SetPositions(curvesZ.ToArray());
            lineZ.positionCount = curvesZ.Count;

            Mesh meshX = new Mesh();
            lineX.BakeMesh(meshX);

            Mesh meshY = new Mesh();
            lineY.BakeMesh(meshY);

            Mesh meshZ = new Mesh();
            lineZ.BakeMesh(meshZ);

            lineX.GetComponent<MeshCollider>().sharedMesh = meshX;
            lineY.GetComponent<MeshCollider>().sharedMesh = meshY;
            lineZ.GetComponent<MeshCollider>().sharedMesh = meshZ;
        }

        [ContextMenu("generate position")]
        public void generatePositionCurves()
        {
            LineRenderer posX = xPosition.GetComponent<LineRenderer>();
            LineRenderer posY = yPosition.GetComponent<LineRenderer>();
            LineRenderer posZ = zPosition.GetComponent<LineRenderer>();

            posX.SetPositions(new Vector3[] { new Vector3(0, 0, 0), new Vector3(10, 0, 0) });
            posX.positionCount = 2;
            posY.SetPositions(new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, 10, 0) });
            posY.positionCount = 2;
            posZ.SetPositions(new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, -0.01f, 10) });
            posZ.positionCount = 2;

            Mesh meshX = new Mesh();
            posX.BakeMesh(meshX);
            Mesh meshY = new Mesh();
            posY.BakeMesh(meshY);
            Mesh meshZ = new Mesh();
            posZ.BakeMesh(meshZ);

            posX.GetComponent<MeshCollider>().sharedMesh = meshX;
            posY.GetComponent<MeshCollider>().sharedMesh = meshY;
            posZ.GetComponent<MeshCollider>().sharedMesh = meshZ;
        }

    }

}