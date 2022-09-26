using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class IKPoseManipulation : PoseManipulation
    {
        private List<PoseProperty> poseProperties;

        private struct State
        {
            public Vector3 position;
            public Quaternion rotation;
        }

        private State currentState;
        private State desiredState;

        private List<JointController> lockedObjects;
        private List<int> lockedObjectsIndex = new List<int>();

        private int p;
        private double[] theta;
        private double[,] Q_opt;
        private double[] b_opt;
        private double[] lowerBound, upperBound;
        private double[] delta_theta_0;
        private double[] s;
        private double[] delta_theta;

        public IKPoseManipulation(DirectController goalController, Transform mouthpiece, Transform origin = null, List<JointController> locks = null)
        {
            ControllerRig = goalController.target.RootController;
            oTransform = goalController.transform;
            InitMatrices(mouthpiece);
            if (origin == null) origin = goalController.target.PathToRoot.Count > 0 ? goalController.target.PathToRoot[0] : goalController.transform;
            Transform originTrs = InitHierarchy(goalController, origin);
            if (locks != null)
            {
                AddLocks(originTrs.GetComponent<JointController>(), locks);
                lockedObjects.Add(goalController.target);
            }
            SetPoseProperties(goalController.target);
            InitIkData();
        }


        public override void SetDestination(Transform mouthpiece)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            Matrix4x4 target = transformation * initialTransformMatrix;
            Maths.DecomposeMatrix(target, out Vector3 position, out Quaternion rotation, out Vector3 scale);
            targetPosition = position;
            targetRotation = rotation * Quaternion.Euler(-180, 0, 0);
        }

        public override bool TrySolver()
        {
            Setup();
            if (Compute())
                Apply();
            return true;
        }

        private void InitIkData()
        {
            movedObjects = new List<GameObject>();
            startPositions = new List<Vector3>();
            endPositions = new List<Vector3>();
            startRotations = new List<Quaternion>();
            endRotations = new List<Quaternion>();
            startScales = new List<Vector3>();
            endScales = new List<Vector3>();

            fullHierarchy.ForEach(x =>
            {
                movedObjects.Add(x.gameObject);
                startPositions.Add(x.localPosition);
                endPositions.Add(x.localPosition);
                startRotations.Add(x.localRotation);
                endRotations.Add(x.localRotation);
                startScales.Add(x.localScale);
                endScales.Add(x.localScale);
            });
        }

        private void SetPoseProperties(JointController Target)
        {
            poseProperties = new List<PoseProperty>();
            foreach (Transform transform in fullHierarchy)
            {
                DirectController cont = transform.GetComponent<DirectController>();
                poseProperties.Add(new PoseProperty(cont, Target.transform, PoseProperty.PropertyEnum.RotationX));
                poseProperties.Add(new PoseProperty(cont, Target.transform, PoseProperty.PropertyEnum.RotationY));
                poseProperties.Add(new PoseProperty(cont, Target.transform, PoseProperty.PropertyEnum.RotationZ));
                if (cont.FreePosition)
                {
                    poseProperties.Add(new PoseProperty(cont, Target.transform, PoseProperty.PropertyEnum.PositionX));
                    poseProperties.Add(new PoseProperty(cont, Target.transform, PoseProperty.PropertyEnum.PositionY));
                    poseProperties.Add(new PoseProperty(cont, Target.transform, PoseProperty.PropertyEnum.PositionZ));
                }
            }
        }

        private void AddLocks(JointController origin, List<JointController> locks)
        {
            lockedObjects = new List<JointController>();
            foreach (JointController lockController in locks)
            {
                int originIndex = lockController.PathToRoot.IndexOf(origin.transform);
                if (originIndex == -1) continue;
                for (int i = originIndex; i < lockController.PathToRoot.Count; i++)
                {
                    if (fullHierarchy.Contains(lockController.PathToRoot[i].transform)) continue;
                    fullHierarchy.Add(lockController.PathToRoot[i].transform);
                }
                if (fullHierarchy.Contains(lockController.transform)) lockedObjectsIndex.Add(fullHierarchy.IndexOf(lockController.transform));
                else
                {
                    fullHierarchy.Add(lockController.transform);
                    lockedObjectsIndex.Add(fullHierarchy.Count - 1);
                }
                lockedObjects.Add(lockController);
            }
        }

        public bool Setup()
        {
            // rotation curves * hierarchy size + root position curves
            //p = 3 * hierarchySize;
            p = poseProperties.Count;
            theta = GetAllValues(p);

            currentState = new State()
            {
                position = ControllerRig.transform.InverseTransformPoint(oTransform.position),
                rotation = oTransform.rotation
            };
            desiredState = new State()
            {
                position = ControllerRig.transform.InverseTransformPoint(targetPosition),
                rotation = targetRotation
            };

            //debugObject.transform.localPosition = desiredState.position;
            //debugObject.transform.rotation = desiredState.rotation;

            double[,] Js = ds_dtheta(p);

            double[,] DT_D = new double[p, p];


            for (int i = 0; i < poseProperties.Count; i++)
            {
                DT_D[i, i] = poseProperties[i].GetStifness();
            }
            double[,] Delta_s_prime = new double[7, 1];
            for (int i = 0; i < 3; i++)
            {
                Delta_s_prime[i, 0] = desiredState.position[i] - currentState.position[i];
            }
            if ((currentState.rotation * Quaternion.Inverse(desiredState.rotation)).w < 0)
                desiredState.rotation = new Quaternion(-desiredState.rotation.x, -desiredState.rotation.y, -desiredState.rotation.z, -desiredState.rotation.w);
            for (int i = 0; i < 4; i++)
            {
                Delta_s_prime[i + 3, 0] = desiredState.rotation[i] - currentState.rotation[i];
            }

            double wm = 20;
            double wd = 50;

            Q_opt = Maths.Add(Maths.Add(Maths.Multiply(2d * wm, Maths.Multiply(Maths.Transpose(Js), Js)), Maths.Multiply(2d * wd, DT_D)), Maths.Multiply((double)Mathf.Pow(10, -6), Maths.Identity(p)));

            double[,] B_opt = Maths.Multiply(-2d * wm, Maths.Multiply(Maths.Transpose(Js), Delta_s_prime));
            b_opt = Maths.ArrayToColumnArray(B_opt);

            lowerBound = InitializeUBound(p);
            upperBound = InitializeVBound(p);

            delta_theta_0 = new double[p];

            s = new double[p];
            for (int i = 0; i < p; i++)
            {
                s[i] = 1d;
                delta_theta_0[i] = 0d;
            }

            return true;
        }

        public bool Compute()
        {
            alglib.minqpstate state_opt;
            alglib.minqpreport rep;

            alglib.minqpcreate(p, out state_opt);
            alglib.minqpsetquadraticterm(state_opt, Q_opt);
            alglib.minqpsetlinearterm(state_opt, b_opt);
            alglib.minqpsetstartingpoint(state_opt, delta_theta_0);
            alglib.minqpsetbc(state_opt, lowerBound, upperBound);
            alglib.minqpsetscale(state_opt, s);

            if (lockedObjectsIndex.Count > 0)
            {
                double[,] Jrho = drho_dtheta();

                (double[,], int[]) linearConstraints = FindLenearEqualityConstraints(Jrho);
                double[,] C = linearConstraints.Item1;
                int[] CT = linearConstraints.Item2;
                int K_size = CT.Length;

                alglib.minqpsetlc(state_opt, C, CT, K_size);
            }

            alglib.minqpsetalgobleic(state_opt, 0.0, 0.0, 0.0, 0);
            alglib.minqpoptimize(state_opt);
            alglib.minqpresults(state_opt, out delta_theta, out rep);

            return true;
        }

        private bool Apply()
        {

            for (int i = 0; i < poseProperties.Count; i++)
            {
                poseProperties[i].Apply((float)delta_theta[i]);
            }

            for (int c = 0; c < fullHierarchy.Count; c++)
            {
                endPositions[c] = fullHierarchy[c].localPosition;
                endRotations[c] = fullHierarchy[c].localRotation;
                endScales[c] = fullHierarchy[c].localScale;
            }
            return true;
        }

        private double[] GetAllValues(int p)
        {
            double[] theta = new double[p];
            for (int i = 0; i < poseProperties.Count; i++)
            {
                theta[i] = poseProperties[i].GetValue();
            }


            return theta;
        }

        double[,] ds_dtheta(int p)
        {
            double[,] Js = new double[7, p];
            double[] j = new double[7];
            for (int i = 0; i < poseProperties.Count; i++)
            {
                poseProperties[i].GetJs(ref j);
                for (int l = 0; l < 7; l++)
                {
                    Js[l, i] = j[l];
                }
            }
            return Js;
        }

        private double[,] drho_dtheta()
        {
            int size = 7 * lockedObjects.Count;
            double[,] Jrho = new double[size, poseProperties.Count];

            for (int iC = 0; iC < lockedObjects.Count - 1; iC++)
            {
                for (int iProp = 0; iProp < poseProperties.Count; iProp++)
                {
                    double[] Js = new double[7];
                    poseProperties[iProp].GetLockJS(lockedObjects[iC].transform, ref Js);
                    for (int lockProp = 0; lockProp < 7; lockProp++)
                    {
                        Jrho[iC * 7 + lockProp, iProp] = Js[lockProp];
                    }
                }
            }
            int iC2 = lockedObjects.Count - 1;
            for (int iProp = 0; iProp < poseProperties.Count; iProp++)
            {
                double[] Js = new double[7];
                poseProperties[iProp].GetLockJS(lockedObjects[iC2].transform, ref Js);
                for (int lockProp = 3; lockProp < 7; lockProp++)
                {
                    Jrho[iC2 * 7 + lockProp, iProp] = Js[lockProp];
                }
            }

            return Jrho;
        }


        double[] InitializeUBound(int n)
        {
            double[] u = new double[n];
            for (int i = 0; i < n; i++)
            {
                u[i] = Mathf.Min(0, poseProperties[i].GetLowerBound());
            }
            return u;
        }

        double[] InitializeVBound(int n)
        {
            double[] v = new double[n];
            for (int i = 0; i < poseProperties.Count; i++)
            {
                v[i] = Mathf.Max(0, poseProperties[i].GetUpperBound());
            }
            return v;
        }

        private (double[,], int[]) FindLenearEqualityConstraints(double[,] A)
        {
            int K_size = A.GetUpperBound(0) + 1;
            int n_size = A.GetUpperBound(1) + 1;
            double[,] C = new double[K_size, n_size + 1];
            int[] CT = new int[K_size];

            for (int i = 0; i < K_size; i++)
            {
                CT[i] = 0;
                C[i, n_size] = 0;
                for (int j = 0; j < n_size; j++)
                {
                    C[i, j] = A[i, j];
                }
            }
            return (C, CT);
        }

    }
}