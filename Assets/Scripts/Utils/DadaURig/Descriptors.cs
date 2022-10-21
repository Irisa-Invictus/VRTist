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

using System;
using UnityEngine;

// We want that the rig works in the editor as well, so can't encapsulate data because of serialization.

namespace Dada.URig.Descriptors
{
	[Serializable]
	public class Target
	{
		public float factor = 1f;
		public float offset = 0f;
		public Range range = new Range();

		public Target()
		{
		}

		public Target(float value)
		{
			range = new Range(value);
		}

		public float Transform(float value)
		{
			return range.Clamp((value * factor) + offset);
		}
	}

	[Serializable]
	public class BlendShapeTarget : Target
	{
		public SkinnedMeshRenderer skinnedMeshRenderer;
		public int blendShapeIndex;
	}

	[Serializable]
	public class ControllerAttributeTarget : Target
	{
		public string name;
	}

	[Serializable]
	public class AimUpConstraint
	{
		public Transform upTransform;
		public Matrix4x4 localToDrivenMatrix;
	}

	[Serializable]
	public abstract class CopyAttributeConstraint
	{
		public string attributeName;
	}

	[Serializable]
	public class CopyLocalAttributeToBlendShapeWeightConstraint : CopyAttributeConstraint
	{
		public BlendShapeTarget target;
	}

	[Serializable]
	public class CopyLocalAttributeToTransformAttributeConstraint : CopyAttributeConstraint
	{
		public ControllerAttributeTarget target;
	}

	[Serializable]
	public class CopyWorldAttributeToTransformAttributeConstraint : CopyAttributeConstraint
	{
		public ControllerAttributeTarget target;
	}

	[Serializable]
	public class OrientConstraint
	{
	}

	[Serializable]
	public class ParentConstraint
	{
		public Matrix4x4 localTargetMatrix;

		public Target xTranslationTarget;
		public Target yTranslationTarget;
		public Target zTranslationTarget;

		public Target xRotationTarget;
		public Target yRotationTarget;
		public Target zRotationTarget;

		public Target xScaleTarget;
		public Target yScaleTarget;
		public Target zScaleTarget;
	}

	[Serializable]
	public class Constraint
	{
		public Transform drivenObjectTransform;

		public enum Type
		{
			AimUpAim,
			AimUpRotate,
			CopyLocalAttributeToBlendShapeWeight,
			CopyLocalAttributeToTransformAttribute,
			CopyWorldAttributeToTransformAttribute,
			Orient,
			Parent,
		}

		public Type type;

		// One of those.
		public AimUpConstraint aimUpAim;
		public AimUpConstraint aimUpRotate;
		public CopyLocalAttributeToBlendShapeWeightConstraint copyLocalAttributeToBlendShapeWeight;
		public CopyLocalAttributeToTransformAttributeConstraint copyLocalAttributeToTransformAttribute;
		public CopyWorldAttributeToTransformAttributeConstraint copyWorldAttributeToTransformAttribute;
		public OrientConstraint orient;
		public ParentConstraint parent;
	}
}