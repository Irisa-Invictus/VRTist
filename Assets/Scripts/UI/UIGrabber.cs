﻿using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    public class UIGrabber : UIElement
    {
        [HideInInspector] public int? uid;
        public bool rotateOnHover = true;
        public GameObject prefab;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public GameObjectHashChangedEvent onEnterUI3DObject = null;
        public GameObjectHashChangedEvent onExitUI3DObject = null;
        public UnityEvent onHoverEvent = null;
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        void Start()
        {
            if (prefab)
            {
                if (ToolsUIManager.Instance != null)
                {
                    ToolsUIManager.Instance.RegisterUI3DObject(prefab);
                    uid = prefab.GetHashCode();
                    transform.localRotation = Quaternion.Euler(25f, -35f, 0f);
                }
            }
        }

        private void OnValidate()
        {
            NeedsRebuild = true;
        }

        private void Update()
        {
            if (NeedsRebuild)
            {
                UpdateLocalPosition();
                ResetColor();
                NeedsRebuild = false;
            }
        }

        public override void ResetColor()
        {
            base.ResetColor();
        }

        // Handles multi-mesh and multi-material per mesh.
        public override void SetColor(Color color)
        {
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                Material[] materials = meshRenderer.materials;
                foreach (Material material in meshRenderer.materials)
                {
                    material.SetColor("_BaseColor", color);
                }
            }
        }

        #region ray

        public override void OnRayEnter()
        {
            base.OnRayEnter();

            GoFrontAnimation();

            if (uid != null)
            {
                onEnterUI3DObject.Invoke((int) uid);
            }

            WidgetBorderHapticFeedback();
        }

        public override void OnRayEnterClicked()
        {
            base.OnRayEnterClicked();

            GoFrontAnimation();

            if (uid != null)
            {
                onEnterUI3DObject.Invoke((int) uid);
            }

            WidgetBorderHapticFeedback();
        }

        public override void OnRayHover(Ray ray)
        {
            base.OnRayHover(ray);

            onHoverEvent.Invoke();

            if (rotateOnHover) { RotateAnimation(); }
        }

        public override void OnRayHoverClicked()
        {
            base.OnRayHoverClicked();

            onHoverEvent.Invoke();

            if (rotateOnHover) { RotateAnimation(); }
        }

        public override void OnRayExit()
        {
            base.OnRayExit();

            GoBackAnimation();

            if (rotateOnHover) { ResetRotation(); }

            if (uid != null)
            {
                onExitUI3DObject.Invoke((int) uid);
            }

            WidgetBorderHapticFeedback();
        }

        public override void OnRayExitClicked()
        {
            base.OnRayExitClicked();

            GoBackAnimation();

            if (uid != null)
            {
                onExitUI3DObject.Invoke((int) uid);
            }

            WidgetBorderHapticFeedback();
        }

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
            if (rotateOnHover) { ResetRotation(); }
            return base.OnRayReleaseOutside();
        }

        public void GoFrontAnimation()
        {
            transform.localPosition += new Vector3(0f, 0f, -0.02f); // avance vers nous, dnas le repere de la page (local -Z)
            transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }

        public void GoBackAnimation()
        {
            transform.localPosition += new Vector3(0f, 0f, +0.02f); // recule, dnas le repere de la page (local +Z)
            transform.localScale = Vector3.one;
        }

        public void RotateAnimation()
        {
            transform.localRotation *= Quaternion.Euler(0f, -3f, 0f); // rotate autour du Y du repere du parent (penche a 25, -35, 0)
        }

        public void ResetRotation()
        {
            transform.localRotation = Quaternion.Euler(25f, -35f, 0f);
        }

        #endregion
    }
}
