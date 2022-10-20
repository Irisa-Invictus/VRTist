/* MIT License
 *
 * © Dada ! Animation
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
