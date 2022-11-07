using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist.Serialization
{
    public abstract class LoadSceneAbstract : ScriptableObject
    {

        public abstract void Load(string path);

        public abstract void LoadPlayerData(Transform cameraRig);

        public abstract void LoadObjects(Transform root);

        public abstract void LoadLights();

        public abstract void LoadCameras();

        public abstract void LoadSkinMeshes();

        public abstract void LoadAnimations();

        public abstract void LoadRigs();

        public abstract void LoadConstraints();

        public abstract void LoadShots();

        public abstract SkySettings GetSkySettings();

        public abstract float GetFps();

        public abstract int GetStartFrame();

        public abstract int GetEndFrame();

        public abstract int GetCurrentFrame();

        public abstract List<CameraController> GetCameraControllers();

    }
}