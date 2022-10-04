using System;

namespace Dada.URig.JSONDescriptors
{
	[Serializable]
	public class ObjectAttribute
	{
		public string name;
		public Range range = new Range();
	}

	[Serializable]
	public class AimConstraintUpTarget
	{
		public string[] targetPath;
	}

	[Serializable]
	public class AimConstraintUp
	{
		public float[] vector;

		// One of these.
		public AimConstraintUpTarget aim;
		public AimConstraintUpTarget rotate;
	}

	[Serializable]
	public class ObjectAttributesBasedConstraint
	{
		public ObjectAttribute[] drivenAttributes;

	}

	[Serializable]
	public class AimConstraint
	{
		public float[] vector;
		// Nullable.
		public AimConstraintUp up;
	}

	[Serializable]
	public class BlendShapeAttribute
	{
		public string name;
		public Range range = new Range();
	}

	[Serializable]
	public class CursorTarget
	{
		public BlendShapeAttribute blendShape;
		public ObjectAttribute @object;
	}

	[Serializable]
	public class CursorConstraint
	{
		public string attributeName;
		public CursorTarget target;
	}

	[Serializable]
	public class CopyAttributesConstraint : ObjectAttributesBasedConstraint
	{
	}

	[Serializable]
	public class ParentConstraint : ObjectAttributesBasedConstraint
	{
	}

	[Serializable]
	public class Constraint
	{
		public string[] drivenObjectPath;

		public float weight = 1f;
		// Nullable.

		// One of these.
		public AimConstraint aim;
		public CopyAttributesConstraint copyAttributes;
		public CursorConstraint cursor;
		public ParentConstraint parent;
	}

	[Serializable]
	public class Controller
	{
		public string[] path;
		public ObjectAttribute[] attributes;

		public Constraint[] constraints;
	}

	[Serializable]
	public class Rig
	{
		public Controller[] controllers;
	}
}
