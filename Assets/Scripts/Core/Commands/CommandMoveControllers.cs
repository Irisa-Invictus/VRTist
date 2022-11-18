using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandMoveControllers : ICommand
    {
        private List<RigObjectController> controllers;

        private List<Vector3> beginPositions;
        private List<Quaternion> beginRotations;
        private List<Vector3> beginScales;

        private List<Vector3> endPositions;
        private List<Quaternion> endRotations;
        private List<Vector3> endScales;

        public CommandMoveControllers(List<RigObjectController> co, List<Vector3> bp, List<Quaternion> br, List<Vector3> bs, List<Vector3> ep, List<Quaternion> er, List<Vector3> es)
        {
            controllers = new List<RigObjectController>(co);
            beginPositions = bp;
            beginRotations = br;
            beginScales = bs;
            endPositions = ep;
            endRotations = er;
            endScales = es;
        }

        public CommandMoveControllers(List<RigConstraintController> co, List<Vector3> bp, List<Quaternion> br, List<Vector3> bs, List<Vector3> ep, List<Quaternion> er, List<Vector3> es)
        {
            controllers = new List<RigObjectController>();
            co.ForEach(x => controllers.Add(x));
            beginPositions = bp;
            beginRotations = br;
            beginScales = bs;
            endPositions = ep;
            endRotations = er;
            endScales = es;
        }

        public CommandMoveControllers(RigObjectController co, Vector3 bp, Quaternion br, Vector3 bs, Vector3 ep, Quaternion er, Vector3 es)
        {
            controllers = new List<RigObjectController> { co };
            beginPositions = new List<Vector3> { bp };
            beginRotations = new List<Quaternion> { br };
            beginScales = new List<Vector3> { bs };
            endPositions = new List<Vector3> { ep };
            endRotations = new List<Quaternion> { er };
            endScales = new List<Vector3> { es };
        }

        public override void Redo()
        {
            for (int i = 0; i < controllers.Count; i++)
            {
                Transform transform = controllers[i].transform;
                transform.localPosition = endPositions[i];
                transform.localRotation = endRotations[i];
                transform.localScale = endScales[i];
                controllers[i].UpdateController();
            }
        }

        public override void Undo()
        {
            for (int i = 0; i < controllers.Count; i++)
            {
                Transform transform = controllers[i].transform;
                transform.localPosition = beginPositions[i];
                transform.localRotation = beginRotations[i];
                transform.localScale = beginScales[i];
                controllers[i].UpdateController();
            }
        }

        public override void Submit()
        {
            if (null != controllers && controllers.Count > 0)
            {
                Redo();
                CommandManager.AddCommand(this);
            }
        }
    }

}