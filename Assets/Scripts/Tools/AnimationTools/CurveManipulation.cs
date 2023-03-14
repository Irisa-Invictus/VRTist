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
            /// <summary>
            /// Store copy of animation to apply single update on release (for undo/redo)
            /// </summary>
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
            /// <summary>
            /// Target being moved
            /// </summary>
            public JointController Controller;
            /// <summary>
            /// Parent hierarchy being affected until joint target
            /// </summary>
            public List<JointController> Hierarchy;
            /// <summary>
            /// Store copy of animations to apply single update on release (for undo/redo)
            /// </summary>
            public List<AnimationSet> JointAnimations;
            /// <summary>
            /// Target object animation
            /// </summary>
            public AnimationSet ObjectAnimation;
            /// <summary>
            /// Position of the joint in relation of first object in hierarchy
            /// </summary>
            public Matrix4x4 InitFrameMatrix;
            public TangentIKSolver Solver;
            /// <summary>
            /// Object's root
            /// </summary>
            public Transform rootTransform;
        }
        private HumanData humanData;

        /// <summary>
        /// Initial mouthpiece world to local
        /// </summary>
        private Matrix4x4 initialMouthMatrix;

        private bool isRig;

        public CurveManipulation(GameObject target, int frame, int startSelection, int endSelection, Transform mouthpiece, AnimationTool.PoseEditMode poseMode)
        {
            Target = target;
            Frame = frame;
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;

            //If target doesn't have a parent joint (root joint) then should behave like a non-rig object
            if (target.TryGetComponent(out JointController jointController) && jointController.TryGetParentJoint(out JointController parent))
            {
                isRig = true;
                if (poseMode == AnimationTool.PoseEditMode.FK) RigFKZone(jointController, parent, frame, startSelection, endSelection);
                else RigIKZone(jointController, parent, frame, startSelection, endSelection);
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
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            //If target doesn't have a parent joint (root joint) then should behave like a non-rig object
            if (target.TryGetComponent(out JointController jointController) && jointController.TryGetParentJoint(out JointController parent))
            {
                isRig = true;

                if (poseMode == AnimationTool.PoseEditMode.FK) RigFKPoint(jointController, parent, frame);
                else RigIKPoint(jointController, parent, frame);
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
        /// <summary>
        ///  Set start frame and end frame with previous and next keyframe from frame, then call RigFk
        /// </summary>
        private void RigFKPoint(JointController joint, JointController parent, int frame)
        {

            startFrame = joint.Animation.GetCurve(AnimatableProperty.RotationX).GetPreviousKey(frame).frame;
            endFrame = joint.Animation.GetCurve(AnimatableProperty.RotationX).GetNextKey(frame).frame;

            RigFK(joint, parent, frame);
        }

        /// <summary>
        /// Copy start frame and end frame, then call RigFk
        /// </summary>
        private void RigFKZone(JointController joint, JointController parent, int frame, int startFrame, int endFrame)
        {
            this.startFrame = startFrame;
            this.endFrame = endFrame;
            RigFK(joint, parent, frame);
        }

        /// <summary>
        /// Initialize HumanData for FK
        /// </summary>
        private void RigFK(JointController joint, JointController parent, int frame)
        {
            List<JointController> controllerHierarchy = new List<JointController>();
            List<AnimationSet> animations = new List<AnimationSet>();

            controllerHierarchy.Add(parent);
            animations.Add(new AnimationSet(parent.Animation));
            Matrix4x4 baseMatrix = Matrix4x4.identity;
            if (parent.TryGetParentJoint(out JointController baseController))
            {
                baseMatrix = Matrix4x4.Inverse(baseController.MatrixAtFrame(frame));
            }
            else
            {
                baseMatrix = parent.transform.parent.worldToLocalMatrix;
            }

            humanData = new HumanData()
            {
                Controller = joint,
                ObjectAnimation = new AnimationSet(joint.Animation),
                Hierarchy = controllerHierarchy,
                InitFrameMatrix = baseMatrix,
                JointAnimations = animations,
                rootTransform = joint.RootController.transform
            };
        }

        #region IKManip
        /// <summary>
        /// Set start frame and end frame with previous and next keyframe from frame, then call RigIK
        /// </summary>
        private void RigIKPoint(JointController joint, JointController parent, int frame)
        {
            startFrame = joint.Animation.GetCurve(AnimatableProperty.RotationX).GetPreviousKey(frame).frame;
            endFrame = joint.Animation.GetCurve(AnimatableProperty.RotationX).GetNextKey(frame).frame;
            RigIK(joint, parent, frame);
        }

        /// <summary>
        /// Copy start frame and end frame, then call RigIk
        /// </summary>
        private void RigIKZone(JointController joint, JointController parent, int frame, int startFrame, int endFrame)
        {
            this.startFrame = startFrame;
            this.endFrame = endFrame;
            RigIK(joint, parent, frame);
        }

        /// <summary>
        /// Initialize HumanData for IK
        /// </summary>
        private void RigIK(JointController joint, JointController parent, int frame)
        {
            List<JointController> controllerHierarchy = new List<JointController>();
            List<AnimationSet> animations = new List<AnimationSet>();
            if (parent.TryGetParentJoint(out JointController grandPa))
            {
                controllerHierarchy.Add(grandPa);
                animations.Add(new AnimationSet(grandPa.Animation));
            }
            controllerHierarchy.Add(parent);
            animations.Add(new AnimationSet(parent.Animation));

            Transform rTransform = controllerHierarchy.Count > 0 ? controllerHierarchy[0].Parent : joint.transform.parent;

            Matrix4x4 baseMatrix = Matrix4x4.identity;
            if (controllerHierarchy[0].TryGetParentJoint(out JointController baseController))
            {
                baseMatrix = Matrix4x4.Inverse(baseController.MatrixAtFrame(frame));
            }
            else
            {
                baseMatrix = parent.transform.parent.worldToLocalMatrix;
            }

            humanData = new HumanData()
            {
                Controller = joint,
                ObjectAnimation = new AnimationSet(joint.Animation),
                Hierarchy = controllerHierarchy,
                InitFrameMatrix = baseMatrix,
                JointAnimations = animations,
                rootTransform = rTransform
            };
        }
        #endregion

        /// <summary>
        /// Move the curve using tangent solvers to follow mouthpiece
        /// </summary>
        internal void DragCurve(Transform mouthpiece)
        {
            //TODO: add scale
            Matrix4x4 mouthpieceWorldPosition = Matrix4x4.TRS(mouthpiece.position, mouthpiece.rotation, mouthpiece.lossyScale);
            if (isRig)
            {
                Matrix4x4 target = humanData.InitFrameMatrix * mouthpieceWorldPosition;
                Maths.DecomposeMatrix(target, out Vector3 targetPos, out Quaternion targetRot, out Vector3 targetScale);

                TangentIKSolver solver = new TangentIKSolver(humanData.Controller, targetPos, targetRot, Frame, startFrame, endFrame, humanData.Hierarchy, target);
                solver.Setup();
                humanData.Solver = solver;
                GlobalState.Animation.onChangeCurve.Invoke(humanData.Controller.gameObject, AnimatableProperty.PositionX);
                humanData.Hierarchy.ForEach(x => GlobalState.Animation.onChangeCurve.Invoke(x.gameObject, AnimatableProperty.PositionX));
            }
            else
            {
                Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
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

        /// <summary>
        /// Stop moving the curve and update keyframes with current curve values 
        /// </summary>
        internal void ReleaseCurve()
        {
            if (isRig)
            {
                List<GameObject> objectList = new List<GameObject>();
                List<Dictionary<AnimatableProperty, List<AnimationKey>>> keyframesLists = new List<Dictionary<AnimatableProperty, List<AnimationKey>>>();

                for (int iHierarchy = 0; iHierarchy < humanData.Hierarchy.Count; iHierarchy++)
                {
                    if (null == humanData.Controller.AnimToRoot[iHierarchy]) continue;
                    keyframesLists.Add(new Dictionary<AnimatableProperty, List<AnimationKey>>());
                    for (int iProperty = 0; iProperty < 3; iProperty++)
                    {
                        AnimatableProperty property = (AnimatableProperty)iProperty + 3;
                        List<AnimationKey> keys = new List<AnimationKey>();
                        int curveIndex = iHierarchy * 3 + iProperty;
                        keys.Add(humanData.Solver.previousKeys[curveIndex]);
                        keys.Add(humanData.Solver.nextKeys[curveIndex]);
                        keyframesLists[keyframesLists.Count - 1].Add(property, keys);
                    }
                    GlobalState.Animation.SetObjectAnimations(humanData.Hierarchy[iHierarchy].gameObject, humanData.JointAnimations[iHierarchy]);
                    objectList.Add(humanData.Hierarchy[iHierarchy].gameObject);
                }
                keyframesLists.Add(new Dictionary<AnimatableProperty, List<AnimationKey>>());
                for (int iProperty = 0; iProperty < 3; iProperty++)
                {
                    AnimatableProperty property = (AnimatableProperty)iProperty + 3;
                    List<AnimationKey> keys = new List<AnimationKey>();
                    int curveIndex = humanData.Hierarchy.Count * 3 + iProperty;
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
                for (int iProperty = 0; iProperty < 6; iProperty++)
                {
                    AnimatableProperty property = (AnimatableProperty)iProperty;
                    keyList.Add(property, new List<AnimationKey>());
                    int firstKey = objectData.Solver.previousKeyIndex;
                    int lastKey = objectData.Solver.nextKeyIndex;
                    for (int iKey = firstKey; iKey <= lastKey; iKey++)
                    {
                        keyList[property].Add(objectData.Solver.ObjectAnimation.GetCurve(property).keys[iKey]);
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