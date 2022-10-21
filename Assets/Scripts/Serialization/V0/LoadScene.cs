using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRtist.Serialization.V0
{
    [CreateAssetMenu(fileName = "LoadSceneV0", menuName = "VrTist/V0/loader")]
    public class LoadScene : LoadSceneAbstract
    {
        public SceneData DataScene;
        private readonly Dictionary<string, Mesh> loadedMeshes = new Dictionary<string, Mesh>();
        private readonly List<CameraController> loadedCameras = new List<CameraController>();
        public List<CameraController> LoadedCameras { get { return loadedCameras; } }

        private readonly Dictionary<GameObject, SkinMeshData> loadedSkinMeshes = new Dictionary<GameObject, SkinMeshData>();
        private readonly Dictionary<GameObject, Material[]> skinMeshMaterials = new Dictionary<GameObject, Material[]>();
        private Transform rootTransform;


        public override void Load(string path)
        {
            loadedMeshes.Clear();
            loadedCameras.Clear();
            loadedSkinMeshes.Clear();
            skinMeshMaterials.Clear();

            DataScene = new SceneData();
            SerializationManager.Load(path, DataScene);
        }

        public override void LoadPlayerData(Transform cameraRig)
        {
            cameraRig.localPosition = DataScene.playerData.position;
            cameraRig.localRotation = DataScene.playerData.rotation;
            GlobalState.WorldScale = DataScene.playerData.scale;
            cameraRig.localScale = Vector3.one * (1f / DataScene.playerData.scale);
            Camera.main.nearClipPlane = 0.1f * cameraRig.localScale.x;
            Camera.main.farClipPlane = 1000f * cameraRig.localScale.x;
        }

        public override void LoadObjects(Transform root)
        {
            rootTransform = root;
            foreach (ObjectData data in DataScene.objects)
            {
                LoadObject(data);
            }
        }

        private void LoadObject(ObjectData data)
        {
            GameObject gobject;
            string absoluteMeshPath;
            Transform importedParent = null;

            // Check for import
            if (data.isImported)
            {
                try
                {
                    importedParent = new GameObject("__VRtist_tmp_load__").transform;
                    absoluteMeshPath = data.meshPath;
                    // Don't use async import since we may reference the game object for animations or constraints
                    // and the object must be loaded before we do so
                    GlobalState.GeometryImporter.ImportObject(absoluteMeshPath, importedParent, true);
                    if (importedParent.childCount == 0)
                        return;
                    gobject = importedParent.GetChild(0).gameObject;
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to load external object: " + e.Message);
                    return;
                }
            }
            else
            {
                absoluteMeshPath = SaveManager.Instance.GetSaveFolderPath(SaveManager.Instance.CurrentProjectName) + data.meshPath;
                gobject = new GameObject(data.name);
            }

            LoadCommonData(gobject, data);
            gobject.name = data.name;

            // Mesh
            if (null != data.meshPath && data.meshPath.Length > 0)
            {
                if (!data.isImported && !data.isSkinMesh)
                {
                    if (!loadedMeshes.TryGetValue(absoluteMeshPath, out Mesh mesh))
                    {
                        MeshData meshData = new MeshData();
                        SerializationManager.Load(absoluteMeshPath, meshData);
                        mesh = meshData.CreateMesh();
                        loadedMeshes.Add(absoluteMeshPath, mesh);
                    }
                    gobject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    gobject.AddComponent<MeshRenderer>().materials = LoadMaterials(data);
                    gobject.AddComponent<MeshCollider>();
                }
                if (data.isSkinMesh)
                {
                    SkinMeshData skinData = new SkinMeshData();
                    SerializationManager.Load(absoluteMeshPath, skinData);
                    loadedSkinMeshes.Add(gobject, skinData);
                    skinMeshMaterials.Add(gobject, LoadMaterials(data));
                }

                if (!data.visible)
                {
                    foreach (Component component in gobject.GetComponents<Component>())
                    {
                        Type componentType = component.GetType();
                        var prop = componentType.GetProperty("enabled");
                        if (null != prop)
                        {
                            prop.SetValue(component, data.visible);
                        }
                    }
                }
            }

            SceneManager.AddObject(gobject);
            if (data.parentPath.Length > 0)
                SceneManager.SetObjectParent(gobject, rootTransform.Find(data.parentPath).gameObject);

            if (data.isImported)
            {
                if (!gobject.TryGetComponent<ParametersController>(out ParametersController controller))
                {
                    controller = gobject.AddComponent<ParametersController>();
                }
                controller.isImported = true;
                controller.importPath = data.meshPath;
                if (null != importedParent)
                    Destroy(importedParent.gameObject);
            }
        }

        private void LoadCommonData(GameObject gobject, ObjectData data)
        {
            if (null != data.tag && data.tag.Length > 0)
            {
                gobject.tag = data.tag;
            }

            gobject.transform.localPosition = data.position;
            gobject.transform.localRotation = data.rotation;
            gobject.transform.localScale = data.scale;
            gobject.name = data.name;

            if (data.lockPosition || data.lockRotation || data.lockScale)
            {
                ParametersController controller = gobject.GetComponent<ParametersController>();
                if (null == controller)
                    controller = gobject.AddComponent<ParametersController>();

                controller.lockPosition = data.lockPosition;
                controller.lockRotation = data.lockRotation;
                controller.lockScale = data.lockScale;
            }
        }

        private Material[] LoadMaterials(ObjectData data)
        {
            Material[] materials = new Material[data.materialsData.Count];
            for (int i = 0; i < data.materialsData.Count; ++i)
            {
                materials[i] = data.materialsData[i].CreateMaterial(SaveManager.Instance.GetSaveFolderPath(SaveManager.Instance.CurrentProjectName));
            }
            return materials;
        }

        public override void LoadLights()
        {
            foreach (LightData light in DataScene.lights)
            {
                LoadLight(light);
            }
        }

        private void LoadLight(LightData data)
        {
            GameObject lightPrefab = null;
            switch (data.lightType)
            {
                case LightType.Directional:
                    lightPrefab = ResourceManager.GetPrefab(PrefabID.SunLight);
                    break;
                case LightType.Spot:
                    lightPrefab = ResourceManager.GetPrefab(PrefabID.SpotLight);
                    break;
                case LightType.Point:
                    lightPrefab = ResourceManager.GetPrefab(PrefabID.PointLight);
                    break;
            }

            if (lightPrefab)
            {
                GameObject newPrefab = SceneManager.InstantiateUnityPrefab(lightPrefab);
                GameObject newObject = SceneManager.AddObject(newPrefab);

                if (data.parentPath.Length > 0)
                    SceneManager.SetObjectParent(newObject, rootTransform.Find(data.parentPath).gameObject);

                LoadCommonData(newObject, data);

                LightController controller = newObject.GetComponent<LightController>();
                controller.Intensity = data.intensity;
                controller.minIntensity = data.minIntensity;
                controller.maxIntensity = data.maxIntensity;
                controller.Color = data.color;
                controller.CastShadows = data.castShadows;
                controller.ShadowNearPlane = data.near;
                controller.Range = data.range;
                controller.minRange = data.minRange;
                controller.maxRange = data.maxRange;
                controller.OuterAngle = data.outerAngle;
                controller.Sharpness = data.sharpness;
            }
        }

        public override void LoadCameras()
        {
            foreach (CameraData data in DataScene.cameras)
            {
                LoadCamera(data);
            }
        }

        private void LoadCamera(CameraData data)
        {
            GameObject cameraPrefab = ResourceManager.GetPrefab(PrefabID.Camera);

            GameObject newPrefab = SceneManager.InstantiateUnityPrefab(cameraPrefab);
            GameObject newObject = SceneManager.AddObject(newPrefab);
            if (data.parentPath.Length > 0)
                SceneManager.SetObjectParent(newObject, rootTransform.Find(data.parentPath).gameObject);

            LoadCommonData(newObject, data);

            CameraController controller = newObject.GetComponent<CameraController>();
            controller.enableDOF = data.enableDOF;
            if (controller.enableDOF)
                controller.CreateColimator();

            controller.focal = data.focal;
            controller.Focus = data.focus;
            controller.aperture = data.aperture;
            controller.near = data.near;
            controller.far = data.far;
            controller.filmHeight = data.filmHeight;
            controller.filmWidth = data.filmWidth;
            controller.gateFit = (Camera.GateFitMode)data.gateFit;

            loadedCameras.Add(controller);
        }

        public override void LoadSkinMeshes()
        {
            foreach (KeyValuePair<GameObject, SkinMeshData> pair in loadedSkinMeshes)
            {
                SkinnedMeshRenderer renderer = pair.Key.AddComponent<SkinnedMeshRenderer>();
                pair.Value.SetSkinnedMeshRenderer(renderer, pair.Key.transform.parent);
                renderer.materials = skinMeshMaterials[pair.Key];
            }
        }

        public override void LoadAnimations()
        {
            foreach (AnimationData data in DataScene.animations)
            {
                LoadAnimation(data);
            }
        }

        private void LoadAnimation(AnimationData data)
        {
            Transform animTransform = rootTransform.Find(data.objectPath);
            if (null == animTransform)
            {
                Debug.LogWarning($"Object name not found for animation: {data.objectPath}");
                return;
            }
            GameObject gobject = animTransform.gameObject;

            // Create animation
            AnimationSet animSet = new AnimationSet(gobject);
            foreach (CurveData curve in data.curves)
            {
                List<AnimationKey> keys = new List<AnimationKey>();
                foreach (KeyframeData keyData in curve.keyframes)
                {
                    keys.Add(new AnimationKey(keyData.frame, keyData.value, keyData.interpolation, keyData.inTangent, keyData.outTangent));
                }

                animSet.SetCurve(curve.property, keys);
            }
            SceneManager.SetObjectAnimations(gobject, animSet);
        }

        public override void LoadRigs()
        {
            foreach (RigData data in DataScene.rigs)
            {
                LoadRig(data);
            }
        }

        private void LoadRig(RigData data)
        {
            GameObject obj = rootTransform.Find(data.ObjectName).gameObject;
            if (null != obj)
            {
                GameObject rendererObj = obj.transform.Find(data.meshPath).gameObject;
                if (null != rendererObj)
                {
                    if (rendererObj.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer renderer))
                    {
                        RigController controller = obj.AddComponent<RigController>();
                        controller.SkinMesh = renderer;
                        controller.RootObject = renderer.rootBone;

                        obj.tag = "PhysicObject";
                        BoxCollider objectCollider = obj.AddComponent<BoxCollider>();
                        objectCollider.center = renderer.bounds.center;
                        objectCollider.size = renderer.bounds.size;
                        renderer.updateWhenOffscreen = true;
                        controller.Collider = objectCollider;

                        GlobalState.GeometryImporter.GenerateImportSkeleton(controller.RootObject, controller);
                    }
                }
            }
        }

        public override void LoadConstraints()
        {
            foreach (ConstraintData data in DataScene.constraints)
            {
                LoadConstraint(data);
            }
        }

        private void LoadConstraint(ConstraintData data)
        {
            Transform sourceTransform = rootTransform.Find(data.source);
            if (null == sourceTransform)
            {
                Debug.LogWarning($"Object name not found for animation: {data.source}");
                return;
            }
            Transform targetTransform = rootTransform.Find(data.target);
            if (null == targetTransform)
            {
                Debug.LogWarning($"Object name not found for animation: {data.target}");
                return;
            }

            // Create constraint
            ConstraintManager.AddConstraint(sourceTransform.gameObject, targetTransform.gameObject, data.type);
        }

        public override void LoadShots()
        {
            foreach (ShotData data in DataScene.shots)
            {
                LoadShot(data);
            }
        }

        private void LoadShot(ShotData data)
        {
            GameObject cam = null;
            if (data.cameraName.Length > 0)
            {
                Transform cameraTransform = rootTransform.Find(data.cameraName);
                if (null == cameraTransform)
                {
                    Debug.LogWarning($"Object name not found for camera: {data.cameraName}");
                    return;
                }
                cam = cameraTransform.gameObject;
            }

            ShotManager.Instance.AddShot(new Shot
            {
                name = data.name,
                start = data.start,
                end = data.end,
                enabled = data.enabled,
                camera = cam
            });
        }

        public override SkySettings GetSkySettings()
        {
            return DataScene.skyData;
        }

        public override float GetFps()
        {
            return DataScene.fps;
        }

        public override int GetStartFrame()
        {
            return DataScene.startFrame;
        }

        public override int GetEndFrame()
        {
            return DataScene.endFrame;
        }

        public override int GetCurrentFrame()
        {
            return DataScene.currentFrame;
        }

        public override List<CameraController> GetCameraControllers()
        {
            return loadedCameras;
        }
    }
}