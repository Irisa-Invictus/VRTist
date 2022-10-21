using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist.Serialization
{
    public interface IBlob
    {
        byte[] ToBytes();
        void FromBytes(byte[] bytes, ref int index);
    }
}