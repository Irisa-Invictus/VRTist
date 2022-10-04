using UnityEngine;

namespace VRtist
{
    public static class TransformExtensions
    {
        public static Matrix4x4 GetLocalToParentMatrix(this Transform transform)
        {
            return transform.parent != null ? transform.parent.worldToLocalMatrix * transform.localToWorldMatrix : transform.localToWorldMatrix;
        }

        public static Vector3 GetScale(this Transform transform)
        {
            return transform.parent != null ? transform.localScale.ElementwiseDivide(GetScale(transform.parent)) : transform.localScale;
        }

        public static void SetLocalToParentMatrix(this Transform transform, Matrix4x4 matrix)
        {
            Maths.DecomposeMatrix(matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale);

            transform.localPosition = position;
            transform.localRotation = rotation;
            transform.localScale = scale;
        }

        public static void SetLocalToWorldMatrix(this Transform transform, Matrix4x4 matrix)
        {
            var localToParentMatrix = transform.parent != null ? transform.parent.worldToLocalMatrix * matrix : matrix;
            transform.SetLocalToParentMatrix(localToParentMatrix);
        }

        public static void SetScale(this Transform transform, Vector3 scale)
        {
            transform.localScale = transform.parent != null ? scale.ElementwiseMultiply(GetScale(transform.parent)) : scale;
        }
    }

    public static class Vector3Extensions
    {
        public static Vector3 ElementwiseDivide(this Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z);
        }

        public static Vector3 ElementwiseMultiply(this Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z);
        }
    }
}
