/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
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

using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class UITouchScreen : UIElement
    {
        public Vector2ChangedEvent touchEvent = new Vector2ChangedEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        private float thickness;

        #region Ray

        public override void OnRayClick()
        {
            base.OnRayClick();
            onClickEvent.Invoke();
        }

        public override void OnRayReleaseInside()
        {
            base.OnRayReleaseInside();
            onReleaseEvent.Invoke();
        }

        public override bool OnRayReleaseOutside()
        {
            onReleaseEvent.Invoke();
            return base.OnRayReleaseOutside();
        }

        public override bool OverridesRayEndPoint() { return true; }
        public override void OverrideRayEndPoint(Ray ray, ref Vector3 rayEndPoint)
        {
            // Project ray on the widget plane.
            Plane widgetPlane = new Plane(-transform.forward, transform.position);
            widgetPlane.Raycast(ray, out float enter);
            Vector3 worldCollisionOnWidgetPlane = ray.GetPoint(enter);

            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCollisionOnWidgetPlane);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0f);

            // Clamp
            localProjectedWidgetPosition.x = Mathf.Clamp(localProjectedWidgetPosition.x, 0, width);
            localProjectedWidgetPosition.y = Mathf.Clamp(localProjectedWidgetPosition.y, -height, 0);

            // Normalize local coordinates in [-1, 1]
            Vector2 localScreenCoords = new Vector2((localWidgetPosition.x / width) * 2f - 1f, -((localWidgetPosition.y / height) * 2f + 1f));
            touchEvent.Invoke(localScreenCoords);

            // Ray end point on the screen
            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            rayEndPoint = worldProjectedWidgetPosition;
        }

        #endregion

        #region Create

        public class CreateTouchScreenParams
        {
            public Transform parent = null;
            public string widgetName = "TouchScreen";
            public Vector3 relativeLocation = new Vector3(0, 0, -0.001f);
            public float width = 0.1f;
            public float height = 0.1f;
            public float thickness = 0.001f;
        }


        public static UITouchScreen Create(CreateTouchScreenParams input)
        {
            GameObject go = new GameObject(input.widgetName)
            {
                tag = "UICollider"
            };

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (input.parent)
            {
                UIElement elem = input.parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UITouchScreen uiTouchScreen = go.AddComponent<UITouchScreen>();
            uiTouchScreen.relativeLocation = input.relativeLocation;
            uiTouchScreen.transform.parent = input.parent;
            uiTouchScreen.transform.localPosition = parentAnchor + input.relativeLocation;
            uiTouchScreen.transform.localRotation = Quaternion.identity;
            uiTouchScreen.transform.localScale = Vector3.one;
            uiTouchScreen.width = input.width;
            uiTouchScreen.height = input.height;
            uiTouchScreen.thickness = input.thickness;

            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(uiTouchScreen.width, uiTouchScreen.height, uiTouchScreen.thickness);
            collider.center = new Vector3(uiTouchScreen.width * 0.5f, -uiTouchScreen.height * 0.5f, -uiTouchScreen.thickness * 0.5f);
            collider.isTrigger = true;

            UIUtils.SetRecursiveLayer(go, "CameraHidden");

            return uiTouchScreen;
        }
        #endregion
    }
}
