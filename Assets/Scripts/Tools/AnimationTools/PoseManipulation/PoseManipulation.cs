using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public abstract class PoseManipulation
    {

        public RigController ControllerRig;
        public Transform oTransform;

        public bool isGizmo = false;

        internal Vector3 targetPosition;
        internal Quaternion targetRotation;

        public List<GameObject> movedObjects = new List<GameObject>();
        internal List<Vector3> startPositions = new List<Vector3>();
        internal List<Quaternion> startRotations = new List<Quaternion>();
        internal List<Vector3> startScales = new List<Vector3>();
        internal List<Vector3> endPositions = new List<Vector3>();
        internal List<Quaternion> endRotations = new List<Quaternion>();
        internal List<Vector3> endScales = new List<Vector3>();

        internal Matrix4x4 InitialParentMatrixWorldToLocal;
        internal Matrix4x4 InitialParentMatrix;
        internal Matrix4x4 initialMouthMatrix;
        internal Matrix4x4 initialTransformMatrix;
        internal Matrix4x4 InitialTRS;

        internal List<Transform> fullHierarchy;
        internal int hierarchySize;

        public abstract void SetDestination(Transform mouthpiece);

        public abstract bool TrySolver();


        public CommandMoveObjects GetCommand()
        {
            return new CommandMoveObjects(movedObjects, startPositions, startRotations, startScales, endPositions, endRotations, endScales);
        }

        internal virtual void InitMatrices(Transform mouthpiece)
        {
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            InitialParentMatrix = oTransform.parent.localToWorldMatrix;
            InitialParentMatrixWorldToLocal = oTransform.parent.worldToLocalMatrix;
            InitialTRS = Matrix4x4.TRS(oTransform.localPosition, oTransform.localRotation, oTransform.localScale);
            initialTransformMatrix = oTransform.localToWorldMatrix;
        }

        internal virtual Transform InitHierarchy(DirectController Target, Transform origin)
        {
            fullHierarchy = new List<Transform>();
            if (origin == null) origin = Target.target.PathToRoot[Target.target.PathToRoot.Count - 2];
            fullHierarchy.Add(origin);
            int index = Target.target.PathToRoot.IndexOf(origin) + 1;

            if (index >= 0)
            {
                for (int i = index; i < Target.target.PathToRoot.Count; i++)
                {
                    fullHierarchy.Add(Target.target.PathToRoot[i]);
                }
            }
            if (Target.transform != origin) fullHierarchy.Add(Target.transform);
            hierarchySize = fullHierarchy.Count;
            return origin;
        }

    }
}
