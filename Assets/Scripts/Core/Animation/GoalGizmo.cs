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

        public void Initialize(RigObjectController controller)
        {
            Controller = controller;
            if (controller is DirectController)
            {
                transform.parent = controller.transform.parent;
                transform.localPosition = controller.transform.localPosition;
                transform.localRotation = controller.transform.localRotation;
                transform.localScale = controller.transform.localScale;
            }
            generatePositionCurves();
            generateRotationCurves();
            ChangeGizmo(GizmoTool.Rotation);
        }

        public void RemoveGizmo()
        {

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
            AnimationTool.Vector3Axis axis = GetAcutatorAxis(actuator.gameObject);
            SelectedAxis(axis);
            Controller.OnGrabGizmo(mouthpiece, this, CurrentGizmo, axis, false);
        }

        public void DragGizmo(Transform mouthpiece)
        {
            Controller.OnDragGizmo(mouthpiece);
            transform.localRotation = Controller.transform.localRotation;
        }

        public void ReleaseGizmo()
        {
            Controller.OnReleaseGizmo();
            UnSelectAxis();
        }
        public void SelectGizmo(Transform actuator)
        {

        }
        public void DeselectGizmo()
        {

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