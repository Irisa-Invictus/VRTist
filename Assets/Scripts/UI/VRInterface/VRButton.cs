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