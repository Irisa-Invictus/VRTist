using System;
using UnityEngine;

// We want that the rig works in the editor as well, so can't encapsulate data because of serialization.

namespace Dada.URig.Descriptors
{
	[Serializable]
	public class ObjectAttribute
	{
		public string name;
		public Range range;
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
		public SkinnedMeshRenderer skinnedMeshRenderer;
		public int blendShapeIndex;
		public Range range = new Range();
	}

	[Serializable]
	public class CopyLocalAttributeToTransformAttributeConstraint : CopyAttributeConstraint
	{
		public ObjectAttribute target;
	}

	[Serializable]
	public class CopyWorldAttributeToTransformAttributeConstraint : CopyAttributeConstraint
	{
		public ObjectAttribute target;
	}

	[Serializable]
	public class OrientConstraint
	{
	}

	[Serializable]
	public class ParentConstraint
	{
		public Matrix4x4 localToTargetMatrix;

		public Range xTranslationRange;
		public Range yTranslationRange;
		public Range zTranslationRange;

		public Range xRotationRange;
		public Range yRotationRange;
		public Range zRotationRange;

		public Range xScaleRange;
		public Range yScaleRange;
		public Range zScaleRange;
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
