using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{

    public class TangentIKSolver
    {
        private JointController target;
        private List<AnimationSet> animationList;
        private int frame;
        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private int previousFrame;
        private int previousFrameIndex;
        private int nextFrame;
        private int nextFrameIndex;

        private Matrix4x4 rootParentMatrix;

        //only affects rotation x,y,z
        private int propertyCount = 3;
        private int valueCount;
        private int animationCount;

        struct State
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public int Frame;
        }
        private State currentState;
        private State targetState;

        private List<AnimationKey> previousKeys;
        private List<AnimationKey> nextKeys;
        private List<Vector3> hierarchyLocalPositions;
        private List<Vector3> hierarchyLocalScales;




        public TangentIKSolver(JointController joint, Vector3 targetPosition, Quaternion targetRotation, int frame, int start, int end, List<JointController> hierarchy)
        {
            target = joint;
            this.frame = frame;
            this.targetPosition = targetPosition;
            this.targetRotation = targetRotation;
            animationList = new List<AnimationSet>();
            hierarchy.ForEach(x => animationList.Add(x.Animation));
            animationList.Add(joint.Animation);
            animationCount = animationList.Count;
            rootParentMatrix = hierarchy[0].ParentMatrixAtFrame(frame);

            //property count * 2 keyframes * 4 (inTan.x inTan.y outTan.x outTan.y) * animation count
            valueCount = propertyCount * 2 * 4 * animationList.Count;

            previousKeys = new List<AnimationKey>();
            nextKeys = new List<AnimationKey>();
            hierarchyLocalPositions = new List<Vector3>();
            hierarchyLocalScales = new List<Vector3>();
        }

        public bool Setup()
        {
            Curve rotXCurve = target.Animation.GetCurve(AnimatableProperty.RotationX);
            rotXCurve.GetKeyIndex(previousFrame, out int previousKeyIndex);
            rotXCurve.GetKeyIndex(nextFrame, out int nextKeyIndex);
            if (frame < previousFrame) return false;
            if (frame > nextFrame) return false;

            animationList.ForEach(anim =>
            {
                previousKeys.Add(anim.GetCurve(AnimatableProperty.RotationX).keys[previousKeyIndex]);
                previousKeys.Add(anim.GetCurve(AnimatableProperty.RotationY).keys[previousKeyIndex]);
                previousKeys.Add(anim.GetCurve(AnimatableProperty.RotationZ).keys[previousKeyIndex]);
                previousKeys.Add(anim.GetCurve(AnimatableProperty.RotationX).keys[nextKeyIndex]);
                previousKeys.Add(anim.GetCurve(AnimatableProperty.RotationY).keys[nextKeyIndex]);
                previousKeys.Add(anim.GetCurve(AnimatableProperty.RotationZ).keys[nextKeyIndex]);

                Vector3 position = Vector3.zero;
                Curve posx = anim.GetCurve(AnimatableProperty.PositionX);
                Curve posy = anim.GetCurve(AnimatableProperty.PositionY);
                Curve posz = anim.GetCurve(AnimatableProperty.PositionZ);
                if (null != posx && null != posy && null != posz)
                {
                    if (!posx.Evaluate(frame, out float px) || !posy.Evaluate(frame, out float py) || !posz.Evaluate(frame, out float pz))
                    {
                        px = 0;
                        py = 0;
                        pz = 0;
                    }
                    position = new Vector3(px, py, pz);
                }
                hierarchyLocalPositions.Add(position);
                Vector3 scale = Vector3.one;
                Curve scalex = anim.GetCurve(AnimatableProperty.ScaleX);
                Curve scaley = anim.GetCurve(AnimatableProperty.ScaleY);
                Curve scalez = anim.GetCurve(AnimatableProperty.ScaleZ);
                if (null != scalex && null != scaley && null != scalez)
                {
                    if (scalex.Evaluate(frame, out float sx) && scaley.Evaluate(frame, out float sy) && scalez.Evaluate(frame, out float sz))
                    {
                        scale = new Vector3(sx, sy, sz);
                    }
                }
                hierarchyLocalScales.Add(scale);
            });

            double[] theta = GetAllTangents();
            double[,] Theta = Maths.ColumnArrayToArray(theta);
            State currentState = GetState(frame);
            State desiredState = new State()
            {
                Position = targetPosition,
                Rotation = targetRotation,
                Frame = frame
            };

            double[,] Js = dc_dtheta(frame);

            double[,] DT_D = new double[valueCount, valueCount];
            for (int i = 0; i < valueCount; i++)
            {
                DT_D[i, i] = 0d * 0d;
            }
            double[,] Delta_s_prime = new double[7, 1];
            for (int i = 0; i < 3; i++)
            {
                Delta_s_prime[i, 0] = targetState.Position[i] - currentState.Position[i];
            }
            if ((currentState.Rotation * Quaternion.Inverse(targetState.Rotation)).w < 0)
                targetState.Rotation = new Quaternion(-targetState.Rotation.x, -targetState.Rotation.y, -targetState.Rotation.z, -targetState.Rotation.w);
            for (int i = 0; i < 4; i++)
            {
                Delta_s_prime[i + 3, 0] = targetState.Rotation[i] - currentState.Rotation[i];
            }
            double[,] TT_T = new double[valueCount, valueCount];
            for (int j = 0; j < valueCount; j++)
            {
                TT_T[j, j] = 1d;
                if (j % 4 == 0 || j % 4 == 1)
                {
                    TT_T[j + 2, j] = -1d;
                }
                else
                {
                    TT_T[j - 2, j] = -1d;
                }
            }
            double wm = 100d;
            double wb = 1;
            double wd = 1d;

            double[,] Q_opt = Maths.Add(Maths.Add(Maths.Multiply(2d * wm, Maths.Multiply(Maths.Transpose(Js), Js)), Maths.Add(Maths.Multiply(2d * wd, DT_D), Maths.Multiply(2d * wb, TT_T))), Maths.Multiply((double)Mathf.Pow(10, -6), Maths.Identity(valueCount)));

            double[,] B_opt = Maths.Add(Maths.Multiply(-2d * wm, Maths.Multiply(Maths.Transpose(Js), Delta_s_prime)), Maths.Multiply(2d * wb, Maths.Multiply(TT_T, Theta)));
            double[] b_opt = Maths.ArrayToColumnArray(B_opt);

            return true;
        }

        private double[,] dc_dtheta(object currentFrame)
        {
            double[,] Js = new double[propertyCount, valueCount];

            for (int a = 0; a < animationCount; a++)
            {
                for (int i = 0; i < propertyCount; i++)
                {

                }
            }


            return Js;
        }

        private double[] GetAllTangents()
        {
            double[] theta = new double[valueCount];

            for (int i = 0; i < previousKeys.Count; i++)
            {
                theta[(i * 8) + 0] = previousKeys[i].inTangent.x;
                theta[(i * 8) + 1] = previousKeys[i].inTangent.y;
                theta[(i * 8) + 2] = previousKeys[i].outTangent.x;
                theta[(i * 8) + 3] = previousKeys[i].outTangent.y;
                theta[(i * 8) + 4] = nextKeys[i].inTangent.x;
                theta[(i * 8) + 5] = nextKeys[i].inTangent.y;
                theta[(i * 8) + 6] = nextKeys[i].outTangent.x;
                theta[(i * 8) + 7] = nextKeys[i].outTangent.y;
            }

            return theta;
        }

        private State GetState(int frame)
        {
            Matrix4x4 currentMatrix = FrameMatrix(frame, animationList);
            Maths.DecomposeMatrix(currentMatrix, out Vector3 position, out Quaternion rotation, out Vector3 scale);
            return new State()
            {
                Position = position,
                Rotation = rotation,
                Frame = frame
            };
        }

        public Matrix4x4 FrameMatrix(int frame, List<AnimationSet> animations)
        {
            Matrix4x4 trsMatrix = Matrix4x4.identity;

            for (int i = 0; i < animations.Count; i++)
            {
                trsMatrix = trsMatrix * GetBoneMatrix(animations[i], frame);
            }

            return trsMatrix;
        }

        private Matrix4x4 GetBoneMatrix(AnimationSet anim, int frame)
        {
            if (null == anim) return Matrix4x4.identity;
            Vector3 position = Vector3.zero;
            Curve posx = anim.GetCurve(AnimatableProperty.PositionX);
            Curve posy = anim.GetCurve(AnimatableProperty.PositionY);
            Curve posz = anim.GetCurve(AnimatableProperty.PositionZ);
            if (null != posx && null != posy && null != posz)
            {
                if (posx.Evaluate(frame, out float px) && posy.Evaluate(frame, out float py) && posz.Evaluate(frame, out float pz))
                {
                    position = new Vector3(px, py, pz);
                }
            }
            Quaternion rotation = Quaternion.identity;
            Curve rotx = anim.GetCurve(AnimatableProperty.RotationX);
            Curve roty = anim.GetCurve(AnimatableProperty.RotationY);
            Curve rotz = anim.GetCurve(AnimatableProperty.RotationZ);
            if (null != posx && null != roty && null != rotz)
            {
                if (rotx.Evaluate(frame, out float rx) && roty.Evaluate(frame, out float ry) && rotz.Evaluate(frame, out float rz))
                {
                    rotation = Quaternion.Euler(rx, ry, rz);
                }
            }
            Vector3 scale = Vector3.one;
            Curve scalex = anim.GetCurve(AnimatableProperty.ScaleX);
            Curve scaley = anim.GetCurve(AnimatableProperty.ScaleY);
            Curve scalez = anim.GetCurve(AnimatableProperty.ScaleZ);
            if (null != scalex && null != scaley && null != scalez)
            {
                if (scalex.Evaluate(frame, out float sx) && scaley.Evaluate(frame, out float sy) && scalez.Evaluate(frame, out float sz))
                {
                    scale = new Vector3(sx, sy, sz);
                }
            }
            return Matrix4x4.TRS(position, rotation, scale);
        }

    }

}