using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace VRtist
{
    public class VRButton : MonoBehaviour
    {
        public UnityEvent OnClickEvent;
        public UnityEvent OnReleaseEvent;

        private bool isHovered;
        public void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<VRPickerSelector>(out VRPickerSelector selector))
            {
                isHovered = true;
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (isHovered && other.TryGetComponent<VRPickerSelector>(out VRPickerSelector selector))
            {
                isHovered = false;
            }
        }

        public void Update()
        {
            if (isHovered)
            {
                VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.trigger, () => OnClickEvent.Invoke(), () => OnReleaseEvent.Invoke());
            }
        }

    }

}