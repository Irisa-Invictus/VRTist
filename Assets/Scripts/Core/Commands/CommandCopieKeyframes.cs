using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandCopieKeyframes : CommandGroup
    {

        readonly List<GameObject> gObjects = new List<GameObject>();



        public CommandCopieKeyframes(List<GameObject> objs, int startSelection, int endSelection, int toFrame) : base("Add Keyframes")
        {
            foreach (GameObject gobject in objs)
            {
                gObjects.Add(gobject);

                AnimationSet animSet = AnimationEngine.Instance.GetObjectAnimation(gobject);
                if (animSet == null) continue;

                foreach (KeyValuePair<AnimatableProperty, Curve> curvePair in animSet.curves)
                {
                    List<AnimationKey> toCopie = new List<AnimationKey>();
                    foreach (AnimationKey key in curvePair.Value.keys)
                    {
                        if (key.frame < startSelection || key.frame > endSelection) continue;
                        toCopie.Add(key);
                    }
                    foreach (AnimationKey cop in toCopie)
                    {
                        int frame = cop.frame - startSelection + toFrame;
                        new CommandAddKeyframe(gobject, curvePair.Key, frame, cop.value, cop.interpolation, false).Submit();
                    }
                }
            }
        }

        public override void Undo()
        {
            base.Undo();
            gObjects.ForEach(x =>
            {
                GlobalState.Animation.onChangeCurve.Invoke(x, AnimatableProperty.PositionX);
            });
        }

        public override void Redo()
        {
            base.Redo();
            gObjects.ForEach(x =>
            {
                GlobalState.Animation.onChangeCurve.Invoke(x, AnimatableProperty.PositionX);
            });
        }

        public override void Submit()
        {
            base.Submit();
            gObjects.ForEach(x =>
            {
                GlobalState.Animation.onChangeCurve.Invoke(x, AnimatableProperty.PositionX);
            });
        }
    }

}