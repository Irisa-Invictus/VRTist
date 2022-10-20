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
        public bool AutoSwitch;
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
        public Material Invisible;
        private Vector3 oldPos;
        private Vector3 oldScale;
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

        public void ResetHands()
        {
            SkinnedMeshRenderer[] Renderers = PickerClone.transform.GetComponentsInChildren<SkinnedMeshRenderer>();
            SkinnedMeshRenderer[] OldRenderers = Target.GetComponentsInChildren<SkinnedMeshRenderer>();
            int incr = 0;
            foreach (var item in Renderers)
            {
                item.material = OldRenderers[incr].material;
                item.gameObject.GetComponentInChildren<Transform>().gameObject.SetActive(true);
                incr++;
            }
            Transform[] Rig = PickerClone.gameObject.GetComponentsInChildren<Transform>();
            //Debug.Log(Rig);
            foreach (var item in Rig)
            {
                if (item.name.Contains("mixamo") && !item.name.Contains("controller"))
                    item.gameObject.layer = 21;
                else if (item.name.Contains("controller"))
                {
                    item.gameObject.GetComponentInChildren<MeshRenderer>().enabled = true;
                }
                else
                {
                    item.gameObject.layer = 0;
                }
            }
            PickerClone.transform.localPosition = oldPos;
            PickerClone.transform.localScale = oldScale;
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



        public void Start()
        {
            Selection.onSelectionChanged.AddListener(OnSelectionChange);
            GlobalState.Animation.onFrameEvent.AddListener(OnFrameChange);
            GlobalState.ObjectMovingEvent.AddListener(MovedObject);

            PoseButton.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", UseTPose ? Color.blue : Color.red);
            PickerGizmo.gameObject.SetActive(false);
        }

        public void OnDisable()
        {
            Selection.onSelectionChanged.RemoveListener(OnSelectionChange);
            GlobalState.Animation.onFrameEvent.RemoveListener(OnFrameChange);
            GlobalState.ObjectMovingEvent.RemoveListener(MovedObject);
        }

        public void Update()
        {
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
            foreach (KeyValuePair<GameObject, GameObject> items in CloneToTarget)
            {
                if (items.Key == PickerClone) continue;
                if (tPose) items.Key.transform.localRotation = Quaternion.identity;
                else items.Key.transform.localRotation = items.Value.transform.localRotation;
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
            Target = rigController.gameObject;
            root = rigController.RootObject.gameObject;

            CloneToTarget = new Dictionary<GameObject, GameObject>();
            PickerClone = Instantiate(rigController.gameObject, transform);
            PickerClone.transform.localScale = rigController.transform.localScale;
            PickerClone.tag = "Untagged";

            BoxCollider cloneCollider = PickerClone.GetComponent<BoxCollider>();
            cloneCollider.isTrigger = true;
            BoxCollider pickerCollider = GetComponent<BoxCollider>();

            float xRatio = pickerCollider.size.x / cloneCollider.size.x * 0.5f;
            float yRatio = (pickerCollider.size.y - 0.1f) / cloneCollider.size.y * 0.5f;
            float zRatio = pickerCollider.size.z / cloneCollider.size.z * 0.5f;


            PickerClone.transform.localScale = new Vector3(-1, 1, 1) * Mathf.Min(new float[] { xRatio, yRatio, zRatio });
            PickerClone.transform.localPosition = PickerBase.transform.localPosition + new Vector3(0, 0.05f, 0);
            PickerClone.transform.localRotation = Quaternion.identity;
            RecursiveMaping(rigController.transform, PickerClone.transform);

            Rigidbody body = PickerClone.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            //AssignController();
            ResetTPose(UseTPose);

            Vector3 maxValues = Vector3.one * 0.5f;
            Vector3 minValues = Vector3.one * 0.5f;
            foreach (KeyValuePair<GameObject, GameObject> pair in CloneToTarget)
            {

                Vector3 localPosition = transform.InverseTransformPoint(pair.Key.transform.position);
                maxValues.x = Mathf.Max(maxValues.x, localPosition.x);
                maxValues.y = Mathf.Max(maxValues.y, localPosition.y);
                maxValues.z = Mathf.Max(maxValues.z, localPosition.z);
                minValues.x = Mathf.Min(minValues.x, localPosition.x);
                minValues.y = Mathf.Min(minValues.y, localPosition.y);
                minValues.z = Mathf.Min(minValues.z, localPosition.z);

            }
            BoxCollider thisCollider = GetComponent<BoxCollider>();
            thisCollider.center = new Vector3((maxValues.x + minValues.x) / 2f, (maxValues.y + minValues.y) / 2f, (maxValues.z + minValues.z) / 2f);
            thisCollider.size = new Vector3(Mathf.Max(maxValues.x, -minValues.x) * 2f, Mathf.Max(maxValues.y, -minValues.y) * 2f, Mathf.Max(maxValues.z, -minValues.z) * 2f);
        }

        public void RecursiveMaping(Transform target, Transform clone)
        {
            CloneToTarget.Add(clone.gameObject, target.gameObject);

            if (clone.TryGetComponent(out RigConstraintController controller))
            {
                if (clone.TryGetComponent(out MeshRenderer renderer)) renderer.enabled = true;
                if (clone.TryGetComponent(out MeshCollider collider)) collider.enabled = true;
                controllers.Add(controller);
                controllers.Add(target.GetComponent<RigConstraintController>());
            }
            if (clone.TryGetComponent<JointController>(out JointController joint))
            {
                joint.LinkJoint = target.GetComponent<JointController>();
                target.GetComponent<JointController>().LinkJoint = joint;
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
                if (items.Value.TryGetComponent(out JointController joint)) joint.LinkJoint = null;
                Destroy(items.Key);
            }
            controllers.Clear();
            Target = null;
            PickerClone = null;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!AutoSwitch) return;

            if (other.TryGetComponent<AnimationTrigger>(out AnimationTrigger animTrigger))
            {
                AutoSwitchOnTool();
            }
            if (other.TryGetComponent<SelectorTrigger>(out SelectorTrigger selectTrigger))
            {
                AutoSwitchOnTool();
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (!AutoSwitch) return;

            if (other.TryGetComponent<VRPickerSelector>(out VRPickerSelector picker))
            {
                AutoSwitchOffTool();
            }
        }

        public void AutoSwitchOnTool()
        {
            if (locked)
            {
                OutAndLocked = false;
                return;
            }
            previousTool = ToolsUIManager.Instance.CurrentTool == "Lobby" ? "Selector" : ToolsUIManager.Instance.CurrentTool;
            //Debug.Log("switch tool picker");
            ToolsUIManager.Instance.ChangeTool("Picker");
        }

        public void AutoSwitchOffTool()
        {
            if (locked)
            {
                OutAndLocked = true;
                return;
            }
            //Debug.Log("switch tool " + previousTool);
            ToolsUIManager.Instance.ChangeTool(previousTool);
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