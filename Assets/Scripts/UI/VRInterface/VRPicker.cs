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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class VRPicker : MonoBehaviour
    {
        public VRPickerTool PickerTool;
        public GoalGizmo PickerGizmo;
        private bool locked;
        private bool OutAndLocked;

        public bool Lock { set { locked = value; if (!locked && OutAndLocked) AutoSwitchOffTool(); } }


        private string previousTool;
        public GameObject PickerBase;
        private GameObject root;
        public bool UseTPose;
        public GameObject PoseButton;

        private GameObject PickerClone;
        private GameObject Target;
        public Dictionary<GameObject, GameObject> CloneToTarget = new Dictionary<GameObject, GameObject>();
        public List<RigConstraintController> controllers = new List<RigConstraintController>();
        public List<DirectController> directController = new List<DirectController>();
        public Material Invisible;
        public GameObject BoxControllerBase;
        public GameObject ControllerBase;
        public Material Red;
        public Material Blue;
        public Material Yellow;

        public void SwitchTPose()
        {
            UseTPose = !UseTPose;
            PoseButton.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", UseTPose ? Color.blue : Color.red);
            ResetTPose(UseTPose);
        }

        public void ResetControllerPosition()
        {
            PickerTool.ResetControllers();
        }

        public void ColorizeController(GameObject Controller)
        {
            if (Controller.name.Contains("Left"))
            {
                Controller.GetComponent<MeshRenderer>().material = Red;
            }
            else if (Controller.name.Contains("Right"))
            {
                Controller.GetComponent<MeshRenderer>().material = Blue;
            }
            else
            {
                Controller.GetComponent<MeshRenderer>().material = Yellow;
            }
        }

        public void InstantiateController(GameObject Controller, Transform item, Vector3 scale, Vector3 angle)
        {
            Controller = Instantiate(Controller, item.transform);
            Controller.transform.localScale = scale;
            Controller.transform.localEulerAngles = angle;
            Controller.AddComponent<MeshCollider>().sharedMesh = Controller.GetComponent<MeshFilter>().mesh;
            Controller.GetComponent<MeshCollider>().convex = true;
            Controller.GetComponent<MeshCollider>().isTrigger = true;
            ColorizeController(Controller);
            if (Controller.name.Contains("Thumb") | Controller.name.Contains("Index") | Controller.name.Contains("Middle") | Controller.name.Contains("Ring") | Controller.name.Contains("Pinky"))
            {
                Controller.transform.localScale *= 0.1f;
            }
        }

        public void OnEnable()
        {
            Selection.onSelectionChanged.AddListener(OnSelectionChange);
            GlobalState.Animation.onFrameEvent.AddListener(OnFrameChange);
            GlobalState.ObjectMovingEvent.AddListener(MovedObject);

            PoseButton.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", UseTPose ? Color.blue : Color.red);
            PickerGizmo.gameObject.SetActive(false);

            foreach (GameObject item in Selection.SelectedObjects)
            {
                if (item.TryGetComponent(out RigController controller))
                {
                    CreatePickerClone(controller);
                    break;
                }
            }
        }

        public void OnDisable()
        {
            Selection.onSelectionChanged.RemoveListener(OnSelectionChange);
            GlobalState.Animation.onFrameEvent.RemoveListener(OnFrameChange);
            GlobalState.ObjectMovingEvent.RemoveListener(MovedObject);
        }

        private void MovedObject(GameObject movedObject)
        {
            if (UseTPose) return;
            if (Target == null) return;
            if (movedObject == PickerClone) return;
            if (movedObject.transform.IsChildOf(Target.transform) || movedObject.transform.IsChildOf(PickerClone.transform))
            {
                foreach (KeyValuePair<GameObject, GameObject> items in CloneToTarget)
                {
                    if (items.Key == PickerClone) continue;
                    if (movedObject == items.Value)
                    {
                        items.Key.transform.localPosition = items.Value.transform.localPosition;
                        items.Key.transform.localRotation = items.Value.transform.localRotation;
                        items.Key.transform.localScale = items.Value.transform.localScale;
                    }
                    else if (movedObject == items.Key)
                    {
                        items.Value.transform.localPosition = items.Key.transform.localPosition;
                        items.Value.transform.localRotation = items.Key.transform.localRotation;
                        items.Value.transform.localScale = items.Key.transform.localScale;
                    }
                }
                controllers.ForEach(x => x.MoveController());
            }
        }

        private void OnFrameChange(int frame)
        {
            if (UseTPose) return;
            Vector3 rootPosition = Vector3.zero;
            foreach (KeyValuePair<GameObject, GameObject> items in CloneToTarget)
            {
                if (items.Value == Target) continue;
                if (items.Key == null || items.Value == null) continue;
                if (items.Value == root) rootPosition = items.Key.transform.position;
                AnimationSet anim = GlobalState.Animation.GetObjectAnimation(items.Value);
                if (anim != null)
                {
                    if (items.Key == null) Debug.Log(items.Value);
                    anim.EvaluateTransform(frame, items.Key.transform);
                }
                if (items.Value == root) items.Key.transform.position = rootPosition;
            }
        }

        public void ResetTPose(bool tPose)
        {
            if (!tPose)
            {
                controllers.ForEach(x =>
                {
                    if (x.isPickerController)
                    {
                        x.isTPose = false;
                        x.CopiePairedController();
                    }
                });
                directController.ForEach(x =>
                {
                    if (x.isPickerController)
                    {
                        x.isTPose = false;
                        x.CopiePairedController();
                    }
                });
                return;
            }
            foreach (RigConstraintController controller in controllers)
            {
                if (controller.isPickerController)
                {
                    controller.ResetPosition(applyToPair: false);
                    controller.isTPose = true;
                }
            }
            foreach (DirectController controller in directController)
            {
                if (controller.isPickerController)
                {
                    controller.ResetPosition(applyToPair: false);
                    controller.isTPose = true;
                }
            }
        }

        public void RefreshPosition()
        {
            if (UseTPose) return;
            foreach (KeyValuePair<GameObject, GameObject> items in CloneToTarget)
            {
                if (items.Key == PickerClone) continue;
                else
                {
                    items.Key.transform.localPosition = items.Value.transform.localPosition;
                    items.Key.transform.localRotation = items.Value.transform.localRotation;
                }
            }
        }

        public void OnSelectionChange(HashSet<GameObject> previousSelectedObjects, HashSet<GameObject> selectedObjects)
        {
            if (previousSelectedObjects.Contains(Target)) ClearPickerClone();
            RigController rigController = null;
            foreach (GameObject obj in selectedObjects)
            {
                obj.TryGetComponent<RigController>(out rigController);
            }
            if (rigController != null && rigController.gameObject != Target)
            {
                CreatePickerClone(rigController);
            }
        }

        private void CreatePickerClone(RigController rigController)
        {
            ClearPickerClone();
            Target = rigController.gameObject;
            root = rigController.RootObject.gameObject;

            CloneToTarget = new Dictionary<GameObject, GameObject>();
            PickerClone = Instantiate(rigController.gameObject, transform);
            PickerClone.layer = 17;
            PickerClone.transform.localScale = rigController.transform.localScale;
            PickerClone.tag = "Untagged";

            PickerClone.GetComponent<BoxCollider>().enabled = false;

            float xRatio = 0.075f;// pickerCollider.size.x / cloneCollider.size.x;
            float yRatio = 0.075f;// pickerCollider.size.y / cloneCollider.size.y;
            float zRatio = 0.075f;// pickerCollider.size.z / cloneCollider.size.z;

            PickerClone.transform.localPosition = Vector3.zero;// PickerClone.transform.TransformVector(cloneCollider.center) + pickerCollider.center;
            PickerClone.transform.localScale = new Vector3(-xRatio, yRatio, zRatio);
            PickerClone.transform.localRotation = Quaternion.identity;
            RecursiveMaping(rigController.transform, PickerClone.transform);

            Rigidbody body = PickerClone.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            ResetTPose(UseTPose);
        }

        public void RecursiveMaping(Transform target, Transform clone)
        {
            clone.gameObject.layer = 17;
            CloneToTarget.Add(clone.gameObject, target.gameObject);
            if (clone.TryGetComponent(out RigConstraintController controller))
            {
                if (clone.TryGetComponent(out MeshRenderer renderer)) renderer.enabled = true;
                if (clone.TryGetComponent(out MeshCollider collider)) collider.enabled = true;
                controllers.Add(controller);
                controllers.Add(target.GetComponent<RigConstraintController>());
                controller.isPickerController = true;
                controller.isTPose = UseTPose;
                RigConstraintController originalController = target.GetComponent<RigConstraintController>();
                controller.pairedController = originalController;
                originalController.pairedController = controller;
            }
            if (clone.TryGetComponent(out JointController joint))
            {
                joint.LinkJoint = target.GetComponent<JointController>();
                target.GetComponent<JointController>().LinkJoint = joint;
            }
            if (clone.TryGetComponent(out DirectController dController))
            {
                if (clone.TryGetComponent(out Renderer dRenderer)) dRenderer.enabled = false;
                if (clone.TryGetComponent(out Collider dCollider)) dCollider.enabled = false;
                DirectController targetDirect = target.GetComponent<DirectController>();
                directController.Add(dController);
                dController.isPickerController = true;
                dController.pairedController = targetDirect;
                targetDirect.pairedController = dController;
            }
            for (int i = 0; i < target.childCount; i++)
            {
                RecursiveMaping(target.GetChild(i), clone.GetChild(i));
            }
        }

        private void ClearPickerClone()
        {
            PickerTool.SelectEmpty();
            foreach (KeyValuePair<GameObject, GameObject> items in CloneToTarget)
            {
                if (items.Value != null && items.Value.TryGetComponent(out JointController joint)) joint.LinkJoint = null;
                Destroy(items.Key);
            }
            controllers.Clear();
            directController.Clear();
            Target = null;
            PickerClone = null;
            CloneToTarget.Clear();
        }

        public void AutoSwitchOnTool()
        {
            if (locked)
            {
                OutAndLocked = false;
                return;
            }
            previousTool = ToolsUIManager.Instance.CurrentTool == "Lobby" ? "Selector" : ToolsUIManager.Instance.CurrentTool;
            ToolsUIManager.Instance.ChangeTool("Picker");
        }

        public void AutoSwitchOffTool()
        {
            if (locked)
            {
                OutAndLocked = true;
                return;
            }
            ToolsUIManager.Instance.ChangeTool("Animation");
            OutAndLocked = false;
        }

        public void RotateClone(Vector2 direction)
        {
            if (PickerClone == null) return;
            Vector3 dir = new Vector3(direction.x, 0, direction.y);
            dir = transform.TransformDirection(dir);
            PickerClone.transform.forward = -dir;
        }

    }

}