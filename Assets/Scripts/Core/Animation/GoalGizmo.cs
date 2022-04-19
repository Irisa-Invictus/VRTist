using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{

    public class GoalGizmo : MonoBehaviour
    {

        public GameObject xRotation;
        public GameObject yRotation;
        public GameObject zRotation;

        public GameObject xPosition;
        public GameObject yPosition;
        public GameObject zPosition;

        public RigGoalController Controller;

        private bool isListening;

        public void Init(RigGoalController controller)
        {
            Controller = controller;

            Matrix4x4 targetMatrix = controller.transform.localToWorldMatrix;
            Maths.DecomposeMatrix(targetMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);
            transform.position = pos;
            transform.rotation = rot;
            transform.localScale = scale;

            if (!isListening)
            {
                GlobalState.ObjectMovingEvent.AddListener(ResetPosition);
                GlobalState.Animation.onFrameEvent.AddListener(ResetPosition);
                isListening = true;
            }
        }

        public void ResetPosition(GameObject gObject)
        {
            if (Controller == null) return;
            Matrix4x4 targetMatrix = Controller.transform.localToWorldMatrix;
            Maths.DecomposeMatrix(targetMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);
            transform.position = pos;
            transform.rotation = rot;
            transform.localScale = scale;
        }

        public void ResetPosition(int frame)
        {
            ResetPosition(Controller.gameObject);
        }

        public PoseManipulation.AcutatorAxis GetAcutatorAxis(GameObject Gizmo)
        {
            if (Gizmo == xPosition || Gizmo == xRotation) return PoseManipulation.AcutatorAxis.X;
            if (Gizmo == yPosition || Gizmo == yRotation) return PoseManipulation.AcutatorAxis.Y;
            if (Gizmo == zPosition || Gizmo == zRotation) return PoseManipulation.AcutatorAxis.Z;
            return PoseManipulation.AcutatorAxis.X;
        }

        public void ChangeGizmo(AnimationTool.GizmoTool newTool)
        {
            switch (newTool)
            {
                case AnimationTool.GizmoTool.Position:
                    SetRotationGizmo(false);
                    SetPositionGizmo(true);
                    break;
                case AnimationTool.GizmoTool.Rotation:
                    SetRotationGizmo(true);
                    SetPositionGizmo(false);
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