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

        public List<AnimationKey> previousKeys;
        public List<AnimationKey> nextKeys;



        double[]
           lowerBound,
           upperBound;

        public TangentIKSolver(JointController joint, Vector3 targetPosition, Quaternion targetRotation, int frame, int start, int end, List<JointController> hierarchy, Matrix4x4 parentMatrix)
        {
            target = joint;
            this.frame = frame;
            this.targetPosition = targetPosition;
            this.targetRotation = targetRotation;
            animationList = new List<AnimationSet>();
            hierarchy.ForEach(x => animationList.Add(x.Animation));
            animationList.Add(joint.Animation);
            animationCount = animationList.Count;
            rootParentMatrix = parentMatrix;

            //property count * 2 keyframes * 4 (inTan.x inTan.y outTan.x outTan.y) * animation count
            valueCount = propertyCount * 2 * 4 * animationList.Count;
            previousFrame = start;
            nextFrame = end;

            previousKeys = new List<AnimationKey>();
            nextKeys = new List<AnimationKey>();
        }

        private GameObject CreateDebugSphere(Color color, Transform root)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = root;
            sphere.transform.localScale = Vector3.one * 0.25f;
            sphere.GetComponent<MeshRenderer>().material.color = color;
            return sphere;
        }

        public bool Setup()
        {
            Curve rotXCurve = target.Animation.GetCurve(AnimatableProperty.RotationX);
            animationList.ForEach(x =>
            {
                Curve rotXCurve = x.GetCurve(AnimatableProperty.RotationX);
                if (!rotXCurve.HasKeyAt(previousFrame) || !rotXCurve.HasKeyAt(nextFrame)) rotXCurve.AddTangentKey(previousFrame, nextFrame);
                Curve rotYCurve = x.GetCurve(AnimatableProperty.RotationY);
                if (!rotYCurve.HasKeyAt(previousFrame) || !rotYCurve.HasKeyAt(nextFrame)) rotYCurve.AddTangentKey(previousFrame, nextFrame);
                Curve rotZCurve = x.GetCurve(AnimatableProperty.RotationZ);
                if (!rotZCurve.HasKeyAt(previousFrame) || !rotZCurve.HasKeyAt(nextFrame)) rotZCurve.AddTangentKey(previousFrame, nextFrame);
            });

            rotXCurve.GetKeyIndex(previousFrame, out int previousKeyIndex);
            rotXCurve.GetKeyIndex(nextFrame, out int nextKeyIndex);
            if (frame < previousFrame) return false;
            if (frame > nextFrame) return false;

            animationList.ForEach(anim =>
            {
                previousKeys.Add(anim.GetCurve(AnimatableProperty.RotationX).keys[previousKeyIndex]);
                previousKeys.Add(anim.GetCurve(AnimatableProperty.RotationY).keys[previousKeyIndex]);
                previousKeys.Add(anim.GetCurve(AnimatableProperty.RotationZ).keys[previousKeyIndex]);
                nextKeys.Add(anim.GetCurve(AnimatableProperty.RotationX).keys[nextKeyIndex]);
                nextKeys.Add(anim.GetCurve(AnimatableProperty.RotationY).keys[nextKeyIndex]);
                nextKeys.Add(anim.GetCurve(AnimatableProperty.RotationZ).keys[nextKeyIndex]);
            });

            double[] theta = GetAllTangents();
            double[,] Theta = Maths.ColumnArrayToArray(theta);
            currentState = GetState(frame);
            targetState = new State()
            {
                Position = targetPosition,
                Rotation = targetRotation,
                Frame = frame
            };

            double[,] Js = dc_dtheta(frame);

            double[,] DT_D = new double[valueCount, valueCount];
            for (int i = 0; i < valueCount; i++)
            {
                DT_D[i, i] = 1;
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
            //Target strength
            double wm = 3d;
            //Tangent stiffness
            double wb = 1;
            //Continuity 
            double wd = 1d;

            double[,] Q_opt = Maths.Add(Maths.Add(Maths.Multiply(2d * wm, Maths.Multiply(Maths.Transpose(Js), Js)), Maths.Add(Maths.Multiply(2d * wd, DT_D), Maths.Multiply(2d * wb, TT_T))), Maths.Multiply((double)Mathf.Pow(10, -6), Maths.Identity(valueCount)));

            double[,] B_opt = Maths.Add(Maths.Multiply(-2d * wm, Maths.Multiply(Maths.Transpose(Js), Delta_s_prime)), Maths.Multiply(2d * wb, Maths.Multiply(TT_T, Theta)));
            double[] b_opt = Maths.ArrayToColumnArray(B_opt);

            alglib.minqpstate state_opt;
            alglib.minqpreport rep;
            double[] delta_theta_0 = new double[valueCount];
            double[] delta_theta;


            alglib.minqpcreate(valueCount, out state_opt);
            alglib.minqpsetquadraticterm(state_opt, Q_opt);
            alglib.minqpsetlinearterm(state_opt, b_opt);
            alglib.minqpsetstartingpoint(state_opt, delta_theta_0);
            alglib.minqpsetbc(state_opt, lowerBound, upperBound);

            //alglib.minqpsetscale(state_opt, scale);

            alglib.minqpsetalgobleic(state_opt, 0.0, 0.0, 0.0, 0);
            alglib.minqpoptimize(state_opt);
            alglib.minqpresults(state_opt, out delta_theta, out rep);

            double[] new_theta = new double[valueCount];
            for (int i = 0; i < valueCount; i++)
            {
                new_theta[i] = delta_theta[i] + theta[i];
            }
            for (int a = 0; a < animationCount; a++)
            {
                AnimationSet animation = animationList[a];
                for (int p = 0; p < 3; p++)
                {
                    AnimatableProperty property = (AnimatableProperty)p + 3;
                    Curve curve = animation.GetCurve(property);
                    int curveIndex = (a * 3) + p;
                    int propIndex = a * 24 + p * 8;

                    Vector2 PrevInTangent = new Vector2((float)new_theta[propIndex + 0], (float)(new_theta[propIndex + 1]));
                    Vector2 PrevOutTangent = new Vector2((float)new_theta[propIndex + 2], (float)(new_theta[propIndex + 3]));
                    Vector2 nextInTangent = new Vector2((float)new_theta[propIndex + 4], (float)(new_theta[propIndex + 5]));
                    Vector2 nextOutTangent = new Vector2((float)new_theta[propIndex + 6], (float)(new_theta[propIndex + 7]));
                    ModifyTangents(curve, previousKeyIndex, PrevInTangent, PrevOutTangent);
                    ModifyTangents(curve, nextKeyIndex, nextInTangent, nextOutTangent);
                }
            }

            return true;
        }

        private double[,] dc_dtheta(object currentFrame)
        {
            //pos x,y,z rot x,y,z,w
            double[,] Js = new double[7, valueCount];
            float dtheta = 1f;

            for (int a = 0; a < animationCount; a++)
            {
                //modified property: rot.x, rot.y, rot.z
                for (int i = 0; i < 3; i++)
                {
                    //modified value, k-1 out.x k-1 out.y k+1 in.x k+1 in.y
                    for (int prop = 0; prop < 4; prop++)
                    {
                        Matrix4x4 matrice = Matrix4x4.identity;

                        for (int m = 0; m < animationCount; m++)
                        {
                            if (!animationList[m].GetCurve(AnimatableProperty.PositionX).Evaluate(frame, out float posx)) posx = 0;
                            if (!animationList[m].GetCurve(AnimatableProperty.PositionY).Evaluate(frame, out float posy)) posy = 0;
                            if (!animationList[m].GetCurve(AnimatableProperty.PositionZ).Evaluate(frame, out float posz)) posz = 0;

                            Vector3 position = new Vector3(posx, posy, posz);

                            if (!animationList[m].GetCurve(AnimatableProperty.ScaleX).Evaluate(frame, out float scalx)) scalx = 1;
                            if (!animationList[m].GetCurve(AnimatableProperty.ScaleY).Evaluate(frame, out float scaly)) scaly = 1;
                            if (!animationList[m].GetCurve(AnimatableProperty.ScaleZ).Evaluate(frame, out float scalz)) scalz = 1;
                            Vector3 scale = new Vector3(scalx, scaly, scalx);

                            if (!animationList[m].GetCurve(AnimatableProperty.RotationX).Evaluate(frame, out float rotx)) rotx = 0;
                            if (!animationList[m].GetCurve(AnimatableProperty.RotationY).Evaluate(frame, out float roty)) roty = 0;
                            if (!animationList[m].GetCurve(AnimatableProperty.RotationZ).Evaluate(frame, out float rotz)) rotz = 0;
                            Vector3 eulerRotation = new Vector3(rotx, roty, rotz);

                            if (m == a)
                            {
                                switch (prop)
                                {
                                    case 0:
                                        Vector2 A1 = new Vector2(previousKeys[(m * 3) + i].frame, previousKeys[(m * 3) + i].value);
                                        Vector2 B1 = A1 + (previousKeys[(m * 3) + i].outTangent + new Vector2(dtheta, 0));
                                        Vector2 D1 = new Vector2(nextKeys[(m * 3) + i].frame, nextKeys[(m * 3) + i].value);
                                        Vector2 C1 = D1 - nextKeys[(m * 3) + i].inTangent;
                                        eulerRotation[i] = Bezier.EvaluateBezier(A1, B1, C1, D1, frame);
                                        break;
                                    case 1:
                                        Vector2 A2 = new Vector2(previousKeys[(m * 3) + i].frame, previousKeys[(m * 3) + i].value);
                                        Vector2 B2 = A2 + (previousKeys[(m * 3) + i].outTangent + new Vector2(0, dtheta));
                                        Vector2 D2 = new Vector2(nextKeys[(m * 3) + i].frame, nextKeys[(m * 3) + i].value);
                                        Vector2 C2 = D2 - nextKeys[(m * 3) + i].inTangent;
                                        eulerRotation[i] = Bezier.EvaluateBezier(A2, B2, C2, D2, frame);
                                        break;
                                    case 2:
                                        Vector2 A3 = new Vector2(previousKeys[(m * 3) + i].frame, previousKeys[(m * 3) + i].value);
                                        Vector2 B3 = A3 + previousKeys[(m * 3) + i].outTangent;
                                        Vector2 D3 = new Vector2(nextKeys[(m * 3) + i].frame, nextKeys[(m * 3) + i].value);
                                        Vector2 C3 = D3 - (nextKeys[(m * 3) + i].inTangent + new Vector2(dtheta, 0));
                                        eulerRotation[i] = Bezier.EvaluateBezier(A3, B3, C3, D3, frame);
                                        break;
                                    case 3:
                                        Vector2 A4 = new Vector2(previousKeys[(m * 3) + i].frame, previousKeys[(m * 3) + i].value);
                                        Vector2 B4 = A4 + previousKeys[(m * 3) + i].outTangent;
                                        Vector2 D4 = new Vector2(nextKeys[(m * 3) + i].frame, nextKeys[(m * 3) + i].value);
                                        Vector2 C4 = D4 - (nextKeys[(m * 3) + i].inTangent + new Vector2(0, dtheta));
                                        eulerRotation[i] = Bezier.EvaluateBezier(A4, B4, C4, D4, frame);
                                        break;
                                }
                            }
                            else
                            {
                                if (!animationList[m].GetCurve(AnimatableProperty.RotationX).Evaluate(frame, out eulerRotation.x)) eulerRotation.x = 0;
                                if (!animationList[m].GetCurve(AnimatableProperty.RotationY).Evaluate(frame, out eulerRotation.y)) eulerRotation.y = 0;
                                if (!animationList[m].GetCurve(AnimatableProperty.RotationZ).Evaluate(frame, out eulerRotation.z)) eulerRotation.z = 0;
                            }

                            matrice = matrice * Matrix4x4.TRS(position, Quaternion.Euler(eulerRotation), scale);
                        }
                        Maths.DecomposeMatrix(matrice, out Vector3 resultPosition, out Quaternion resultRotation, out Vector3 resultScale);
                        //object position.x affected by delta 
                        Js[0, (a * 24) + (i * 8) + (prop + 2)] = (resultPosition.x - currentState.Position.x) / dtheta;
                        Js[1, (a * 24) + (i * 8) + (prop + 2)] = (resultPosition.y - currentState.Position.y) / dtheta;
                        Js[2, (a * 24) + (i * 8) + (prop + 2)] = (resultPosition.z - currentState.Position.z) / dtheta;
                        Js[3, (a * 24) + (i * 8) + (prop + 2)] = (resultRotation.x - currentState.Rotation.x) / dtheta;
                        Js[4, (a * 24) + (i * 8) + (prop + 2)] = (resultRotation.y - currentState.Rotation.y) / dtheta;
                        Js[5, (a * 24) + (i * 8) + (prop + 2)] = (resultRotation.z - currentState.Rotation.z) / dtheta;
                        Js[6, (a * 24) + (i * 8) + (prop + 2)] = (resultRotation.w - currentState.Rotation.w) / dtheta;
                    }

                }
            }


            return Js;
        }

        private double[] GetAllTangents()
        {
            double[] theta = new double[valueCount];
            lowerBound = new double[valueCount];
            upperBound = new double[valueCount];

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

                FillLowerBounds(i, previousKeys[i], nextKeys[i], -90, 90);
                FillUpperBounds(i, previousKeys[i], nextKeys[i], -90, 90);

            }

            return theta;
        }

        private void FillLowerBounds(int curveIndex, AnimationKey previous1Key, AnimationKey next1Key, float Min, float Max)
        {
            //k- in.x
            //lowerBound[curveIndex * 8 + 0] = 0 - previous1Key.inTangent.x;
            ////k- in.y
            //lowerBound[curveIndex * 8 + 1] = -Mathf.Max(0, Max - Mathf.Max(curvesMinMax[curveIndex, 0].x, curvesMinMax[curveIndex, 0].y));
            //k- out.x
            lowerBound[curveIndex * 8 + 2] = 0 - previous1Key.outTangent.x;
            //k- out.y
            lowerBound[curveIndex * 8 + 3] = Mathf.Min(0, (4 / 3f) * (Min - (previous1Key.value + (3 / 4f) * previous1Key.outTangent.y)));
            //k+ in.x
            lowerBound[curveIndex * 8 + 4] = 0 - next1Key.inTangent.x;
            //k+ in.y
            lowerBound[curveIndex * 8 + 5] = Mathf.Min(0, -(4 / 3f) * (Max - (next1Key.value - (3 / 4f) * next1Key.inTangent.y)));
            ////k+ out.x
            //lowerBound[curveIndex * 8 + 6] = 0 - next1Key.outTangent.x;
            ////k+ out.y
            //lowerBound[curveIndex * 8 + 7] = Mathf.Min(0, Min - Mathf.Min(curvesMinMax[curveIndex, 1].x, curvesMinMax[curveIndex, 1].y));

        }


        /// <summary>
        /// Fill upper bounds array with delta from current values
        /// </summary>
        private void FillUpperBounds(int curveIndex, AnimationKey previous1Key, AnimationKey next1Key, float Min, float Max)
        {
            ////k- in.x
            //upperBound[curveIndex * 8 + 0] = previous1Key.frame - previous2Key.frame - previous1Key.inTangent.x;
            ////k- in.y
            //upperBound[curveIndex * 8 + 1] = -Mathf.Min(0, Min - Mathf.Min(curvesMinMax[curveIndex, 0].x, curvesMinMax[curveIndex, 0].y));
            //k- out.x
            upperBound[curveIndex * 8 + 2] = frame - previous1Key.frame - previous1Key.outTangent.x;
            //k- out.y
            upperBound[curveIndex * 8 + 3] = Mathf.Max(0, (4 / 3f) * (Max - (previous1Key.value + (3 / 4f) * previous1Key.outTangent.y)));
            //k+ in.x
            upperBound[curveIndex * 8 + 4] = next1Key.frame - frame - next1Key.inTangent.x;
            //k+ in.y
            upperBound[curveIndex * 8 + 5] = Mathf.Max(0, -(4 / 3f) * (Min - (next1Key.value - (3 / 4f) * next1Key.inTangent.y)));
            ////k+ out.x
            //upperBound[curveIndex * 8 + 6] = next2Key.frame - next1Key.frame - next1Key.outTangent.x;
            ////k+ out.y
            //upperBound[curveIndex * 8 + 7] = Mathf.Max(0, Max - Mathf.Max(curvesMinMax[curveIndex, 1].x, curvesMinMax[curveIndex, 1].y));
        }


        public void ModifyTangents(Curve curve, int index, Vector2 inTangent, Vector2 outTangent)
        {
            curve.keys[index].inTangent = inTangent;
            curve.keys[index].outTangent = outTangent;
            curve.ComputeCacheValuesAt(index);
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

            //for (int i = 0; i < animations.Count; i++)
            //{
            //    trsMatrix = trsMatrix * GetBoneMatrix(animations[i], frame);
            //}
            trsMatrix = trsMatrix * GetBoneMatrix(animations[0], frame);
            trsMatrix = trsMatrix * GetBoneMatrix(animations[1], frame);
            return trsMatrix;
        }

        private Matrix4x4 GetBoneMatrix(AnimationSet anim, int frame, bool usepose = true)
        {
            if (null == anim) return Matrix4x4.identity;
            Vector3 position = Vector3.zero;
            if (usepose)
            {
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
            }
            Quaternion rotation = Quaternion.identity;
            Curve rotx = anim.GetCurve(AnimatableProperty.RotationX);
            Curve roty = anim.GetCurve(AnimatableProperty.RotationY);
            Curve rotz = anim.GetCurve(AnimatableProperty.RotationZ);
            if (null != rotx && null != roty && null != rotz)
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