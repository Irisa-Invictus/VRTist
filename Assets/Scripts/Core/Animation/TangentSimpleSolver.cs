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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Profiling;

namespace VRtist
{

    public class TangentSimpleSolver
    {
        private Vector3 positionTarget;
        private Quaternion rotationTarget;
        public AnimationSet ObjectAnimation;
        private int currentFrame;
        private int startFrame;
        public int previousKeyIndex;
        private int endFrame;
        public int nextKeyIndex;
        private double tangentEnergy;

        private List<AnimationKey> previousKeys;
        private List<AnimationKey> nextKeys;

        //optimize for position x,y,z eulerRotation x,y,z 
        private int propertiesCount = 6;
        //foreach property k-1 in.x, k-1 in.y, k-1 out.x, k-1 out.y, k+1 in.x, K+1 in.y, k+1 out.x, k+1 out.y
        //6*8
        private int valueCount = 48;


        struct State
        {
            public Vector3 position;
            public Vector3 euler_orientation;
            public int time;
        }


        public TangentSimpleSolver(Vector3 targetPosition, Quaternion targetRotation, AnimationSet animation, int frame, int start, int end, double tanEnergy)
        {
            positionTarget = targetPosition;
            rotationTarget = targetRotation;
            ObjectAnimation = animation;
            currentFrame = frame;
            startFrame = start;
            endFrame = end;
            tangentEnergy = tanEnergy;

        }



        public bool TrySolver()
        {
            ObjectAnimation.curves[AnimatableProperty.PositionX].GetKeyIndex(startFrame, out previousKeyIndex);
            int firstFrame = ObjectAnimation.curves[AnimatableProperty.PositionX].keys[previousKeyIndex].frame;
            ObjectAnimation.curves[AnimatableProperty.PositionX].GetKeyIndex(endFrame, out nextKeyIndex);
            int lastFrame = ObjectAnimation.curves[AnimatableProperty.PositionX].keys[nextKeyIndex].frame;

            if (currentFrame < firstFrame) return false;
            if (currentFrame > lastFrame) return false;

            double[] theta = GetAllTangents();
            double[,] Theta = ColumnArrayToArray(theta);
            State currentState = GetCurrentState(currentFrame);
            State desiredState = new State()
            {
                position = positionTarget,
                euler_orientation = rotationTarget.eulerAngles,
                time = currentFrame
            };

            double[,] Js = dc_dtheta(currentFrame);
            double[,] DT_D = new double[valueCount, valueCount];
            for (int i = 0; i < valueCount; i++)
            {
                DT_D[i, i] = 0d * 0d;
            }

            double[,] Delta_s_prime = new double[6, 1];
            for (int i = 0; i <= 2; i++)
            {
                Delta_s_prime[i, 0] = desiredState.position[i] - currentState.position[i];
            }
            for (int i = 3; i <= 5; i++)
            {
                Delta_s_prime[i, 0] = -Mathf.DeltaAngle(desiredState.euler_orientation[i - 3], currentState.euler_orientation[i - 3]);
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
            double wb = tangentEnergy;
            double wd = 1d;

            double[,] Q_opt = Maths.Add(Maths.Add(Maths.Multiply(2d * wm, Maths.Multiply(Maths.Transpose(Js), Js)), Maths.Add(Maths.Multiply(2d * wd, DT_D), Maths.Multiply(2d * wb, TT_T))), Maths.Multiply((double)Mathf.Pow(10, -6), Maths.Identity(valueCount)));

            double[,] B_opt = Maths.Add(Maths.Multiply(-2d * wm, Maths.Multiply(Maths.Transpose(Js), Delta_s_prime)), Maths.Multiply(2d * wb, Maths.Multiply(TT_T, Theta)));
            double[] b_opt = Maths.ArrayToColumnArray(B_opt);

            double[] delta_theta_0 = new double[valueCount];
            double[] delta_theta;
            double[] s = new double[valueCount];
            for (int i = 0; i < valueCount; i++)
            {
                s[i] = 1d;
                delta_theta_0[i] = 0d;
            }

            alglib.minqpstate state_opt;
            alglib.minqpreport rep;

            alglib.minqpcreate(valueCount, out state_opt);
            alglib.minqpsetquadraticterm(state_opt, Q_opt);
            alglib.minqpsetlinearterm(state_opt, b_opt);
            alglib.minqpsetstartingpoint(state_opt, delta_theta_0);

            alglib.minqpsetscale(state_opt, s);

            alglib.minqpsetalgobleic(state_opt, 0.0, 0.0, 0.0, 0);
            alglib.minqpoptimize(state_opt);
            alglib.minqpresults(state_opt, out delta_theta, out rep);


            double[] new_theta = new double[valueCount];
            for (int i = 0; i < valueCount; i++)
            {
                new_theta[i] = delta_theta[i] + theta[i];
            }

            for (int i = 0; i < valueCount; i++)
            {
                if (System.Double.IsNaN(delta_theta[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < 6; i++)
            {

                AnimatableProperty property = (AnimatableProperty)i;
                Curve curve = ObjectAnimation.curves[property];

                Vector2 inTangentp = new Vector2((float)new_theta[(i * 8) + 0], (float)new_theta[(i * 8) + 1]);
                Vector2 outTangentp = new Vector2((float)new_theta[(i * 8) + 2], (float)new_theta[(i * 8) + 3]);
                ModifyTangents(curve, previousKeyIndex, inTangentp, outTangentp);

                Vector2 inTangentn = new Vector2((float)new_theta[(i * 8) + 4], (float)new_theta[(i * 8) + 5]);
                Vector2 outTangentn = new Vector2((float)new_theta[(i * 8) + 6], (float)new_theta[(i * 8) + 7]);
                ModifyTangents(curve, nextKeyIndex, inTangentn, outTangentn);
            }
            return true;
        }

        private State GetCurrentState(int currentFrame)
        {
            float[] data = new float[6];
            for (int i = 0; i < data.Length; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                ObjectAnimation.GetCurve(property).Evaluate(currentFrame, out data[i]);
            }
            return new State()
            {
                position = new Vector3(data[0], data[1], data[2]),
                euler_orientation = new Vector3(data[3], data[4], data[5]),
                time = currentFrame
            };

        }

        private double[] GetAllTangents()
        {
            double[] theta = new double[valueCount];
            previousKeys = new List<AnimationKey>();
            nextKeys = new List<AnimationKey>();
            for (int i = 0; i < 6; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                Curve curve = ObjectAnimation.GetCurve(property);
                AnimationKey prevKey = curve.keys[previousKeyIndex];
                previousKeys.Add(prevKey);
                theta[8 * i + 0] = prevKey.inTangent.x;
                theta[8 * i + 1] = prevKey.inTangent.y;
                theta[8 * i + 2] = prevKey.outTangent.x;
                theta[8 * i + 3] = prevKey.outTangent.y;
                AnimationKey nextKey = curve.keys[nextKeyIndex];
                nextKeys.Add(nextKey);
                theta[8 * i + 4] = nextKey.inTangent.x;
                theta[8 * i + 5] = nextKey.inTangent.y;
                theta[8 * i + 6] = nextKey.outTangent.x;
                theta[8 * i + 7] = nextKey.outTangent.y;
            }
            return theta;
        }

        public void ModifyTangents(Curve curve, int index, Vector2 inTangent, Vector2 outTangent)
        {
            curve.keys[index].inTangent = inTangent;
            curve.keys[index].outTangent = outTangent;
            curve.ComputeCacheValuesAt(index);
        }

        double[,] ColumnArrayToArray(double[] m)
        {
            int row = m.Length;
            double[,] response = new double[row, 1];
            for (int i = 0; i < row; i++)
            {
                response[i, 0] = m[i];
            }
            return response;
        }


        double[,] dc_dtheta(int frame)
        {

            double[,] Js2 = new double[propertiesCount, valueCount];
            float dtheta = 1;

            //property moved
            for (int i = 0; i < 6; i++)
            {
                Vector2 prevK = new Vector2(previousKeys[i].frame, previousKeys[i].value);
                Vector2 nextK = new Vector2(nextKeys[i].frame, nextKeys[i].value);
                Vector2 previousKeyOut = new Vector2(previousKeys[0].outTangent.x, previousKeys[0].outTangent.y);
                Vector2 nextKeyIn = new Vector2(nextKeys[0].inTangent.x, nextKeys[0].inTangent.y);

                float currentValue = Bezier.EvaluateBezier(prevK, prevK + previousKeyOut, nextK - nextKeyIn, nextK, frame);
                //prop i affected by property k-1 out.x
                Vector2 B1 = prevK + (previousKeyOut + new Vector2(dtheta, 0));
                Vector2 C1 = nextK - nextKeyIn;
                Js2[i, (i * 8) + 2] = (Bezier.EvaluateBezier(prevK, B1, C1, nextK, frame) - currentValue) / (double)dtheta;
                //property k-1 out.y
                Vector2 B2 = prevK + (previousKeyOut + new Vector2(0, dtheta));
                Vector2 C2 = nextK - nextKeyIn;
                Js2[i, (i * 8) + 3] = (Bezier.EvaluateBezier(prevK, B2, C2, nextK, frame) - currentValue) / (double)dtheta;
                //property K+1 in.x
                Vector2 B4 = prevK + previousKeyOut;
                Vector2 C4 = nextK - (nextKeyIn + new Vector2(dtheta, 0));
                Js2[i, (i * 8) + 4] = (Bezier.EvaluateBezier(prevK, B4, C4, nextK, frame) - currentValue) / (double)dtheta;
                //property K+1 in.y
                Vector2 B5 = prevK + previousKeyOut;
                Vector2 C5 = nextK - (nextKeyIn + new Vector2(0, dtheta));
                Js2[i, (i * 8) + 5] = (Bezier.EvaluateBezier(prevK, B5, C5, nextK, frame) - currentValue) / (double)dtheta;
            }

            return Js2;
        }
    }
}
