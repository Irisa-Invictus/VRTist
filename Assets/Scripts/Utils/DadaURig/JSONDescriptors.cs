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

namespace Dada.URig.JSONDescriptors
{
	[Serializable]
	public class ControllerAttribute
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
	public class ObjectAttribute
	{
		public string name;
		public float factor = 1f;
		public float offset = 0f;
		public Range range = new Range();
	}

	[Serializable]
	public class ControllerAttributesBasedConstraint
	{
		public string[] drivenObjectPath;

		public ObjectAttribute[] drivenAttributes;

	}

	[Serializable]
	public class AimConstraint
	{
		public string[] drivenObjectPath;

		public float[] vector;
		// Nullable.
		public AimConstraintUp up;
	}

	[Serializable]
	public class CursorTargetBlendShape
	{
		public string drivenNodeName;

		public string drivenBlendShapeName;
		public float factor = 1f;
		public float offset = 0f;
		public Range range = new Range();
	}

	[Serializable]
	public class CursorTargetObject
	{
		public string[] drivenObjectPath;
		public string drivenAttributeName;
		public float factor = 1f;
		public float offset = 0f;
		public Range range = new Range();
	}

	[Serializable]
	public class CursorTarget
	{
		public CursorTargetBlendShape blendShape;
		public CursorTargetObject @object;
	}

	[Serializable]
	public class CursorConstraint
	{
		public string attributeName;
		public CursorTarget target;
	}

	[Serializable]
	public class CopyAttributesConstraint : ControllerAttributesBasedConstraint
	{
	}

	[Serializable]
	public class ParentConstraint : ControllerAttributesBasedConstraint
	{
	}

	[Serializable]
	public class Constraint
	{
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
		public ControllerAttribute[] attributes;

		public Constraint[] constraints;
	}

	[Serializable]
	public class Rig
	{
		public Controller[] controllers;
	}
}

