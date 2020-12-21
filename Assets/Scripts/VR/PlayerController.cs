﻿using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Base Parameters")]
        [SerializeField] private Transform world = null;
        [SerializeField] private Transform buttonsContainer = null;
        [SerializeField] private Transform navigationParametersContainer = null;
        [SerializeField] private Transform leftHandle = null;
        [SerializeField] private Transform rightHandle = null;
        [SerializeField] private Transform pivot = null;
        [SerializeField] private Transform vrCamera = null;
        [Tooltip("Player can be xxx times bigger than the world")]
        public float maxPlayerScale = 2000.0f;// world min scale = 0.0005f;
        [Tooltip("Player can be xxx times smaller than the world")]
        public float minPlayerScale = 50.0f; // world scale = 50.0f;

        [Header("Navigation Options")]
        public NavigationOptions options;

        [Header("BiManual Navigation Mode")]
        public StretchUI lineUI = null;

        [Header("Orbit Navigation")]
        public StraightRay ray = null;

        [Header("Teleport Navigation")]
        public TeleportUI teleport = null;
        public TrajectoryParams trajectoryParams = new TrajectoryParams();

        //private NavigationMode currentNavigationMode = null;

        private const float deadZone = 0.3f; // for palette pop

        private GameObject tooltipPalette = null;
        private GameObject tooltipUndo = null;
        private GameObject tooltipRedo = null;
        private GameObject tooltipReset = null;

        private Vector3 previousPosition;
        private Vector3 previousForward;
        private Transform rightHanded;

        void Start()
        {
            if (!VRInput.TryGetDevices())
            {
                Debug.LogWarning("PlayerController cannot VRInput.TryGetDevices().");
            }

            if (vrCamera == null) { Debug.LogWarning("Cannot find 'VRCamera' game object"); }
            if (leftHandle == null) { Debug.LogWarning("Cannot find 'LeftHandle' game object"); }
            if (rightHandle == null) { Debug.LogWarning("Cannot find 'RightHandle' game object"); }
            if (pivot == null) { Debug.LogWarning("Cannot find 'Pivot' game object"); }

            if (ray != null)
                ray.gameObject.SetActive(false);

            if (teleport != null)
                teleport.gameObject.SetActive(false);

            Tooltips.SetText(VRDevice.SecondaryController, Tooltips.Location.Trigger, Tooltips.Action.HoldPush, "Open Palette");
            Tooltips.SetText(VRDevice.SecondaryController, Tooltips.Location.Primary, Tooltips.Action.Push, "Undo");
            Tooltips.SetText(VRDevice.SecondaryController, Tooltips.Location.Secondary, Tooltips.Action.Push, "Redo");
            Tooltips.SetText(VRDevice.SecondaryController, Tooltips.Location.Joystick, Tooltips.Action.Push, "Reset");

            OnChangeNavigationMode("BiManual");

            rightHanded = world.Find("Avatars");

            StartCoroutine(SendPlayerTransform());
        }

        void Update()
        {
            if (VRInput.TryGetDevices())
            {
                // NAVIGATION
                HandleNavigation();

                // RESET/FIT -- Left Joystick Click
                if (IsCompatibleWithReset(options.currentNavigationMode))
                {
                    HandleReset();
                }

                // RESET SCALE -- Right Joystick Click
                if (IsCompatibleWithResetScale(options.currentNavigationMode))
                {
                    HandleResetScale();
                }

                // PALETTE POP -- Left Trigger
                if (IsCompatibleWithPalette(options.currentNavigationMode))
                {
                    HandlePalette();
                }

                // UNDO/REDO -- Left A/B
                if (IsCompatibleWithUndoRedo(options.currentNavigationMode))
                {
                    HandleUndoRedo();
                }

                // Time manipulation
                if (IsCompatibleWithTimeManipulation(options.currentNavigationMode))
                {
                    HandleTimeManipulation();
                }
            }
        }

        IEnumerator SendPlayerTransform()
        {
            while (true)
            {
                // Send position and orientation
                Vector3 forward = vrCamera.forward;
                if (vrCamera.position != previousPosition || previousForward != forward)
                {
                    previousPosition = vrCamera.position;
                    previousForward = forward;

                    Vector3 upRight = vrCamera.position + vrCamera.forward + vrCamera.up + vrCamera.right;
                    Vector3 upLeft = vrCamera.position + vrCamera.forward + vrCamera.up - vrCamera.right;
                    Vector3 bottomRight = vrCamera.position + vrCamera.forward - vrCamera.up + vrCamera.right;
                    Vector3 bottomLeft = vrCamera.position + vrCamera.forward - vrCamera.up - vrCamera.right;
                    Vector3 target = vrCamera.position + vrCamera.forward * 2f;

                    GlobalState.networkUser.eye = rightHanded.InverseTransformPoint(vrCamera.position);
                    GlobalState.networkUser.target = rightHanded.InverseTransformPoint(target);
                    GlobalState.networkUser.corners[0] = rightHanded.InverseTransformPoint(upLeft);
                    GlobalState.networkUser.corners[1] = rightHanded.InverseTransformPoint(upRight);
                    GlobalState.networkUser.corners[2] = rightHanded.InverseTransformPoint(bottomRight);
                    GlobalState.networkUser.corners[3] = rightHanded.InverseTransformPoint(bottomLeft);

                    // TODO: fix this. Instance is sometimes null, even if retrieved in Awake.
                    if (MixerClient.GetInstance())
                    {
                        MixerClient.GetInstance().SendPlayerTransform(GlobalState.networkUser);
                    }
                }
                yield return new WaitForSeconds(1f / 15f);
            }
        }

        private void HandleNavigation()
        {
            if (!leftHandle.gameObject.activeSelf)
            {
                leftHandle.gameObject.SetActive(true);
            }

            // Update left controller transform
            VRInput.UpdateTransformFromVRDevice(leftHandle, VRInput.secondaryController);
            VRInput.UpdateTransformFromVRDevice(rightHandle, VRInput.primaryController);

            if (null != options.currentNavigationMode)
                options.currentNavigationMode.Update();
        }

        private void FitToSelection()
        {
            Transform cam = vrCamera.transform;

            Vector3 bmin = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            Vector3 bmax = new Vector3(-Mathf.Infinity, -Mathf.Infinity, -Mathf.Infinity);

            Vector3[] points = new Vector3[8];
            Vector3 center = Vector3.zero;
            int bboxCount = 0;

            // parse selection
            foreach (var item in Selection.selection)
            {
                // get all meshes of selected object
                GameObject gobj = item.Value;
                MeshFilter[] meshFilters = gobj.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    Mesh mesh = meshFilter.mesh;
                    if (null == mesh)
                        continue;

                    Bounds bounds = mesh.bounds;
                    Vector3 omin = bounds.min;
                    Vector3 omax = bounds.max;
                    bboxCount++;

                    Matrix4x4 meshMatrix = meshFilter.transform.localToWorldMatrix;
                    // Get absolute bbox center
                    Vector3 globalBoundCenter = meshMatrix.MultiplyPoint(bounds.center);
                    center += globalBoundCenter;

                    // Get all points of the bounding box
                    points[0] = meshMatrix.MultiplyPoint(new Vector3(omin.x, omin.y, omin.z));
                    points[1] = meshMatrix.MultiplyPoint(new Vector3(omin.x, omin.y, omax.z));
                    points[2] = meshMatrix.MultiplyPoint(new Vector3(omin.x, omax.y, omin.z));
                    points[3] = meshMatrix.MultiplyPoint(new Vector3(omin.x, omax.y, omax.z));
                    points[4] = meshMatrix.MultiplyPoint(new Vector3(omax.x, omin.y, omin.z));
                    points[5] = meshMatrix.MultiplyPoint(new Vector3(omax.x, omin.y, omax.z));
                    points[6] = meshMatrix.MultiplyPoint(new Vector3(omax.x, omax.y, omin.z));
                    points[7] = meshMatrix.MultiplyPoint(new Vector3(omax.x, omax.y, omax.z));

                    // check min/max
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 p = points[i];
                        if (p.x < bmin.x) bmin.x = p.x;
                        if (p.y < bmin.y) bmin.y = p.y;
                        if (p.z < bmin.z) bmin.z = p.z;
                        if (p.x > bmax.x) bmax.x = p.x;
                        if (p.y > bmax.y) bmax.y = p.y;
                        if (p.z > bmax.z) bmax.z = p.z;
                    }
                }
            }

            center /= (float) bboxCount;

            // compute distance to camera;
            float max = Mathf.Max(bmax.x - bmin.x, bmax.y - bmin.y);
            max = Mathf.Max(max, bmax.z - bmin.z);
            float newDistance = 3 * max;

            // clamp distance with near/far camera planes
            float near = cam.GetComponent<Camera>().nearClipPlane * 1.2f;
            float far = cam.GetComponent<Camera>().farClipPlane * 0.8f;
            if (newDistance < near)
                newDistance = near;
            if (newDistance > far)
                newDistance = far;

            // compute new camera position
            Vector3 cameraGlobalForward = cam.forward;
            Vector3 newCameraPosition = center - cameraGlobalForward * newDistance;

            // do not apply the position to the camera but invert it and apply to world
            Vector3 deltaCamera = newCameraPosition - cam.position;
            transform.position += deltaCamera;
        }

        private void ResetCameraClipPlanes()
        {
            float oldScale = 1f / GlobalState.WorldScale;
            float nearPlane = Camera.main.nearClipPlane / oldScale;
            float farPlane = Camera.main.farClipPlane / oldScale;
            Camera.main.nearClipPlane = nearPlane;
            Camera.main.farClipPlane = farPlane;
            GlobalState.WorldScale = 1f;
        }

        private void HandleReset()
        {
            VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.primary2DAxisClick,
            () =>
            {
                if (Selection.selection.Count == 0)
                {
                    ResetCameraClipPlanes();
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one;
                }
                else
                {
                    FitToSelection();
                }
            });
        }

        private void HandleResetScale()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.primary2DAxisClick,
            () =>
            {
                ResetCameraClipPlanes();

                Transform cam = vrCamera.transform;
                Vector3 initCameraInWorldPosition = cam.position;
                transform.localScale = Vector3.one;

                transform.position += initCameraInWorldPosition - cam.position;
            });
        }

        private void HandlePalette()
        {
            if (null == ToolsUIManager.Instance)
                return;

            if (VRInput.GetValue(VRInput.secondaryController, CommonUsages.trigger) > deadZone)
            {
                ToolsUIManager.Instance.PopUpPalette(true);
            }
            else
            {
                ToolsUIManager.Instance.PopUpPalette(false);
            }
        }

        private void HandleUndoRedo()
        {
            VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.primaryButton, () => { },
            () =>
            {
                CommandManager.Undo();
            });
            VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.secondaryButton, () => { },
            () =>
            {
                CommandManager.Redo();
            });
        }

        private void HandleTimeManipulation()
        {
            bool joyRightJustClicked = false;
            bool joyRightJustReleased = false;
            bool joyRightLongPush = false;
            VRInput.GetInstantJoyEvent(VRInput.secondaryController, VRInput.JoyDirection.RIGHT, ref joyRightJustClicked, ref joyRightJustReleased, ref joyRightLongPush);

            bool joyLeftJustClicked = false;
            bool joyLeftJustReleased = false;
            bool joyLeftLongPush = false;
            VRInput.GetInstantJoyEvent(VRInput.secondaryController, VRInput.JoyDirection.LEFT, ref joyLeftJustClicked, ref joyLeftJustReleased, ref joyLeftLongPush);

            // Manage time with joystick
            if (joyRightJustClicked || joyLeftJustClicked || joyRightLongPush || joyLeftLongPush)
            {
                int frame = GlobalState.Animation.CurrentFrame;
                if (joyRightJustClicked || joyRightLongPush)
                {
                    frame = Mathf.Clamp(frame + 1, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
                }
                else
                {
                    frame = Mathf.Clamp(frame - 1, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
                }
                GlobalState.Animation.CurrentFrame = frame;
            }
        }

        public void HandleCommonTooltipsVisibility()
        {
            if (options.currentNavigationMode == null)
                return;

            if (IsCompatibleWithReset(options.currentNavigationMode))
            {
                Tooltips.SetText(VRDevice.SecondaryController, Tooltips.Location.Joystick, Tooltips.Action.Push, "Reset");
            }
            else
            {
                Tooltips.SetVisible(VRDevice.SecondaryController, Tooltips.Location.Joystick, false);
            }

            if (IsCompatibleWithPalette(options.currentNavigationMode))
            {
                Tooltips.SetText(VRDevice.SecondaryController, Tooltips.Location.Trigger, Tooltips.Action.Push, "Open Palette");
            }
            else
            {
                Tooltips.SetVisible(VRDevice.SecondaryController, Tooltips.Location.Trigger, false);
            }

            if (IsCompatibleWithUndoRedo(options.currentNavigationMode))
            {
                Tooltips.SetText(VRDevice.SecondaryController, Tooltips.Location.Primary, Tooltips.Action.Push, "Undo");
                Tooltips.SetText(VRDevice.SecondaryController, Tooltips.Location.Secondary, Tooltips.Action.Push, "Redo");
            }
            else
            {
                Tooltips.SetVisible(VRDevice.SecondaryController, Tooltips.Location.Primary, false);
                Tooltips.SetVisible(VRDevice.SecondaryController, Tooltips.Location.Secondary, false);
            }
        }

        #region OnNavMode

        // Callback for the NavigationMode buttons.
        public void OnChangeNavigationMode(string buttonName)
        {
            UpdateRadioButtons(buttonName);

            Tooltips.HideAll(VRDevice.SecondaryController);

            if (options.currentNavigationMode != null)
                options.currentNavigationMode.DeInit();

            switch (buttonName)
            {
                case "BiManual": OnNavMode_BiManual(); break;
                case "Teleport": OnNavMode_Teleport(); break;
                case "Orbit": OnNavMode_Orbit(); break;
                case "Fps": OnNavMode_Fps(); break;
                case "Drone": OnNavMode_Drone(); break;
                case "Fly": OnNavMode_Fly(); break;
                default: Debug.LogError("Unknown navigation mode button name was passed."); break;
            }
            HandleCommonTooltipsVisibility();

            // TODO Scriptable: trouver qui accede a ca
            //GlobalState.currentNavigationMode = currentNavigationMode;
        }

        public void OnNavMode_BiManual()
        {
            // TODO: Scriptable NEW ou CreateInstance(SO), ou avoir une liste de SO dans PlayerController.
            options.currentNavigationMode = new NavigationMode_BiManual(lineUI, minPlayerScale, maxPlayerScale);
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Teleport()
        {
            options.currentNavigationMode = new NavigationMode_Teleport(teleport, trajectoryParams);
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Orbit()
        {
            options.currentNavigationMode = new NavigationMode_Orbit(ray, options.orbitRotationalSpeed, options.orbitScaleSpeed, options.orbitMoveSpeed, minPlayerScale, maxPlayerScale);
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Fps()
        {
            options.currentNavigationMode = new NavigationMode_FPS();
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Drone()
        {
            options.currentNavigationMode = new NavigationMode_Drone();
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Fly()
        {
            options.currentNavigationMode = new NavigationMode_Fly(options.flySpeed, minPlayerScale, maxPlayerScale);
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
        }

        private void UpdateRadioButtons(string activeButtonName)
        {
            if (buttonsContainer != null)
            {
                for (int i = 0; i < buttonsContainer.transform.childCount; ++i)
                {
                    Transform t = buttonsContainer.GetChild(i);
                    UIButton button = t.GetComponent<UIButton>();
                    if (button != null)
                    {
                        button.Checked = button.name == activeButtonName;
                    }
                }
            }
        }

        #endregion

        #region On Navigation Options Change

        public void OnFlightSpeedChange(float value)
        {
            options.flightSpeed = value;
        }

        public void OnFlightRotationSpeedChange(float value)
        {
            options.flightRotationSpeed = value;
        }

        public void OnFlightDampingChange(float value)
        {
            options.flightDamping = value;
        }

        public void OnFPSSpeedChange(float value)
        {
            options.fpsSpeed = value;
        }

        public void OnFPSRotationSpeedChange(float value)
        {
            options.fpsRotationSpeed = value;
        }

        public void OnFPSDampingChange(float value)
        {
            options.fpsDamping = value;
        }

        public void OnOrbitScaleSpeedChange(float value)
        {
            options.orbitScaleSpeed = value / 100.0f;
        }

        public void OnOrbitMoveSpeedChange(float value)
        {
            options.orbitMoveSpeed = value / 100.0f;
        }

        public void OnOrbitRotationalSpeedChange(float value)
        {
            options.orbitRotationalSpeed = value;
        }

        public void OnTeleportLockHeightChange(bool value)
        {
            options.lockHeight = value;
        }

        #endregion

        private bool IsCompatibleWithPalette(NavigationMode n) { return options.CanUseControls(NavigationMode.UsedControls.LEFT_TRIGGER); }
        private bool IsCompatibleWithUndoRedo(NavigationMode n) { return options.CanUseControls(NavigationMode.UsedControls.LEFT_PRIMARY | NavigationMode.UsedControls.LEFT_SECONDARY); }
        private bool IsCompatibleWithReset(NavigationMode n) { return options.CanUseControls(NavigationMode.UsedControls.LEFT_JOYSTICK_CLICK); }
        private bool IsCompatibleWithResetScale(NavigationMode n) { return options.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK_CLICK); }
        private bool IsCompatibleWithTimeManipulation(NavigationMode n) { return options.CanUseControls(NavigationMode.UsedControls.LEFT_JOYSTICK); }
    }
}
