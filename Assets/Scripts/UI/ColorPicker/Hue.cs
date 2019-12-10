﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class Hue : MonoBehaviour
    {
        ColorPicker colorPicker;
        float cursorPosition = 0.5f;

        public Transform cursor;

        void Awake()
        {
            colorPicker = GetComponentInParent<ColorPicker>();            
        }

        public float GetHue()
        {
            return cursorPosition;
        }

        public void SetHue(float value)
        {
            cursorPosition = value;
            cursor.localPosition = new Vector3(value - 0.5f, cursor.localPosition.y, cursor.localPosition.z);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.name != "Cursor")
                return;

            Vector3 colliderSphereCenter = other.gameObject.GetComponent<SphereCollider>().center;
            colliderSphereCenter = other.gameObject.transform.localToWorldMatrix.MultiplyPoint(colliderSphereCenter);

            Vector3 position = transform.worldToLocalMatrix.MultiplyPoint(colliderSphereCenter);

            SetHue(Mathf.Clamp(position.x + 1f * 0.5f, 0, 1));
            colorPicker.OnColorChanged();
        }
    }
}