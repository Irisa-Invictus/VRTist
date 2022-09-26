using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{

    public class IKRotationGizmoPose : IKPoseManipulation
    {
        public IKRotationGizmoPose(DirectController goalController, Transform mouthpiece) : base(goalController, mouthpiece)
        {
        }

    }

}