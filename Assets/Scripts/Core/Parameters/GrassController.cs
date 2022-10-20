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

using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    [RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))] // DEBUG
    public class GrassController : ParametersController
    {
        // DEBUG visualization
        public MeshFilter DEBUG_filter;
        public MeshRenderer DEBUG_render;
        public List<Vector3> DEBUG_positions = new List<Vector3>();
        public List<Color> DEBUG_colors = new List<Color>();
        public List<int> DEBUG_indices = new List<int>();
        public List<Vector3> DEBUG_normals = new List<Vector3>();
        public List<Vector2> DEBUG_uvs = new List<Vector2>();

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct SourceVertex
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector2 uv;
            public Vector3 color;
        }

        public List<SourceVertex> vertices = new List<SourceVertex>();
        public int numSourceVertices = 0;

        public Material material = default;
        public ComputeShader computeShader = default;

        // Blade
        [Header("Blade")]
        public float grassHeight = 0.2f;
        public float grassWidth = 0.02f;
        public float grassRandomHeight = 0.25f;
        [Range(0, 1)] public float bladeRadius = 0.1f;
        public float bladeForwardAmount = 0.1f;
        [Range(1, 4)] public float bladeCurveAmount = 2;

        // Wind
        [Header("Wind")]
        public float windSpeed = 1;
        public float windStrength = 0.01f;
        public float windMultiplierXX = 10.0f;
        public float windMultiplierXY = 5.0f;
        public float windMultiplierXZ = 5.0f;
        public float windMultiplierZX = 7.0f;
        public float windMultiplierZY = 8.0f;
        public float windMultiplierZZ = 9.0f;


        // Interactor
        [Header("Interactor")]
        public float affectRadius = 0.1f;
        public float affectStrength = 1;
        public Transform interactorXf;


        // LOD
        [Header("LOD")]
        public float minFadeDistance = 5;
        public float maxFadeDistance = 10;
        // Material
        [Header("Material")]
        public bool overrideMaterial;
        public Color topColor = new Color(1, 1, 0);
        public Color bottomColor = new Color(0, 1, 0);

        // Other
        [Header("Other")]
        public bool castShadow;
        public Transform cameraXf;

        private readonly int m_AllowedBladesPerVertex = 4;
        private readonly int m_AllowedSegmentsPerBlade = 5;

        // A state variable to help keep track of whether compute buffers have been set up
        private bool m_Initialized;
        // A compute buffer to hold vertex data of the source mesh
        private ComputeBuffer m_SourceVertBuffer;
        // A compute buffer to hold vertex data of the generated mesh
        private ComputeBuffer m_DrawBuffer;
        // A compute buffer to hold indirect draw arguments
        private ComputeBuffer m_ArgsBuffer;
        // Instantiate the shaders so data belong to their unique compute buffers
        private ComputeShader m_InstantiatedComputeShader;
        private Material m_InstantiatedMaterial;
        // The id of the kernel in the grass compute shader
        private int m_IdGrassKernel;
        // The x dispatch size for the grass compute shader
        private int m_DispatchSize;
        // The local bounds of the generated mesh
        private Bounds m_LocalBounds;

        // The size of one entry in the various compute buffers
        private const int SOURCE_VERT_STRIDE = sizeof(float) * (3 + 3 + 2 + 3);
        private const int DRAW_STRIDE = sizeof(float) * (3 + (3 + 2 + 3) * 3); // triangle normal + 3 * vertex(position+uv+color)
        private const int INDIRECT_ARGS_STRIDE = sizeof(int) * 4;

        // The data to reset the args buffer with every frame
        // 0: vertex count per draw instance. We will only use one instance
        // 1: instance count. One
        // 2: start vertex location if using a Graphics Buffer
        // 3: and start instance location if using a Graphics Buffer
        private int[] argsBufferReset = new int[] { 0, 2, 0, 0 }; // 2 instances for StereoInstancing.

        private void OnEnable()
        {
            // If initialized, call on disable to clean things up
            if (m_Initialized)
                OnDisable();
            
            if (computeShader == null || material == null)
                return;
            
            if (vertices.Count == 0)
                return;

            m_Initialized = true;

            // Instantiate the shaders so they can point to their own buffers
            m_InstantiatedComputeShader = Instantiate(computeShader);
            m_InstantiatedMaterial = Instantiate(material);

            InitResources();
        }

        private void OnDisable()
        {
            // Dispose of buffers and copied shaders here
            if (m_Initialized)
            {
                // If the application is not in play mode, we have to call DestroyImmediate
                if (Application.isPlaying)
                {
                    Destroy(m_InstantiatedComputeShader);
                    Destroy(m_InstantiatedMaterial);
                }
                else
                {
                    DestroyImmediate(m_InstantiatedComputeShader);
                    DestroyImmediate(m_InstantiatedMaterial);
                }

                ReleaseResources();
            }

            m_Initialized = false;
        }

        public override void CopyParameters(ParametersController sourceController)
        {
            lockPosition = sourceController.lockPosition;
            lockRotation = sourceController.lockRotation;
            lockScale = sourceController.lockScale;
        }

        private void InitResources()
        {
            if (vertices.Count == 0)
                return;

            // Each segment has two points
            int maxBladesPerVertex = Mathf.Max(1, m_AllowedBladesPerVertex);
            int maxSegmentsPerBlade = Mathf.Max(1, m_AllowedSegmentsPerBlade);
            int maxBladeTriangles = maxBladesPerVertex * ((maxSegmentsPerBlade - 1) * 2 + 1);

            // Create compute buffers
            // The stride is the size, in bytes, each object in the buffer takes up
            m_SourceVertBuffer = new ComputeBuffer(vertices.Count, SOURCE_VERT_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
            m_SourceVertBuffer.SetData(vertices);


            m_DrawBuffer = new ComputeBuffer(numSourceVertices * maxBladeTriangles, DRAW_STRIDE, ComputeBufferType.Append);
            m_DrawBuffer.SetCounterValue(0);

            m_ArgsBuffer = new ComputeBuffer(1, INDIRECT_ARGS_STRIDE, ComputeBufferType.IndirectArguments);

            // Cache the kernel IDs we will be dispatching
            m_IdGrassKernel = m_InstantiatedComputeShader.FindKernel("Main");

            // Set buffer data
            m_InstantiatedComputeShader.SetBuffer(m_IdGrassKernel, "_SourceVertices", m_SourceVertBuffer);
            m_InstantiatedComputeShader.SetBuffer(m_IdGrassKernel, "_GrassTriangles", m_DrawBuffer);
            m_InstantiatedComputeShader.SetBuffer(m_IdGrassKernel, "_IndirectArgsBuffer", m_ArgsBuffer);
            // Set vertex data
            m_InstantiatedComputeShader.SetInt("_NumSourceVertices", numSourceVertices);
            m_InstantiatedComputeShader.SetInt("_MaxBladesPerVertex", maxBladesPerVertex);
            m_InstantiatedComputeShader.SetInt("_MaxSegmentsPerBlade", maxSegmentsPerBlade);

            m_InstantiatedMaterial.SetBuffer("_GrassTriangles", m_DrawBuffer);
            m_InstantiatedMaterial.SetShaderPassEnabled("ShadowCaster", castShadow);

            // Calculate the number of threads to use. Get the thread size from the kernel
            // Then, divide the number of triangles by that size
            m_InstantiatedComputeShader.GetKernelThreadGroupSizes(m_IdGrassKernel, out uint threadGroupSize, out _, out _);
            m_DispatchSize = Mathf.CeilToInt((float)numSourceVertices / threadGroupSize);

            // Get the bounds of the source mesh and then expand by the maximum blade width and height
            m_LocalBounds = new Bounds(vertices[0].position, new Vector3(grassWidth, grassWidth, grassWidth));
            foreach (SourceVertex sv in vertices)
            {
                m_LocalBounds.Encapsulate(sv.position);
            }
            m_LocalBounds.Expand(Mathf.Max(grassHeight + grassRandomHeight, grassWidth));

            // TODO: use this to build a box collider????????????
        }

        private void UpdateResources()
        {
            if (vertices.Count == 0)
                return;

            if (m_SourceVertBuffer == null)
                return;

            m_SourceVertBuffer.SetData(vertices);
        }

        private void ReleaseResources()
        {
            // Release each buffer
            m_SourceVertBuffer?.Release();
            m_SourceVertBuffer = null;
            m_DrawBuffer?.Release();
            m_DrawBuffer = null;
            m_ArgsBuffer?.Release();
            m_ArgsBuffer = null;
        }

        private bool dirty = false;

        // LateUpdate is called after all Update calls
        private void LateUpdate()
        {
            // If in edit mode, we need to update the shaders each Update to make sure settings changes are applied
            // Don't worry, in edit mode, Update isn't called each frame
            //if (Application.isPlaying == false)
            //{
            //    OnDisable();
            //    OnEnable();
            //}

            // If not initialized, do nothing (creating zero-length buffer will crash)
            if (!m_Initialized)
            {
                OnEnable();

                // If still not initialized...
                if (!m_Initialized)
                    return;
            }

            if (m_SourceVertBuffer != null && m_SourceVertBuffer.count != vertices.Count)
            {
                ReleaseResources();
            }

            if (vertices.Count != 0 && m_SourceVertBuffer == null)
            {
                InitResources();
            }

            if (vertices.Count == 0)
                return;

            if (dirty)
            {
                UpdateResources();
                dirty = false;
            }

            // Clear the draw and indirect args buffers of last frame's data
            m_DrawBuffer.SetCounterValue(0);
            m_ArgsBuffer.SetData(argsBufferReset);

            // Transform the bounds to world space
            Bounds bounds = TransformBounds(m_LocalBounds);

            // Update the shader with frame specific data
            SetGrassData();

            // Dispatch the grass shader. It will run on the GPU
            m_InstantiatedComputeShader.Dispatch(m_IdGrassKernel, m_DispatchSize, 1, 1);

            // DrawProceduralIndirect queues a draw call up for our generated mesh
            Graphics.DrawProceduralIndirect(m_InstantiatedMaterial, bounds, MeshTopology.Triangles,
                m_ArgsBuffer, 0, null, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
        }

        private void SetGrassData()
        {
            // Compute Shader
            m_InstantiatedComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
            m_InstantiatedComputeShader.SetFloat("_Time", Time.time);
            if (cameraXf != null)
                m_InstantiatedComputeShader.SetVector("_CameraPositionWS", cameraXf.position);
            if (interactorXf != null)
                m_InstantiatedComputeShader.SetVector("_PositionMovingWS", interactorXf.position);

            m_InstantiatedComputeShader.SetFloat("_BaseGrassHeight", grassHeight);
            m_InstantiatedComputeShader.SetFloat("_BaseGrassWidth", grassWidth);
            m_InstantiatedComputeShader.SetFloat("_GrassRandomHeight", grassRandomHeight);
            
            m_InstantiatedComputeShader.SetFloat("_WindSpeed", windSpeed);
            m_InstantiatedComputeShader.SetFloat("_WindStrength", windStrength);
            m_InstantiatedComputeShader.SetFloat("_WindMultiplierXX", windMultiplierXX);
            m_InstantiatedComputeShader.SetFloat("_WindMultiplierXY", windMultiplierXY);
            m_InstantiatedComputeShader.SetFloat("_WindMultiplierXZ", windMultiplierXZ);
            m_InstantiatedComputeShader.SetFloat("_WindMultiplierZX", windMultiplierZX);
            m_InstantiatedComputeShader.SetFloat("_WindMultiplierZY", windMultiplierZY);
            m_InstantiatedComputeShader.SetFloat("_WindMultiplierZZ", windMultiplierZZ);

            m_InstantiatedComputeShader.SetFloat("_InteractorRadius", affectRadius);
            m_InstantiatedComputeShader.SetFloat("_InteractorStrength", affectStrength);

            m_InstantiatedComputeShader.SetFloat("_BladeRadius", bladeRadius);
            m_InstantiatedComputeShader.SetFloat("_BladeForward", bladeForwardAmount);
            m_InstantiatedComputeShader.SetFloat("_BladeCurve", Mathf.Max(0, bladeCurveAmount));

            m_InstantiatedComputeShader.SetFloat("_MinFadeDist", minFadeDistance);
            m_InstantiatedComputeShader.SetFloat("_MaxFadeDist", maxFadeDistance);

            // Material
            m_InstantiatedMaterial.SetFloat("_FogStartDistance", RenderSettings.fogStartDistance);
            m_InstantiatedMaterial.SetFloat("_FogEndDistance", RenderSettings.fogEndDistance);
            m_InstantiatedMaterial.SetMatrix("_GrassObjectToWorldMatrix", transform.localToWorldMatrix);
            if (overrideMaterial)
            {
                m_InstantiatedMaterial.SetColor("_TopColor", topColor);
                m_InstantiatedMaterial.SetColor("_BottomColor", bottomColor);
            }
        }

        // This applies the game object's transform to the local bounds
        // Code by benblo from https://answers.unity.com/questions/361275/cant-convert-bounds-from-world-coordinates-to-loca.html
        private Bounds TransformBounds(Bounds boundsOS)
        {
            var center = transform.TransformPoint(boundsOS.center);

            // transform the local extents' axes
            var extents = boundsOS.extents;
            var axisX = transform.TransformVector(extents.x, 0, 0);
            var axisY = transform.TransformVector(0, extents.y, 0);
            var axisZ = transform.TransformVector(0, 0, extents.z);

            // sum their absolute value to get the world extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }

        // DEBUG
        public void InitDebugData()
        {
            Material debugMaterial = ResourceManager.GetMaterial(MaterialID.ObjectOpaque);

            DEBUG_filter = GetComponent<MeshFilter>();
            DEBUG_render = GetComponent<MeshRenderer>();
            DEBUG_render.sharedMaterial = debugMaterial;
            DEBUG_render.material.SetColor("_BaseColor", Color.red);
            DEBUG_render.material.SetFloat("_Opacity", 1.0f);
            DEBUG_positions = new List<Vector3>();
            DEBUG_colors = new List<Color>();
            DEBUG_indices = new List<int>();
            DEBUG_normals = new List<Vector3>();
            DEBUG_uvs = new List<Vector2>();
        }


        public void AddPoint(Vector3 position, Vector3 normal, Vector2 uv, Color color)
        {
            Vector3 col = new Vector3(color.r, color.g, color.b);
            vertices.Add(new SourceVertex() { position = position, normal = normal, uv = uv, color = col });

            // Update DEBUG arrays & mesh
            DEBUG_positions.Add(position);
            DEBUG_normals.Add(normal);
            DEBUG_uvs.Add(uv);
            DEBUG_colors.Add(color);
            DEBUG_indices.Add(numSourceVertices);

            numSourceVertices++;

            dirty = true;
            //RebuildDebugMesh();
        }

        public void RemovePointAt(int index)
        {
            vertices.RemoveAt(index);

            // Update DEBUG arrays & mesh
            DEBUG_positions.RemoveAt(index);
            DEBUG_normals.RemoveAt(index);
            DEBUG_uvs.RemoveAt(index);
            DEBUG_colors.RemoveAt(index);
            DEBUG_indices.RemoveAt(index);
            for (int i = 0; i < DEBUG_indices.Count; i++)
            {
                DEBUG_indices[i] = i;
            }

            numSourceVertices--;

            dirty = true;
            //RebuildDebugMesh();
        }

        public void ModifyPointAt(int index, Color newColor, Vector2 newUV)
        {
            SourceVertex V = vertices[index];
            vertices[index] = new SourceVertex() { position = V.position, normal = V.normal, uv = newUV, color = new Vector3(newColor.r, newColor.g, newColor.b) };
            
            DEBUG_uvs[index] = newUV;
            DEBUG_colors[index] = newColor;

            dirty = true;
            //RebuildDebugMesh();
        }

        public void RebuildDebugMesh()
        {
            Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.name = "DEBUG Grass Mesh";
            mesh.SetVertices(DEBUG_positions);
            int[] indi = DEBUG_indices.ToArray();
            mesh.SetIndices(indi, MeshTopology.Points, 0);
            mesh.SetUVs(0, DEBUG_uvs);
            mesh.SetColors(DEBUG_colors);
            mesh.SetNormals(DEBUG_normals);
            DEBUG_filter.mesh = mesh;
        }

        public void Clear()
        {
            //vertices.Clear();
            vertices = new List<SourceVertex>();
            numSourceVertices = 0;
        }






    }
}
