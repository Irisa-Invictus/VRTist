using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace VRtist.Serialization.V1
{

    [CreateAssetMenu(fileName = "SaveSceneV1", menuName = "VrTist/V1/Saver")]
    public class SaveScene : SaveSceneAbstract
    {

        public SceneData DataScene;
        private readonly Dictionary<string, MaterialInfo> materials = new Dictionary<string, MaterialInfo>();  // all materials
        private readonly Dictionary<string, MeshInfo> meshes = new Dictionary<string, MeshInfo>();  // meshes to save in separated files
        private readonly Dictionary<string, SkinMeshInfo> skinMeshes = new Dictionary<string, SkinMeshInfo>();

        public override void ClearScene()
        {
            DataScene = new SceneData();
            materials.Clear();
            meshes.Clear();
            skinMeshes.Clear();
        }

        public override void Save(string path, bool deleteFolder = false)
        {
            SerializationManager.Save(path, DataScene, deleteFolder);
        }


        public override void SetLightData(Transform trans, string parentPath, string path, LightController controller)
        {
            LightData data = new LightData();
            SetCommonData(trans, parentPath, path, controller, data);
            data.lightType = controller.Type;

            data.intensity = controller.Intensity;

            data.minIntensity = controller.minIntensity;

            data.maxIntensity = controller.maxIntensity;

            data.color = controller.Color;

            data.castShadows = controller.CastShadows;

            data.near = controller.ShadowNearPlane;

            data.range = controller.Range;

            data.minRange = controller.minRange;

            data.maxRange = controller.maxRange;

            data.outerAngle = controller.OuterAngle;

            data.sharpness = controller.Sharpness;

            DataScene.lights.Add(data);
        }

        public override void SetCameraData(Transform trans, string parentPath, string path, CameraController controller)
        {
            CameraData data = new CameraData();
            SetCommonData(trans, parentPath, path, controller, data);
            data.focal = controller.focal;

            data.focus = controller.focus;

            data.aperture = controller.aperture;

            data.enableDOF = controller.enableDOF;

            data.near = controller.near;

            data.far = controller.far;

            data.filmHeight = controller.filmHeight;
            DataScene.cameras.Add(data);
        }

        public override bool SetCommonObjectData(Transform trans, string parentPath, string path, ParametersController controller)
        {
            ObjectData data = new ObjectData();
            SetCommonData(trans, parentPath, path, controller, data);

            try
            {
                SetObjectData(trans, controller, data);
                DataScene.objects.Add(data);
            }
            catch (Exception e)
            {
                Debug.Log("Failed to set object data: " + e.Message);
            }
            return data.isImported;
        }

        public override void SetRigData(RigController controller)
        {
            DataScene.rigs.Add(new RigData(controller));
        }

        public override void SetAnimationEngineData(float fps, int startFrame, int endFrame, int currentFrame)
        {
            DataScene.fps = fps;
            DataScene.startFrame = startFrame;
            DataScene.endFrame = endFrame;
            DataScene.currentFrame = currentFrame;
        }

        public override void SetAnimationData(AnimationSet animSet, Transform root)
        {
            AnimationData animData = new AnimationData();
            Utils.GetTransformRelativePathTo(animSet.transform, root, out animData.objectPath);
            foreach (Curve curve in animSet.curves.Values)
            {
                CurveData curveData = new CurveData
                {
                    property = curve.property
                };
                foreach (AnimationKey key in curve.keys)
                {
                    KeyframeData keyData = new KeyframeData
                    {
                        frame = key.frame,
                        value = key.value,
                        interpolation = key.interpolation,
                        inTangent = key.inTangent,
                        outTangent = key.outTangent
                    };
                    curveData.keyframes.Add(keyData);
                }
                animData.curves.Add(curveData);
            }
            DataScene.animations.Add(animData);
        }

        public override void SetPlayerData(Transform cameraRig)
        {
            DataScene.playerData = new PlayerData
            {
                position = cameraRig.localPosition,
                rotation = cameraRig.localRotation,
                scale = GlobalState.WorldScale
            };
        }

        public override void SetConstraintData(Constraint constraint, Transform root)
        {
            ConstraintData constraintData = new ConstraintData
            {
                type = constraint.constraintType
            };
            Utils.GetTransformRelativePathTo(constraint.gobject.transform, root, out constraintData.source);
            Utils.GetTransformRelativePathTo(constraint.target.transform, root, out constraintData.target);
            DataScene.constraints.Add(constraintData);
        }

        public override void SetShotData(Shot shot, Transform root)
        {
            ShotData shotData = new ShotData
            {
                name = shot.name,
                start = shot.start,
                end = shot.end,
                enabled = shot.enabled
            };
            shotData.cameraName = "";
            if (null != shot.camera)
                Utils.GetTransformRelativePathTo(shot.camera.transform, root, out shotData.cameraName);
            DataScene.shots.Add(shotData);
        }

        private void SetObjectData(Transform trans, ParametersController controller, ObjectData data)
        {
            // Mesh for non-imported objects
            if (null == controller || !controller.isImported)
            {
                MeshRenderer meshRenderer = trans.GetComponent<MeshRenderer>();
                MeshFilter meshFilter = trans.GetComponent<MeshFilter>();
                if (null != meshFilter && null != meshRenderer)
                {
                    // Materials
                    foreach (Material material in meshRenderer.materials)
                    {
                        string materialId = trans.name + "_" + material.name;
                        SaveManager.Instance.GetMaterialPath(SaveManager.Instance.CurrentProjectName, materialId, out string materialAbsolutePath, out string materialRelativePath);
                        MaterialInfo materialInfo = new MaterialInfo { relativePath = materialRelativePath, absolutePath = materialAbsolutePath, material = material };
                        if (!materials.ContainsKey(materialId))
                            materials.Add(materialId, materialInfo);
                        data.materialsData.Add(new MaterialData(materialInfo));
                    }
                    // Mesh
                    SaveManager.Instance.GetMeshPath(SaveManager.Instance.CurrentProjectName, meshFilter.sharedMesh.name, out string meshAbsolutePath, out string meshRelativePath);
                    meshes[meshRelativePath] = new MeshInfo { relativePath = meshRelativePath, absolutePath = meshAbsolutePath, mesh = meshFilter.sharedMesh };
                    data.meshPath = meshRelativePath;
                }
                data.isImported = false;
                if (trans.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer skinRenderer))
                {
                    foreach (Material material in skinRenderer.materials)
                    {
                        string materialId = trans.name + "_" + material.name;
                        SaveManager.Instance.GetMaterialPath(SaveManager.Instance.CurrentProjectName, materialId, out string materialAbsolutePath, out string materialRelativePath);
                        MaterialInfo materialInfo = new MaterialInfo { relativePath = materialRelativePath, absolutePath = materialAbsolutePath, material = material };
                        if (!materials.ContainsKey(materialId))
                            materials.Add(materialId, materialInfo);
                        data.materialsData.Add(new MaterialData(materialInfo));
                    }
                    SaveManager.Instance.GetMeshPath(SaveManager.Instance.CurrentProjectName, skinRenderer.sharedMesh.name, out string meshAbsolutePath, out string meshRelativePath);
                    MeshInfo meshI = new MeshInfo { absolutePath = meshAbsolutePath, relativePath = meshRelativePath, mesh = skinRenderer.sharedMesh };
                    skinMeshes[meshRelativePath] = new SkinMeshInfo { absolutePath = meshAbsolutePath, relativePath = meshRelativePath, skinMesh = skinRenderer, mesh = meshI };
                    data.meshPath = meshRelativePath;
                    data.isSkinMesh = true;
                }
            }
            else if (null != controller && controller.isImported)
            {
                data.meshPath = controller.importPath;
                data.isImported = true;
            }
        }

        private void SetCommonData(Transform trans, string parentPath, string path, ParametersController controller, ObjectData data)
        {
            data.name = trans.name;

            data.parentPath = parentPath == "" ? "" : parentPath.Substring(1);

            data.path = path.Substring(1);

            data.tag = trans.gameObject.tag;


            data.visible = true;

            MeshRenderer mesh = trans.GetComponent<MeshRenderer>();

            if (null != mesh && !mesh.enabled)

                data.visible = false;

            if (trans.gameObject.activeSelf == false)

                data.visible = false;


            // Transform

            data.position = trans.localPosition;

            data.rotation = trans.localRotation;

            data.scale = trans.localScale;


            if (null != controller)

            {

                data.lockPosition = controller.lockPosition;

                data.lockRotation = controller.lockRotation;

                data.lockScale = controller.lockScale;

            }

        }


        public override void SaveMeshes()
        {
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            foreach (var meshInfo in meshes.Values)

            {

                SerializationManager.Save(meshInfo.absolutePath, new MeshData(meshInfo));

            }

            foreach (var skinMesh in skinMeshes.Values)

            {

                SerializationManager.Save(skinMesh.absolutePath, new SkinMeshData(skinMesh));

            }

            timer.Stop();

            SaveManager.Instance.LogElapsedTime($"Write Meshes ({meshes.Count})", timer);
        }

        public override void SaveMaterials()
        {
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
            foreach (MaterialInfo materialInfo in materials.Values)
            {
                SaveMaterial(materialInfo);
            }
            timer.Stop();
            SaveManager.Instance.LogElapsedTime($"Write Materials ({meshes.Count})", timer);
        }

        private void SaveMaterial(MaterialInfo materialInfo)
        {
            string shaderName = materialInfo.material.shader.name;
            if (shaderName != "VRtist/ObjectOpaque" &&
                shaderName != "VRtist/ObjectTransparent" &&
                shaderName != "VRtist/ObjectOpaqueUnlit" &&
                shaderName != "VRtist/ObjectTransparentUnlit")
            {
                Debug.LogWarning($"Unsupported material {shaderName}. Expected VRtist/Object*.");
                return;
            }

            SaveTexture("_ColorMap", "_UseColorMap", "color", materialInfo);
            SaveTexture("_NormalMap", "_UseNormalMap", "normal", materialInfo);
            SaveTexture("_MetallicMap", "_UseMetallicMap", "metallic", materialInfo);
            SaveTexture("_RoughnessMap", "_UseRoughnessMap", "roughness", materialInfo);
            SaveTexture("_EmissiveMap", "_UseEmissiveMap", "emissive", materialInfo);
            SaveTexture("_AoMap", "_UseAoMap", "ao", materialInfo);
            SaveTexture("_OpacityMap", "_UseOpacityMap", "opacity", materialInfo);
        }

        private void SaveTexture(string textureName, string boolName, string baseName, MaterialInfo materialInfo)
        {
            if (materialInfo.material.GetInt(boolName) == 1)
            {
                string path = materialInfo.absolutePath + baseName + ".tex";
                Texture2D texture = (Texture2D)materialInfo.material.GetTexture(textureName);
                TextureUtils.WriteRawTexture(path, texture);
            }
        }

        public override int GetObjectsCount()
        {
            return DataScene.objects.Count;
        }

        public override void SetSkyData(SkySettings settings)
        {
            DataScene.skyData = settings;
        }
    }
}