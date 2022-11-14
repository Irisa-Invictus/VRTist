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
namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/JsonReader", fileName = "JsonReader")]
    public class RigConfiguration : ScriptableObject
    {
        public Mesh mesh;
        public Material material;

        private Dictionary<string, JointController> joints = new Dictionary<string, JointController>();
        private Dictionary<string, Transform> bones = new Dictionary<string, Transform>();

        public void GenerateJoints(RigController rootController, Transform objectRoot, Dictionary<string, Transform> bones)
        {
            joints = new Dictionary<string, JointController>();
            this.bones = bones;
            List<Transform> path = new List<Transform>();
            ParseObject(rootController, objectRoot, false, false, ref path);
        }

        public void ParseObject(RigController rootController, Transform current, bool inRig, bool parentHasController, ref List<Transform> path)
        {
            if (current == rootController.RootObject) inRig = true;
            if (!bones.TryGetValue(current.name, out Transform tr) && current.childCount == 0) inRig = false;
            bool skipController = parentHasController && current.transform.localPosition.magnitude < 0.01f;
            if (inRig && !skipController)
            {
                JointController joint = current.gameObject.AddComponent<JointController>();
                joint.SetPathToRoot(rootController, path);
                joints.Add(current.name, joint);
                path.Add(current);

                joint.color = current.name.Contains("Left") ? Color.blue : current.name.Contains("Right") ? Color.green : Color.yellow;

                AddDirectController(current, joint);
                parentHasController = true;
            }
            else
                parentHasController = false;
            foreach (Transform child in current)
            {
                ParseObject(rootController, child, inRig, parentHasController, ref path);
            }
            if (inRig) path.Remove(current);
        }

        private void AddDirectController(Transform current, JointController joint)
        {
            DirectController directController = current.gameObject.AddComponent<DirectController>();
            directController.target = joint;
            directController.stiffness = 1;
            directController.ShowCurve = false;
            directController.LowerAngleBound = -Vector3.one * 360; ;
            directController.UpperAngleBound = Vector3.one * 360;
            directController.FreePosition = false;
            directController.SetStartPosition();
            MeshCollider collider = current.gameObject.AddComponent<MeshCollider>();
            collider.convex = true;
            directController.gameObject.layer = 21;
            directController.goalCollider = collider;
            directController.tag = "Controller";
            if (!current.TryGetComponent<MeshFilter>(out MeshFilter filter))
            {
                filter = current.gameObject.AddComponent<MeshFilter>();
            }
            if (!current.TryGetComponent(out MeshRenderer renderer))
            {
                renderer = current.gameObject.AddComponent<MeshRenderer>();
            }
            renderer.material = new Material(material);
            directController.MeshRenderer = renderer;
            directController.UseController(false);
            filter.mesh = mesh;
            collider.sharedMesh = mesh;
            collider.isTrigger = true;
        }
    }
}
