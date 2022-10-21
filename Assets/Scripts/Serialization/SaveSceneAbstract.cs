using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist.Serialization
{

    public abstract class SaveSceneAbstract : ScriptableObject
    {

        public abstract void ClearScene();
        public abstract void Save(string path, bool deleteFolder = false);

        public abstract void SetLightData(Transform trans, string parentPath, string path, LightController controller);

        public abstract void SetCameraData(Transform trans, string parentPath, string path, CameraController controller);

        public abstract bool SetCommonObjectData(Transform trans, string parentPath, string path, ParametersController controller);

        public abstract void SetRigData(RigController controller);

        public abstract void SetAnimationEngineData(float fps, int startFrame, int endFrame, int currentFrame);

        public abstract void SetAnimationData(AnimationSet animSet, Transform root);

        public abstract void SetPlayerData(Transform cameraRig);

        public abstract void SetConstraintData(Constraint constraint, Transform root);

        public abstract void SetShotData(Shot shot, Transform root);

        public abstract void SaveMeshes();

        public abstract void SaveMaterials();

        public abstract int GetObjectsCount();

        public abstract void SetSkyData(SkySettings settings);



    }
}
