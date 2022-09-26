/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 * &
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

using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to add keyframes to all supported properties of an object.
    /// </summary>
    public class CommandAddKeyframes : CommandGroup
    {
        readonly GameObject gObject;
        public CommandAddKeyframes(GameObject obj, bool updateCurve = true) : base("Add Keyframes")
        {
            gObject = obj;
            Interpolation interpolation = GlobalState.Settings.interpolation;
            int frame = GlobalState.Animation.CurrentFrame;

            bool isHuman = obj.TryGetComponent<RigController>(out RigController skinController);

            if (ToolsManager.CurrentToolName() != "Animation" || !isHuman)
            {
                new CommandAddKeyframe(gObject, AnimatableProperty.PositionX, frame, gObject.transform.localPosition.x, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.PositionY, frame, gObject.transform.localPosition.y, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.PositionZ, frame, gObject.transform.localPosition.z, interpolation, updateCurve).Submit();

                // convert to ZYX euler
                Vector3 angles = ReduceAngles(gObject.transform.localRotation);
                new CommandAddKeyframe(gObject, AnimatableProperty.RotationX, frame, angles.x, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.RotationY, frame, angles.y, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.RotationZ, frame, angles.z, interpolation, updateCurve).Submit();
            }

            CameraController controller = gObject.GetComponent<CameraController>();
            LightController lcontroller = gObject.GetComponent<LightController>();

            if (null != controller)
            {
                new CommandAddKeyframe(gObject, AnimatableProperty.CameraFocal, frame, controller.focal, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.CameraFocus, frame, controller.Focus, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.CameraAperture, frame, controller.aperture, interpolation, updateCurve).Submit();
            }
            else if (null != lcontroller)
            {
                new CommandAddKeyframe(gObject, AnimatableProperty.Power, frame, lcontroller.Power, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorR, frame, lcontroller.Color.r, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorG, frame, lcontroller.Color.g, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorB, frame, lcontroller.Color.b, interpolation, updateCurve).Submit();
            }
            else
            {
                // Scale
                Vector3 scale = gObject.transform.localScale;
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleX, frame, scale.x, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleY, frame, scale.y, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleZ, frame, scale.z, interpolation, updateCurve).Submit();
            }

            if (isHuman && ToolsManager.CurrentToolName() == "Animation")
            {
                JointController[] joints = skinController.GetComponentsInChildren<JointController>();
                for (int i = 0; i < joints.Length; i++)
                {
                    Transform target = joints[i].transform;
                    Vector3 angles = ReduceAngles(target.transform.localRotation);
                    new CommandAddKeyframe(target.gameObject, AnimatableProperty.PositionX, frame, target.localPosition.x, interpolation, false).Submit();
                    new CommandAddKeyframe(target.gameObject, AnimatableProperty.PositionY, frame, target.localPosition.y, interpolation, false).Submit();
                    new CommandAddKeyframe(target.gameObject, AnimatableProperty.PositionZ, frame, target.localPosition.z, interpolation, false).Submit();
                    new CommandAddKeyframe(target.gameObject, AnimatableProperty.RotationX, frame, angles.x, interpolation, false).Submit();
                    new CommandAddKeyframe(target.gameObject, AnimatableProperty.RotationY, frame, angles.y, interpolation, false).Submit();
                    new CommandAddKeyframe(target.gameObject, AnimatableProperty.RotationZ, frame, angles.z, interpolation, false).Submit();
                    new CommandAddKeyframe(target.gameObject, AnimatableProperty.ScaleX, frame, target.localScale.x, interpolation, false).Submit();
                    new CommandAddKeyframe(target.gameObject, AnimatableProperty.ScaleY, frame, target.localScale.y, interpolation, false).Submit();
                    new CommandAddKeyframe(target.gameObject, AnimatableProperty.ScaleZ, frame, target.localScale.z, interpolation, false).Submit();
                }
            }
        }

        private void RecursiveAddKeyFrame(Transform target, int frame, Interpolation interpolation)
        {
            Vector3 angles = ReduceAngles(target.transform.localRotation);
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.PositionX, frame, target.localPosition.x, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.PositionY, frame, target.localPosition.y, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.PositionZ, frame, target.localPosition.z, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.RotationX, frame, angles.x, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.RotationY, frame, angles.y, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.RotationZ, frame, angles.z, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.ScaleX, frame, target.localScale.x, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.ScaleY, frame, target.localScale.y, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.ScaleZ, frame, target.localScale.z, interpolation, false).Submit();

            if (target.TryGetComponent(out RigController controller))
            {
                RecursiveAddKeyFrame(controller.transform, frame, interpolation);
                return;
            }
            Debug.Log(controller + target.name, target);
            foreach (Transform child in target)
            {
                if (child.tag == "Actuator") continue;
                RecursiveAddKeyFrame(child, frame, interpolation);
            }
        }

        public Vector3 ReduceAngles(Quaternion qrotation)
        {
            Vector3 rotation = qrotation.eulerAngles;
            rotation = new Vector3(Mathf.DeltaAngle(0, rotation.x), Mathf.DeltaAngle(0, rotation.y), Mathf.DeltaAngle(0, rotation.z));
            Vector3 addedRotation = new Vector3(-(rotation.x - 180), rotation.y + 180, rotation.z + 180);
            Vector3 minusRotation = new Vector3(-(rotation.x + 180), rotation.y - 180, rotation.z - 180);

            if (addedRotation.magnitude < rotation.magnitude)
                rotation = addedRotation;

            if (minusRotation.magnitude < rotation.magnitude)
                rotation = minusRotation;

            return rotation;
        }


        public CommandAddKeyframes(GameObject obj, List<GameObject> objs, int frame, int startFrame, int endFrame, List<Dictionary<AnimatableProperty, List<AnimationKey>>> newKeys)
        {
            gObject = obj;
            for (int l = 0; l < objs.Count; l++)
            {
                GameObject go = objs[l];
                for (int i = 0; i < 6; i++)
                {
                    AnimatableProperty property = (AnimatableProperty)i;
                    if (newKeys[l].ContainsKey(property))
                        new CommandAddKeyframeTangent(go, property, frame, startFrame, endFrame, newKeys[l][property]).Submit();
                }
            }
        }

        public CommandAddKeyframes(GameObject obj, int frame, int startFrame, int endFrame, Dictionary<AnimatableProperty, List<AnimationKey>> newKeys)
        {
            gObject = obj;
            for (int i = 0; i < 6; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                new CommandAddKeyframeTangent(gObject, property, frame, startFrame, endFrame, newKeys[property]).Submit();
            }
        }

        public override void Undo()
        {
            UnityEngine.Profiling.Profiler.BeginSample("CommandAddKeyframes");
            base.Undo();
            GlobalState.Animation.onChangeCurve.Invoke(gObject, AnimatableProperty.PositionX);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public override void Redo()
        {
            base.Redo();
            GlobalState.Animation.onChangeCurve.Invoke(gObject, AnimatableProperty.PositionX);
        }
        public override void Submit()
        {
            base.Submit();
            GlobalState.Animation.onChangeCurve.Invoke(gObject, AnimatableProperty.PositionX);
        }

    }
}
