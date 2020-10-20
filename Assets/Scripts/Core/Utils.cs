﻿using System;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace VRtist
{   
    public class Utils
    {
        static Material paintMaterial = null;

        public static GameObject FindWorld()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == "World")
                {
                    return roots[i];
                }
            }
            return null;
        }

        public static Volume FindVolume()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == "Volume")
                {
                    return roots[i].GetComponent<Volume>();
                }
            }
            return null;
        }

        public static GameObject FindGameObject(string name)
        {
            GameObject world = Utils.FindWorld();
            if (!world)
                return null;

            int childrenCount = world.transform.childCount;
            for (int i = 0; i < childrenCount; i++)
            {
                GameObject child = world.transform.GetChild(i).gameObject;
                if (child.name == name)
                    return child;
            }

            return null;
        }


        public static GameObject GetRoot(GameObject gobject)
        {
            ParametersController parametersController = gobject.GetComponentInParent<ParametersController>();
            if (!parametersController)
                return null;
            return parametersController.gameObject;
        }

        public static string BuildTransformPath(GameObject gobject)
        {
            string res = "";
            while (gobject.GetComponent<ParametersController>() == null)
            {
                res = "/" + gobject.name + res;
                gobject = gobject.transform.parent.gameObject;
            }

            if (res.Length > 0)
                res = res.Substring(1, res.Length - 1);

            return res;
        }

        private static Regex readableNameRegex = new Regex(@"(?<basename>.+?)\.(?<hash>.+?)\.(?<number>\d+)", RegexOptions.Compiled);
        public static string GetReadableName(string name)
        {
            string readableName = name;
            MatchCollection matches = readableNameRegex.Matches(name);
            if (matches.Count == 1)
            {
                GroupCollection groups = matches[0].Groups;
                string baseName = groups["basename"].ToString();
                int number = Int32.Parse(groups["number"].Value);
                readableName = $"{baseName}.{number}";
            }
            return readableName;
        }

        public static GameObject CreatePaint(Transform parent, Color color)
        {
            GameObject intermediateParent = new GameObject();
            intermediateParent.transform.parent = parent;

            GameObject paint = new GameObject();
            paint.transform.parent = intermediateParent.transform;
            paint.name = SyncData.CreateUniqueName(paint, "Paint");
            intermediateParent.name = paint.name + "_parent";

            paint.transform.localPosition = Vector3.zero;
            paint.transform.localRotation = Quaternion.identity;
            paint.transform.localScale = Vector3.one;
            paint.tag = "PhysicObject";

            Mesh mesh = new Mesh();
            MeshFilter meshFilter = paint.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            MeshRenderer renderer = paint.AddComponent<MeshRenderer>();
            Material paintMaterial = NetGeometry.GetMaterial(MaterialType.Paint);
            renderer.sharedMaterial = paintMaterial;

            MaterialParameters parameters = new MaterialParameters();
            parameters.materialType = MaterialType.Paint;
            parameters.baseColor = color;

            NetGeometry.materialsParameters[SyncData.GetMaterialName(paint)] = parameters;
            Material instanceMaterial = renderer.material;
            NetGeometry.ApplyMaterialParameters(instanceMaterial, parameters);
            renderer.material = instanceMaterial;

            paint.AddComponent<MeshCollider>();
            PaintController paintController = paint.AddComponent<PaintController>();

            return paint;
        }

        public static RenderTexture CreateRenderTexture(int width, int height, int depth, RenderTextureFormat format, bool randomWrite)
        {
            RenderTexture renderTexture = new RenderTexture(width, height, depth, format);
            renderTexture.enableRandomWrite = randomWrite;
            renderTexture.Create();
            return renderTexture;
        }
        public static RenderTexture CreateRenderTexture(RenderTexture source)
        {
            return CreateRenderTexture(source.width, source.height, 0, source.format, true);
        }

        public static void TryDispose(System.IDisposable obj)
        {
            if (null == obj) { return; }
            obj.Dispose();
        }
        public static void TryDestroy(UnityEngine.Object obj)
        {
            if (null == obj) { return; }
            UnityEngine.Object.Destroy(obj);
        }

        public static void SwapBuffers(ref ComputeBuffer buf1, ref ComputeBuffer buf2)
        {
            var temp = buf1;
            buf1 = buf2;
            buf2 = temp;
        }

        public static Texture2D CopyRenderTextureToTexture(RenderTexture renderTexture)
        {
            TextureCreationFlags flags = TextureCreationFlags.None;
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, renderTexture.graphicsFormat, flags);

            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            //Graphics.CopyTexture(renderTexture, texture); doesn't work :(
            RenderTexture.active = activeRT;

            return texture;
        }

        public static void SavePNG(Texture2D texture, string path)
        {
            byte[] data = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, data);
        }
    }
}
