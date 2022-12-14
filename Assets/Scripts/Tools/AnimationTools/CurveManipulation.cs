/* MIT License
 *
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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{

    public class CurveManipulation
    {
        public GameObject Target;
        public int Frame;
        private int startFrame;
        private int endFrame;

        public struct ObjectData
        {
            public AnimationSet Animation;
            public Matrix4x4 InitialParentMatrixLocalToWorld;
            public Matrix4x4 InitialParentMatrixWorldToLocal;
            public Matrix4x4 InitialTRS;
            public float ScaleIndice;
            public TangentSimpleSolver Solver;

            public Vector3 lastPosition;
            public Vector3 lastRotation;
            public Quaternion lastQRotation;
            public Vector3 lastScale;
        }
        private ObjectData objectData;

        public struct HumanData
        {
            public JointController Controller;
            public List<JointController> Hierarchy;
            public List<AnimationSet> JointAnimations;
            public AnimationSet ObjectAnimation;
            public Matrix4x4 InitFrameMatrix;
            public TangentIKSolver Solver;
            public Transform rootTransform;
        }
        private HumanData humanData;


        private Matrix4x4 initialMouthMatrix;

        private double continuity;
        private Transform _mouthpiece;
        private AnimationTool.PoseEditMode _poseMode;

        private bool isRig;

        public CurveManipulation(GameObject target, int frame, int startSelection, int endSelection, Transform mouthpiece, AnimationTool.PoseEditMode poseMode)
        {
            Target = target;
            Frame = frame;
            _mouthpiece = mouthpiece;
            _poseMode = poseMode;
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;

            if (target.TryGetComponent(out JointController jointController))
            {
                isRig = true;
                if (poseMode == AnimationTool.PoseEditMode.FK) RigFKZone(jointController, frame, startSelection, endSelection, mouthpiece);
                else RigIKZone(jointController, frame, startSelection, endSelection, mouthpiece);
            }
            else
            {
                isRig = false;
                ObjectKeyFrameZone(target, frame, startSelection, endSelection, mouthpiece);
            }
        }

        public CurveManipulation(GameObject target, int frame, Transform mouthpiece, AnimationTool.PoseEditMode poseMode)
        {
            Target = target;
            Frame = frame;
            _mouthpiece = mouthpiece;
            _poseMode = poseMode;
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            if (target.TryGetComponent(out JointController jointController))
            {
                isRig = true;

                if (poseMode == AnimationTool.PoseEditMode.FK) RigFKPoint(jointController, frame, mouthpiece);
                else RigIKPoint(jointController, frame, mouthpiece);
            }
            else
            {
                isRig = false;
                ObjectKeyframePoint(target, frame, mouthpiece);
            }
        }

        private void ObjectKeyframePoint(GameObject target, int frame, Transform mouthpiece)
        {
            AnimationSet previousAnimation = GlobalState.Animation.GetObjectAnimation(target);
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            startFrame = previousAnimation.GetCurve(AnimatableProperty.RotationX).GetPreviousKey(frame).frame;
            endFrame = previousAnimation.GetCurve(AnimatableProperty.RotationX).GetNextKey(frame).frame;

            if (!previousAnimation.GetCurve(AnimatableProperty.PositionX).Evaluate(frame, out float posx)) posx = target.transform.localPosition.x;
            if (!previousAnimation.GetCurve(AnimatableProperty.PositionY).Evaluate(frame, out float posy)) posy = target.transform.localPosition.y;
            if (!previousAnimation.GetCurve(AnimatableProperty.PositionZ).Evaluate(frame, out float posz)) posz = target.transform.localPosition.z;
            if (!previousAnimation.GetCurve(AnimatableProperty.RotationX).Evaluate(frame, out float rotx)) rotx = target.transform.localEulerAngles.x;
            if (!previousAnimation.GetCurve(AnimatableProperty.RotationY).Evaluate(frame, out float roty)) roty = target.transform.localEulerAngles.y;
            if (!previousAnimation.GetCurve(AnimatableProperty.RotationZ).Evaluate(frame, out float rotz)) rotz = target.transform.localEulerAngles.z;
            if (!previousAnimation.GetCurve(AnimatableProperty.ScaleX).Evaluate(frame, out float scax)) scax = target.transform.localScale.x;
            if (!previousAnimation.GetCurve(AnimatableProperty.ScaleY).Evaluate(frame, out float scay)) scay = target.transform.localScale.y;
            if (!previousAnimation.GetCurve(AnimatableProperty.ScaleZ).Evaluate(frame, out float scaz)) scaz = target.transform.localScale.z;

            Vector3 initialPosition = new Vector3(posx, posy, posz);
            Quaternion initialRotation = Quaternion.Euler(rotx, roty, rotz);
            Vector3 initialScale = new Vector3(scax, scay, scaz);

            objectData = new ObjectData()
            {
                Animation = new AnimationSet(previousAnimation),
                InitialParentMatrixLocalToWorld = target.transform.parent.localToWorldMatrix,
                InitialParentMatrixWorldToLocal = target.transform.parent.worldToLocalMatrix,
                InitialTRS = Matrix4x4.TRS(initialPosition, initialRotation, initialScale),
                ScaleIndice = 1,
            };
        }
        private void ObjectKeyFrameZone(GameObject target, int frame, int firstFrame, int lastFrame, Transform mouthpiece)
        {
            AnimationSet previousAnimation = GlobalState.Animation.GetObjectAnimation(target);
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            startFrame = firstFrame;
            endFrame = lastFrame;

            if (!previousAnimation.GetCurve(AnimatableProperty.PositionX).Evaluate(frame, out float posx)) posx = target.transform.localPosition.x;
            if (!previousAnimation.GetCurve(AnimatableProperty.PositionY).Evaluate(frame, out float posy)) posy = target.transform.localPosition.y;
            if (!previousAnimation.GetCurve(AnimatableProperty.PositionZ).Evaluate(frame, out float posz)) posz = target.transform.localPosition.z;
            if (!previousAnimation.GetCurve(AnimatableProperty.RotationX).Evaluate(frame, out float rotx)) rotx = target.transform.localEulerAngles.x;
            if (!previousAnimation.GetCurve(AnimatableProperty.RotationY).Evaluate(frame, out float roty)) roty = target.transform.localEulerAngles.y;
            if (!previousAnimation.GetCurve(AnimatableProperty.RotationZ).Evaluate(frame, out float rotz)) rotz = target.transform.localEulerAngles.z;
            if (!previousAnimation.GetCurve(AnimatableProperty.ScaleX).Evaluate(frame, out float scax)) scax = target.transform.localScale.x;
            if (!previousAnimation.GetCurve(AnimatableProperty.ScaleY).Evaluate(frame, out float scay)) scay = target.transform.localScale.y;
            if (!previousAnimation.GetCurve(AnimatableProperty.ScaleZ).Evaluate(frame, out float scaz)) scaz = target.transform.localScale.z;

            Vector3 initialPosition = new Vector3(posx, posy, posz);
            Quaternion initialRotation = Quaternion.Euler(rotx, roty, rotz);
            Vector3 initialScale = new Vector3(scax, scay, scaz);

            objectData = new ObjectData()
            {
                Animation = new AnimationSet(previousAnimation),
                InitialParentMatrixLocalToWorld = target.transform.parent.localToWorldMatrix,
                InitialParentMatrixWorldToLocal = target.transform.parent.worldToLocalMatrix,
                InitialTRS = Matrix4x4.TRS(initialPosition, initialRotation, initialScale),
                ScaleIndice = 1,
            };
            AddFilteredKeyframeTangent(target, startFrame, endFrame,
               new AnimationKey(frame, posx),
               new AnimationKey(frame, posy),
               new AnimationKey(frame, posz),
               new AnimationKey(frame, rotx),
               new AnimationKey(frame, roty),
               new AnimationKey(frame, rotz),
               new AnimationKey(frame, scax),
               new AnimationKey(frame, scay),
               new AnimationKey(frame, scaz));

        }
        private void RigFKPoint(JointController joint, int frame, Transform mouthpiece)
        {
            List<JointController> controllerHierarchy = new List<JointController>();
            List<AnimationSet> animations = new List<AnimationSet>();

            startFrame = joint.Animation.GetCurve(AnimatableProperty.RotationX).GetPreviousKey(frame).frame;
            endFrame = joint.Animation.GetCurve(AnimatableProperty.RotationX).GetNextKey(frame).frame;

            if (joint.TryGetParentJoint(out JointController parent))
            {
                controllerHierarchy.Add(parent);
                animations.Add(new AnimationSet(parent.Animation));
            }
            Transform rTransform = controllerHierarchy.Count > 0 ? controllerHierarchy[0].Parent : joint.transform.parent;
            humanData = new HumanData()
            {
                Controller = joint,
                ObjectAnimation = new AnimationSet(joint.Animation),
                Hierarchy = controllerHierarchy,
                InitFrameMatrix = joint.MatrixAtFrame(frame),
                JointAnimations = animations,
                rootTransform = rTransform
            };
        }
        private void RigIKPoint(JointController joint, int frame, Transform mouthpiece)
        {
            List<JointController> controllerHierarchy = new List<JointController>();
            List<AnimationSet> animations = new List<AnimationSet>();

            startFrame = joint.Animation.GetCurve(AnimatableProperty.RotationX).GetPreviousKey(frame).frame;
            endFrame = joint.Animation.GetCurve(AnimatableProperty.RotationX).GetNextKey(frame).frame;

            if (joint.TryGetParentJoint(out JointController parent))
            {
                if (parent.TryGetParentJoint(out JointController grandPa))
                {
                    controllerHierarchy.Add(grandPa);
                    animations.Add(new AnimationSet(grandPa.Animation));
                }
                controllerHierarchy.Add(parent);
                animations.Add(new AnimationSet(parent.Animation));
            }

            Transform rTransform = controllerHierarchy.Count > 0 ? controllerHierarchy[0].Parent : joint.transform.parent;

            humanData = new HumanData()
            {
                Controller = joint,
                ObjectAnimation = new AnimationSet(joint.Animation),
                Hierarchy = controllerHierarchy,
                InitFrameMatrix = joint.MatrixAtFrame(frame),
                JointAnimations = animations,
                rootTransform = rTransform
            };
        }
        private void RigFKZone(JointController joint, int frame, int startFrame, int endFrame, Transform mouthpiece)
        {
            List<JointController> controllerHierarchy = new List<JointController>();
            List<AnimationSet> animations = new List<AnimationSet>();

            this.startFrame = startFrame;
            this.endFrame = endFrame;

            if (joint.TryGetParentJoint(out JointController parent))
            {
                controllerHierarchy.Add(parent);
                animations.Add(new AnimationSet(parent.Animation));
            }
            Transform rTransform = controllerHierarchy.Count > 0 ? controllerHierarchy[0].Parent : joint.transform.parent;
            humanData = new HumanData()
            {
                Controller = joint,
                ObjectAnimation = new AnimationSet(joint.Animation),
                Hierarchy = controllerHierarchy,
                InitFrameMatrix = joint.MatrixAtFrame(frame),
                JointAnimations = animations,
                rootTransform = rTransform
            };
        }
        private void RigIKZone(JointController joint, int frame, int startFrame, int endFrame, Transform mouthpiece)
        {
            List<JointController> controllerHierarchy = new List<JointController>();
            List<AnimationSet> animations = new List<AnimationSet>();

            this.startFrame = startFrame;
            this.endFrame = endFrame;

            if (joint.TryGetParentJoint(out JointController parent))
            {
                if (parent.TryGetParentJoint(out JointController grandPa))
                {
                    controllerHierarchy.Add(grandPa);
                    animations.Add(new AnimationSet(grandPa.Animation));
                }
                controllerHierarchy.Add(parent);
                animations.Add(new AnimationSet(parent.Animation));
            }

            Transform rTransform = controllerHierarchy.Count > 0 ? controllerHierarchy[0].Parent : joint.transform.parent;

            humanData = new HumanData()
            {
                Controller = joint,
                ObjectAnimation = new AnimationSet(joint.Animation),
                Hierarchy = controllerHierarchy,
                InitFrameMatrix = joint.MatrixAtFrame(frame),
                JointAnimations = animations,
                rootTransform = rTransform
            };
        }


        internal void DragCurve(Transform mouthpiece)
        {
            //TODO: add scale
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            if (isRig)
            {
                Matrix4x4 target = transformation * humanData.InitFrameMatrix;
                Matrix4x4 localTarget = humanData.rootTransform.TryGetComponent(out JointController parentJoint) ? parentJoint.MatrixAtFrame(Frame).inverse : humanData.rootTransform.localToWorldMatrix.inverse;
                //Debug.Log("object name " + humanData.ObjectAnimation.transform.name);
                //Debug.Log("object befor " + humanData.ObjectAnimation.GetCurve(AnimatableProperty.RotationX).keys[0].outTangent);

                Maths.DecomposeMatrix(localTarget * target, out Vector3 targetPos, out Quaternion targetRot, out Vector3 targetScale);


                TangentIKSolver solver = new TangentIKSolver(humanData.Controller, targetPos, targetRot, Frame, startFrame, endFrame, humanData.Hierarchy, localTarget);
                solver.Setup();
                humanData.Solver = solver;
                GlobalState.Animation.onChangeCurve.Invoke(humanData.Controller.gameObject, AnimatableProperty.PositionX);
            }
            else
            {
                Matrix4x4 transformed = objectData.InitialParentMatrixWorldToLocal *
                transformation * objectData.InitialParentMatrixLocalToWorld *
                objectData.InitialTRS;

                Maths.DecomposeMatrix(transformed, out objectData.lastPosition, out objectData.lastQRotation, out objectData.lastScale);
                objectData.lastRotation = objectData.lastQRotation.eulerAngles;
                objectData.lastScale *= 1;

                AnimationSet ObjectAnimation = GlobalState.Animation.GetObjectAnimation(Target);
                objectData.Solver = new TangentSimpleSolver(objectData.lastPosition, objectData.lastQRotation, ObjectAnimation, Frame, startFrame, endFrame, 1);
                objectData.Solver.TrySolver();
                GlobalState.Animation.onChangeCurve.Invoke(Target, AnimatableProperty.PositionX);
            }
        }

        internal void ReleaseCurve()
        {
            if (isRig)
            {
                List<GameObject> objectList = new List<GameObject>();
                List<Dictionary<AnimatableProperty, List<AnimationKey>>> keyframesLists = new List<Dictionary<AnimatableProperty, List<AnimationKey>>>();

                for (int iHier = 0; iHier < humanData.Hierarchy.Count; iHier++)
                {
                    if (null == humanData.Controller.AnimToRoot[iHier]) continue;
                    keyframesLists.Add(new Dictionary<AnimatableProperty, List<AnimationKey>>());
                    for (int pIndex = 0; pIndex < 3; pIndex++)
                    {
                        AnimatableProperty property = (AnimatableProperty)pIndex + 3;
                        List<AnimationKey> keys = new List<AnimationKey>();
                        int curveIndex = iHier * 3 + pIndex;
                        keys.Add(humanData.Solver.previousKeys[curveIndex]);
                        keys.Add(humanData.Solver.nextKeys[curveIndex]);
                        keyframesLists[keyframesLists.Count - 1].Add(property, keys);
                    }
                    GlobalState.Animation.SetObjectAnimations(humanData.Hierarchy[iHier].gameObject, humanData.JointAnimations[iHier]);
                    objectList.Add(humanData.Hierarchy[iHier].gameObject);
                }
                keyframesLists.Add(new Dictionary<AnimatableProperty, List<AnimationKey>>());
                for (int pIndex = 0; pIndex < 3; pIndex++)
                {
                    AnimatableProperty property = (AnimatableProperty)pIndex + 3;
                    List<AnimationKey> keys = new List<AnimationKey>();
                    int curveIndex = humanData.Hierarchy.Count * 3 + pIndex;
                    keys.Add(humanData.Solver.previousKeys[curveIndex]);
                    keys.Add(humanData.Solver.nextKeys[curveIndex]);
                    keyframesLists[keyframesLists.Count - 1].Add(property, keys);
                }
                GlobalState.Animation.SetObjectAnimations(humanData.Controller.gameObject, humanData.ObjectAnimation);
                objectList.Add(humanData.Controller.gameObject);

                GlobalState.Animation.onChangeCurve.Invoke(humanData.Controller.gameObject, AnimatableProperty.PositionX);

                CommandGroup group = new CommandGroup("Add Keyframe");

                new CommandAddKeyframes(humanData.Controller.RootController.gameObject, objectList, Frame, startFrame, endFrame, keyframesLists).Submit();

                group.Submit();
            }
            else
            {
                GlobalState.Animation.SetObjectAnimations(Target, objectData.Animation);
                CommandGroup group = new CommandGroup("Add Keyframe");
                Dictionary<AnimatableProperty, List<AnimationKey>> keyList = new Dictionary<AnimatableProperty, List<AnimationKey>>();
                for (int prop = 0; prop < 6; prop++)
                {
                    AnimatableProperty property = (AnimatableProperty)prop;
                    keyList.Add(property, new List<AnimationKey>());
                    int firstKey = objectData.Solver.previousKeyIndex;
                    int lastKey = objectData.Solver.nextKeyIndex;
                    for (int i = firstKey; i <= lastKey; i++)
                    {
                        keyList[property].Add(objectData.Solver.ObjectAnimation.GetCurve(property).keys[i]);
                    }
                }
                new CommandAddKeyframes(Target, Frame, startFrame, endFrame, keyList).Submit();
                group.Submit();
            }
        }


        private void AddFilteredKeyframeTangent(GameObject target, int firstFrame, int lastFrame, AnimationKey posX, AnimationKey posY, AnimationKey posZ, AnimationKey rotX, AnimationKey rotY, AnimationKey rotZ, AnimationKey scaleX, AnimationKey scaleY, AnimationKey scaleZ)
        {
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationX, rotX, firstFrame, lastFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationY, rotY, firstFrame, lastFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationZ, rotZ, firstFrame, lastFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleX, scaleX, firstFrame, lastFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleY, scaleY, firstFrame, lastFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleZ, scaleZ, firstFrame, lastFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionX, posX, firstFrame, lastFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionY, posY, firstFrame, lastFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionZ, posZ, firstFrame, lastFrame, false);
        }

    }
}