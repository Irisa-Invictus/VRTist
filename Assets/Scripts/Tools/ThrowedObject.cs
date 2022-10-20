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

using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace VRtist
{
    public class ThrowedObject : MonoBehaviour
    {
        float timeout = 10f;
        float startTime;
        float epsilon = 0.01f;
        float scaleDuration = 0.2f;
        Vector3 force;
        float initialScale = 0.1f;
        float scale = 1f;

        readonly List<Vector3> startPositions = new List<Vector3>();
        readonly List<Rigidbody> rbs = new List<Rigidbody>();
        readonly List<MeshCollider> nonConvexMeshColliders = new List<MeshCollider>();

        void Start()
        {
            startTime = Time.time;
            transform.localScale = Vector3.one * initialScale;

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.isTrigger = false;
                if (collider.gameObject.TryGetComponent(out MeshCollider meshCollider))
                {
                    if (!meshCollider.convex)
                    {
                        meshCollider.convex = true;
                        nonConvexMeshColliders.Add(meshCollider);
                    }
                }
                Rigidbody rb = collider.gameObject.AddComponent<Rigidbody>();
                rb.AddForce(force, ForceMode.Impulse);
                rbs.Add(rb);

                startPositions.Add(rb.transform.position);
            }

            SoundManager.Instance.PlayUISound(SoundManager.Sounds.Spawn, force: true);

            StartCoroutine(Rescale());
        }

        IEnumerator Rescale()
        {
            float startTime = Time.time;
            float dt = 0f;
            while (dt < scaleDuration)
            {
                float s = Mathf.Lerp(initialScale, scale, dt / scaleDuration);
                transform.localScale = Vector3.one * s;
                dt = Time.time - startTime;
                yield return null;
            }
            transform.localScale = Vector3.one * scale;
        }

        void Update()
        {
            if (Time.time - startTime < scaleDuration) { return; }

            if (Time.time - startTime < timeout)
            {
                for (int i = 0; i < startPositions.Count; i++)
                {
                    Vector3 startPosition = startPositions[i];
                    Vector3 position = rbs[i].transform.position;
                    if (Vector3.Distance(startPosition, position) > epsilon)
                    {
                        return;
                    }
                }
            }
            StartCoroutine(DestroySelf());
        }

        IEnumerator DestroySelf()
        {
            foreach (var rb in rbs)
            {
                Destroy(rb);
            }
            while (true)
            {
                yield return new WaitForSeconds(1f);
                List<int> toRemove = new List<int>();
                for (int i = 0; i < nonConvexMeshColliders.Count; i++)
                {
                    MeshCollider collider = nonConvexMeshColliders[i];
                    if (!collider.TryGetComponent(out Rigidbody rb))
                    {
                        collider.convex = false;
                        toRemove.Add(i);
                    }
                }
                foreach (int index in toRemove)
                {
                    nonConvexMeshColliders.RemoveAt(index);
                }
                if (nonConvexMeshColliders.Count == 0)
                {
                    break;
                }
            }
            Destroy(this);
        }

        public void AddForce(Vector3 force)
        {
            this.force = force;
        }

        public void SetScale(float value)
        {
            scale = value;
        }
    }
}
