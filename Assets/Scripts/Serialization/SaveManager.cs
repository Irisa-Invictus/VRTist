/* MIT License

 *

 * Copyright (c) 2021 Ubisoft

 * &

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

using System.IO;


using UnityEngine;


namespace VRtist.Serialization

{
    public class MeshInfo
    {
        public string relativePath;
        public string absolutePath;
        public Mesh mesh;
    }


    public class SkinMeshInfo
    {
        public string relativePath;
        public string absolutePath;
        public MeshInfo mesh;
        public SkinnedMeshRenderer skinMesh;
    }



    public class MaterialInfo
    {
        public string relativePath;
        public string absolutePath;
        public Material material;
    }

    /// <summary>
    /// Save current scene.
    /// Warning: this class has to be a monobehaviour in order to iterate transforms of the scene.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public Camera screenshotCamera;
        public RenderTexture cubeMapRT;
        public RenderTexture equiRectRT;

        private Transform cameraRig;
        private Transform rootTransform;

        private string defaultSaveFolder;
        public string DefaultSaveFolder { get { return defaultSaveFolder; } }
        private string saveFolder;
        public string SaveFolder { get { return saveFolder; } }
        private string currentProjectName;
        public string CurrentProjectName { get { return currentProjectName; } }



        private readonly string DEFAULT_PROJECT_NAME = "newProject";
        public SaveSceneAbstract Saver;
        public LoadSceneAbstract Loader;

        #region Singleton
        // ----------------------------------------------------------------------------------------
        // Singleton
        // ----------------------------------------------------------------------------------------
        private static SaveManager instance;

        public static SaveManager Instance
        {
            get
            {
                return instance;
            }
        }

        private void Awake()
        {
            if (null == instance)
            {
                instance = this;
            }

            defaultSaveFolder = saveFolder = Application.persistentDataPath + "/saves/";
            cameraRig = Utils.FindRootGameObject("Camera Rig").transform;
            rootTransform = Utils.FindWorld().transform.Find("RightHanded");
        }
        #endregion

        #region Path Management
        // ----------------------------------------------------------------------------------------
        // Path Management
        // ----------------------------------------------------------------------------------------

        private string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        public string NormalizeProjectName(string name)
        {
            return ReplaceInvalidChars(name);
        }

        private string GetScenePath(string projectName)
        {
            return saveFolder + projectName + "/scene.vrtist";
        }

        public void GetMeshPath(string projectName, string meshName, out string absolutePath, out string relativePath)
        {
            relativePath = ReplaceInvalidChars(meshName) + ".mesh";
            absolutePath = saveFolder + projectName + "/" + ReplaceInvalidChars(meshName) + ".mesh";
        }
        private string GetScreenshotPath(string projectName)
        {
            return saveFolder + projectName + "/thumbnail.png";
        }

        public void GetMaterialPath(string projectName, string materialName, out string absolutePath, out string relativePath)
        {
            relativePath = ReplaceInvalidChars(materialName) + "/";
            absolutePath = saveFolder + projectName + "/" + ReplaceInvalidChars(materialName) + "/";
        }

        public string GetSaveFolderPath(string projectName)
        {
            return saveFolder + projectName + "/";
        }

        public List<string> GetProjectThumbnailPaths()
        {
            List<string> paths = new List<string>();
            if (!Directory.Exists(saveFolder)) { return paths; }

            foreach (string directory in Directory.GetDirectories(saveFolder))
            {
                string thumbnail = Path.Combine(directory, "thumbnail.png");
                if (File.Exists(thumbnail))
                {
                    paths.Add(thumbnail);
                }
            }
            return paths;
        }

        public string GetNextValidProjectName()
        {
            string name = DEFAULT_PROJECT_NAME;
            if (!Directory.Exists(saveFolder)) { return name; }
            int number = 1;
            foreach (string directory in Directory.GetDirectories(saveFolder, $"{DEFAULT_PROJECT_NAME}*"))
            {
                string dirname = Path.GetFileName(directory);
                if (name == dirname)
                {
                    name = $"{DEFAULT_PROJECT_NAME}_{number,0:D3}";
                    ++number;
                }
            }

            return name;
        }

        #endregion


        #region Save
        // ----------------------------------------------------------------------------------------
        // Save
        // ----------------------------------------------------------------------------------------

        System.Diagnostics.Stopwatch stopwatch;
        System.Diagnostics.Stopwatch totalStopwatch;

        public void LogElapsedTime(string what, System.Diagnostics.Stopwatch timer)
        {
            TimeSpan ts = timer.Elapsed;
            string elapsedTime = String.Format("{0:00}m {1:00}s {2:00}ms", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Debug.Log($"{what}: {elapsedTime}");
        }

        public void Save(string projectName)
        {

            Debug.Log("start Save");
            totalStopwatch = new System.Diagnostics.Stopwatch();
            totalStopwatch.Start();


            // Pre save
            stopwatch = System.Diagnostics.Stopwatch.StartNew();

            GlobalState.Instance.messageBox.ShowMessage("Saving scene, please wait...");

            currentProjectName = projectName;
            Saver.ClearScene();
            stopwatch.Stop();
            LogElapsedTime("Pre Save", stopwatch);

            // Scene traversal
            stopwatch = System.Diagnostics.Stopwatch.StartNew();
            TraverseScene(rootTransform, "");
            stopwatch.Stop();
            LogElapsedTime($"Scene Traversal ({Saver.GetObjectsCount()} objects)", stopwatch);

            Debug.Log("set shot manager");
            // Retrieve shot manager data
            SetShotManagerData();

            Debug.Log("set anim");
            // Retrieve animation data
            SetAnimationsData();

            Debug.Log("set constraint");
            // Set constraints data
            SetConstraintsData();

            Debug.Log("set skybox");
            // Retrieve skybox
            Saver.SetSkyData(GlobalState.Instance.SkySettings);


            Debug.Log("set player");
            // Set player data
            Saver.SetPlayerData(cameraRig);

            Debug.Log("save scene");
            // Save scene on disk
            SaveScene();
            Debug.Log("save mesh");
            Saver.SaveMeshes();
            Debug.Log("save materials");
            Saver.SaveMaterials();
            Debug.Log("save screenshot");
            StartCoroutine(SaveScreenshot());

            totalStopwatch.Stop();
            LogElapsedTime("Total Time", totalStopwatch);

            SceneManager.sceneSavedEvent.Invoke();
            CommandManager.SetSceneDirty(false);
            GlobalState.Instance.messageBox.SetVisible(false);
            Debug.Log("Finished");
        }


        private void TraverseScene(Transform root, string parentPath)
        {
            foreach (Transform currentTransform in root)
            {
                if (currentTransform == SceneManager.BoundingBox)
                    continue;

                string path = parentPath;
                path += "/" + currentTransform.name;

                // Depending on its type (which controller we can find on it) create data objects to be serialized
                LightController lightController = currentTransform.GetComponent<LightController>();
                if (null != lightController)
                {
                    Saver.SetLightData(currentTransform, parentPath, path, lightController);
                    continue;
                }

                CameraController cameraController = currentTransform.GetComponent<CameraController>();
                if (null != cameraController)
                {
                    Saver.SetCameraData(currentTransform, parentPath, path, cameraController);
                    continue;
                }

                ColimatorController colimatorController = currentTransform.GetComponent<ColimatorController>();
                if (null != colimatorController)
                {
                    // Nothing to do here, ignore the object
                    continue;
                }

                RigController skinController = currentTransform.GetComponent<RigController>();
                if (null != skinController && !skinController.isImported)
                {
                    Saver.SetRigData(skinController);
                }

                // Do this one at the end, because other controllers inherits from ParametersController
                ParametersController controller = currentTransform.GetComponent<ParametersController>();
                bool isImported = Saver.SetCommonObjectData(currentTransform, parentPath, path, controller);

                // Serialize children
                if (!isImported)
                {
                    TraverseScene(currentTransform, path);
                }
            }
        }


        private void SaveScene()
        {
            stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Saver.Save(GetScenePath(currentProjectName), deleteFolder: true);
            stopwatch.Stop();
            LogElapsedTime($"Write Scene", stopwatch);
        }

        private IEnumerator SaveScreenshot()
        {
            yield return new WaitForEndOfFrame();
            screenshotCamera.gameObject.SetActive(true);

            // -- FIX ------------------------------
            // FIRST Render to RenderTarget is black. Do a fake render before the real render.
            RenderTexture fixRT = new RenderTexture(16, 16, 24);
            screenshotCamera.targetTexture = fixRT;
            screenshotCamera.Render();
            // -- ENDFIX ----------------------------

            screenshotCamera.RenderToCubemap(cubeMapRT);
            cubeMapRT.ConvertToEquirect(equiRectRT);
            Texture2D texture = new Texture2D(equiRectRT.width, equiRectRT.height);
            RenderTexture previousActiveRT = RenderTexture.active;
            RenderTexture.active = equiRectRT;
            texture.ReadPixels(new Rect(0, 0, equiRectRT.width, equiRectRT.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previousActiveRT;
            Utils.SavePNG(texture, GetScreenshotPath(currentProjectName));
            screenshotCamera.gameObject.SetActive(false);
        }

        private void SetShotManagerData()
        {
            foreach (Shot shot in ShotManager.Instance.shots)
            {
                Saver.SetShotData(shot, rootTransform);
            }
        }

        private void SetAnimationsData()
        {


            Saver.SetAnimationEngineData(AnimationEngine.Instance.fps, AnimationEngine.Instance.StartFrame, AnimationEngine.Instance.EndFrame, AnimationEngine.Instance.CurrentFrame);

            foreach (AnimationSet animSet in AnimationEngine.Instance.GetAllAnimations().Values)
            {
                Saver.SetAnimationData(animSet, rootTransform);
            }
        }

        private void SetConstraintsData()
        {
            foreach (Constraint constraint in ConstraintManager.GetAllConstraints())
            {
                Saver.SetConstraintData(constraint, rootTransform);
            }
        }
        #endregion

        #region Load
        // ----------------------------------------------------------------------------------------
        // Load
        // ----------------------------------------------------------------------------------------
        public void Load(string projectName, string saveFolderOverride = null)
        {
            if (null != saveFolderOverride)
                saveFolder = saveFolderOverride;

            bool gizmoVisible = GlobalState.Settings.DisplayGizmos;
            bool errorLoading = false;


            GlobalState.Instance.messageBox.ShowMessage("Loading scene, please wait...");
            GlobalState.SetDisplayGizmos(true);

            currentProjectName = projectName;
            GlobalState.Settings.ProjectName = projectName;

            // Clear current scene
            SceneManager.ClearScene();


            // ensure VRtist scene
            VRtistScene scene = new VRtistScene();
            SceneManager.SetSceneImpl(scene);
            GlobalState.SetClientId(null);

            try
            {
                // Load data from file
                string path = GetScenePath(projectName);
                Loader.Load(path);
            }
            catch (Exception e)
            {
                LoadingError("data from file", e);
                errorLoading = true;
            }

            try
            {
                // Position user
                Loader.LoadPlayerData(cameraRig);
            }
            catch (Exception e)
            {
                LoadingError("Player data ", e);
                errorLoading = true;
            }

            // Sky
            GlobalState.Instance.SkySettings = Loader.GetSkySettings();

            try
            {
                // Objects            
                Loader.LoadObjects(rootTransform);
            }
            catch (Exception e)
            {
                LoadingError("Objects ", e);
                errorLoading = true;
            }

            try
            {

                // Lights
                Loader.LoadLights();
            }
            catch (Exception e)
            {
                LoadingError("Lights ", e);
                errorLoading = true;
            }

            try
            {
                // Cameras
                Loader.LoadCameras();
            }
            catch (Exception e)
            {
                LoadingError("Cameras ", e);
                errorLoading = true;
            }

            try
            {
                Loader.LoadSkinMeshes();

            }
            catch (Exception e)
            {
                LoadingError("Skin mehses", e);
                errorLoading = true;
            }


            // Load animations & constraints
            AnimationEngine.Instance.fps = Loader.GetFps();
            AnimationEngine.Instance.StartFrame = Loader.GetStartFrame();
            AnimationEngine.Instance.EndFrame = Loader.GetEndFrame();

            try
            {

                Loader.LoadAnimations();
            }
            catch (Exception e)
            {
                LoadingError("Animations ", e);
                errorLoading = true;
            }
            try
            {

                Loader.LoadRigs();
            }
            catch (Exception e)
            {
                LoadingError("Rigs ", e);
                errorLoading = true;
            }
            try
            {
                Loader.LoadConstraints();
            }
            catch (Exception e)
            {
                LoadingError("Constraints ", e);
                errorLoading = true;
            }
            try
            {

                Loader.LoadShots();
            }
            catch (Exception e)
            {
                LoadingError("Shots ", e);
                errorLoading = true;
            }
            // Load shot manager


            ShotManager.Instance.FireChanged();

            AnimationEngine.Instance.CurrentFrame = Loader.GetCurrentFrame();

            // Load camera snapshots
            StartCoroutine(LoadCameraSnapshots());
            //}
            //catch (Exception e)
            //{
            //    GlobalState.Instance.messageBox.ShowMessage("Error loading file", 5f);
            //    errorLoading = true;
            //}
            //finally
            //{
            saveFolder = defaultSaveFolder;
            if (!gizmoVisible)
                GlobalState.SetDisplayGizmos(false);
            if (!errorLoading)
            {
                GlobalState.Instance.messageBox.SetVisible(false);
                SceneManager.sceneLoadedEvent.Invoke();
            }
            //}
        }


        private void LoadingError(string step, Exception e)
        {
            GlobalState.Instance.messageBox.ShowMessage("Error loading " + step, 5f);
            Debug.Log("error loading " + step + "  " + e.Message);
        }

        private IEnumerator LoadCameraSnapshots()
        {
            foreach (CameraController controller in Loader.GetCameraControllers())
            {
                yield return null;  // wait next frame
                CameraManager.Instance.ActiveCamera = controller.gameObject;
                yield return new WaitForEndOfFrame();
                controller.SetVirtualCamera(null);
            }
            CameraManager.Instance.ActiveCamera = null;
        }
        #endregion


        #region Delete

        // ----------------------------------------------------------------------------------------

        // Delete

        // ----------------------------------------------------------------------------------------


        public void Delete(string projectName)
        {
            string path = saveFolder + projectName;
            if (!Directory.Exists(path)) { return; }
            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to delete project " + projectName + ": " + e.Message);
            }
        }
        #endregion


        #region Duplicate
        // ----------------------------------------------------------------------------------------
        // Load
        // ----------------------------------------------------------------------------------------

        public void Duplicate(string projectName, string newName)
        {
            string srcPath = saveFolder + projectName;
            if (!Directory.Exists(srcPath))
            {
                Debug.LogError($"Failed to duplicate project {projectName}: project doesn't exist.");
                return;
            }

            string dstPath = saveFolder + newName;
            if (Directory.Exists(dstPath))
            {
                Debug.LogError($"Failed to duplicate project {projectName} as {newName}: a project already exists.");
                return;
            }

            DirectoryCopy(srcPath, dstPath);
        }


        private void DirectoryCopy(string srcPath, string dstPath)
        {
            DirectoryInfo directory = new DirectoryInfo(srcPath);
            Directory.CreateDirectory(dstPath);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = directory.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(dstPath, file.Name);
                file.CopyTo(tempPath, false);
            }

            // Copy subdirs
            DirectoryInfo[] subdirs = directory.GetDirectories();
            foreach (DirectoryInfo subdir in subdirs)
            {
                string tempPath = Path.Combine(dstPath, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath);
            }
        }
        #endregion
    }

}
