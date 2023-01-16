﻿/* MIT License
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
using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{
    public enum AnimationState
    {
        Stopped,
        Preroll,
        AnimationRecording,
        Playing,
        VideoOutput
    };

    public enum Interpolation
    {
        Constant,
        Linear,
        Bezier,
        Other
    }


    public enum AnimatableProperty
    {
        PositionX, PositionY, PositionZ,
        RotationX, RotationY, RotationZ,
        ScaleX, ScaleY, ScaleZ,
        Power, ColorR, ColorG, ColorB,
        CameraFocal,
        CameraFocus,
        CameraAperture,
        //BlendShape,
        Unknown
    }

    public class AnimationKey
    {
        public AnimationKey(int frame, float value, Interpolation? interpolation = null, Vector2 inTangent = new Vector2(), Vector2 outTangent = new Vector2())
        {
            this.frame = frame;
            this.value = value;
            this.interpolation = interpolation ?? GlobalState.Settings.interpolation;
            this.inTangent = inTangent;
            this.outTangent = outTangent;
        }

        public AnimationKey(AnimationKey key)
        {
            frame = key.frame;
            value = key.value;
            interpolation = key.interpolation;
            inTangent = key.inTangent;
            outTangent = key.outTangent;
        }

        public int frame;
        public float value;
        public Vector2 inTangent;
        public Vector2 outTangent;
        public Interpolation interpolation;
    }

    /// <summary>
    /// Allow to hook the time of the animation engine.
    /// An example of use is the shot manager.
    /// </summary>
    public abstract class TimeHook
    {
        public abstract int HookTime(int frame);
    }

    /// <summary>
    /// Animation Engine.
    /// </summary>
    public class AnimationEngine : MonoBehaviour
    {
        // All animations
        readonly Dictionary<GameObject, AnimationSet> animations = new Dictionary<GameObject, AnimationSet>();
        readonly Dictionary<GameObject, AnimationSet> disabledAnimations = new Dictionary<GameObject, AnimationSet>();
        readonly Dictionary<GameObject, AnimationSet> recordingObjects = new Dictionary<GameObject, AnimationSet>();
        readonly Dictionary<GameObject, AnimationSet> oldAnimations = new Dictionary<GameObject, AnimationSet>();

        readonly List<TimeHook> timeHooks = new List<TimeHook>();
        public bool timeHooksEnabled = true;


        private const float defaultFPS = 24f;
        public float fps = defaultFPS;
        float playStartTime;
        int playStartFrame;

        private int startFrame = 1;
        public int StartFrame
        {
            get { return startFrame; }
            set
            {
                startFrame = value;
                if (startFrame >= endFrame - 1)
                    startFrame = endFrame - 1;
                if (startFrame < 0)
                    startFrame = 0;
                onRangeEvent.Invoke(new Vector2Int(startFrame, endFrame));
            }
        }

        private int endFrame = 250;
        public int EndFrame
        {
            get { return endFrame; }
            set
            {
                endFrame = value;
                if (endFrame <= (startFrame + 1))
                    endFrame = startFrame + 1;
                if (endFrame < 1)
                    endFrame = 1;
                onRangeEvent.Invoke(new Vector2Int(startFrame, endFrame));
            }
        }
        public bool loop = true;
        private int currentFrame = 1;
        public int CurrentFrame
        {
            get { return currentFrame; }
            set
            {
                currentFrame = Mathf.Clamp(value, localFirstFrame, localLastFrame);
                if (animationState != AnimationState.Playing && animationState != AnimationState.AnimationRecording)
                {
                    EvaluateAnimations();
                    onFrameEvent.Invoke(value);
                }
            }
        }

        public int localFirstFrame = 0;
        public int localLastFrame = 250;

        public bool autoKeyEnabled = false;

        public AnimationState animationState = AnimationState.Stopped;

        public IntChangedEvent onFrameEvent = new IntChangedEvent();
        public CurveChangedEvent onChangeCurve = new CurveChangedEvent();
        public Vector2IntChangedEvent onRangeEvent = new Vector2IntChangedEvent();
        public GameObjectChangedEvent onAddAnimation = new GameObjectChangedEvent();
        public GameObjectChangedEvent onRemoveAnimation = new GameObjectChangedEvent();
        public GameObjectChangedEvent onStartOffsetChanged = new GameObjectChangedEvent();
        public AnimationStateChangedEvent onAnimationStateEvent = new AnimationStateChangedEvent();


        public Countdown countdown = null;

        Interpolation preRecordInterpolation;

        // Singleton
        private static AnimationEngine instance = null;
        public static AnimationEngine Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GameObject.FindObjectOfType<AnimationEngine>();
                }
                return instance;
            }
        }

        void Awake()
        {
            instance = Instance;
        }

        private void Start()
        {
            onRangeEvent.AddListener(RecomputeCurvesCache);
            countdown.onCountdownFinished.AddListener(StartRecording);

            GlobalState.ObjectAddedEvent.AddListener(OnObjectAdded);
            GlobalState.ObjectRemovedEvent.AddListener(OnObjectRemoved);
        }

        public void RegisterTimeHook(TimeHook timeHook)
        {
            timeHooks.Add(timeHook);
        }

        public void UnregisterTimeHook(TimeHook timeHook)
        {
            timeHooks.Remove(timeHook);
        }

        private void Update()
        {
            // Find current time and frame & Animate objects
            if (animationState == AnimationState.Playing || animationState == AnimationState.AnimationRecording)
            {
                // Compute new frame
                float deltaTime = Time.time - playStartTime;
                int newFrame = TimeToFrame(deltaTime) + playStartFrame;

                if (animationState == AnimationState.Playing)
                {
                    int prevFrame = newFrame;
                    if (timeHooksEnabled)
                    {
                        foreach (TimeHook timeHook in timeHooks)
                        {
                            newFrame = timeHook.HookTime(newFrame);
                        }
                        if (prevFrame != newFrame)
                        {
                            playStartFrame = newFrame;
                            playStartTime = Time.time;
                        }
                    }
                }

                if (currentFrame != newFrame)
                {
                    if (newFrame > localLastFrame)
                    {
                        if (animationState == AnimationState.AnimationRecording)
                        {
                            // Stop recording when reaching the end of the timeline
                            newFrame = endFrame;
                            Pause();
                        }
                        else if (loop)
                        {
                            newFrame = localFirstFrame;
                            playStartFrame = localFirstFrame;
                            playStartTime = Time.time;
                        }
                    }

                    currentFrame = newFrame;
                    EvaluateAnimations();
                    onFrameEvent.Invoke(currentFrame);

                    // Record
                    if (animationState == AnimationState.AnimationRecording)
                    {
                        RecordFrame();
                    }
                }
            }

            if (animationState == AnimationState.VideoOutput)
            {
                int newFrame = currentFrame + 1;
                if (timeHooksEnabled)
                {
                    foreach (TimeHook timeHook in timeHooks)
                    {
                        newFrame = timeHook.HookTime(newFrame);
                    }
                }
                CurrentFrame = newFrame;
            }
        }

        public void CopyAnimation(GameObject source, GameObject target)
        {
            if (animations.TryGetValue(source, out AnimationSet sourceAnim))
            {
                AnimationSet targetAnim = new AnimationSet(target);
                foreach (KeyValuePair<AnimatableProperty, Curve> curve in sourceAnim.curves)
                {
                    targetAnim.SetCurve(curve.Key, curve.Value.keys);
                }
                SetObjectAnimations(target, targetAnim);
            }
            for (int i = 0; i < source.transform.childCount; i++)
            {
                CopyAnimation(source.transform.GetChild(i).gameObject, target.transform.GetChild(i).gameObject);
            }
        }

        void OnObjectAdded(GameObject gobject)
        {
            if (disabledAnimations.TryGetValue(gobject, out AnimationSet animationSet))
            {
                disabledAnimations.Remove(gobject);
                animations.Add(gobject, animationSet);
                animationSet.ComputeCache();
            }
        }
        void OnObjectRemoved(GameObject gobject)
        {
            if (animations.TryGetValue(gobject, out AnimationSet animationSet))
            {
                animations.Remove(gobject);
                disabledAnimations.Add(gobject, animationSet);
                animationSet.ClearCache();
            }
        }

        private void RecomputeCurvesCache(Vector2Int _)
        {
            foreach (AnimationSet animationSet in animations.Values)
            {
                animationSet.ComputeCache();
            }
        }

        public bool IsAnimating()
        {
            return animationState == AnimationState.Playing || animationState == AnimationState.AnimationRecording;
        }

        public void Clear()
        {
            foreach (GameObject gobject in animations.Keys)
            {
                onRemoveAnimation.Invoke(gobject);
            }
            animations.Clear();
            disabledAnimations.Clear();
            recordingObjects.Clear();
            oldAnimations.Clear();
            //fps = defaultFPS;
            StartFrame = 1;
            EndFrame = 250;
            CurrentFrame = 1;
        }

        public Dictionary<GameObject, AnimationSet> GetAllAnimations()
        {
            return animations;
        }

        private void EvaluateAnimations()
        {
            foreach (AnimationSet animationSet in animations.Values)
            {
                animationSet.EvaluateAnimation(CurrentFrame);
            }
        }


        public int TimeToFrame(float time)
        {
            return (int)(fps * time);
        }

        public float FrameToTime(int frame)
        {
            return (float)frame / fps;
        }

        public AnimationSet GetObjectAnimation(GameObject gobject)
        {
            animations.TryGetValue(gobject, out AnimationSet animationSet);
            return animationSet;
        }

        public void SetObjectAnimations(GameObject gobject, AnimationSet animationSet, bool callEvent = true)
        {
            animations[gobject] = animationSet;
            foreach (Curve curve in animationSet.curves.Values)
                curve.ComputeCache();
            if (callEvent) onAddAnimation.Invoke(gobject);
        }

        public bool ObjectHasKeyframeAt(GameObject gobject, int frame)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet) { return false; }

            foreach (var curve in animationSet.curves.Values)
            {
                if (curve.HasKeyAt(frame)) { return true; }
            }
            return false;
        }

        public void ClearAnimations(GameObject gobject, bool callEvent = true)
        {
            if (animations.Remove(gobject) && callEvent)
                onRemoveAnimation.Invoke(gobject);
        }

        public void MoveKeyframe(GameObject gobject, AnimatableProperty property, int frame, int newFrame)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
                return;

            animationSet.GetCurve(property).MoveKey(frame, newFrame);
            if (!IsAnimating())
                EvaluateAnimations();
        }

        public AnimationSet GetOrCreateObjectAnimation(GameObject gobject)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
            {
                animationSet = new AnimationSet(gobject);
                animations.Add(gobject, animationSet);
            }
            return animationSet;
        }


        // To be used by in-app add key (not from networked keys)
        public void AddFilteredKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key, bool updateCurves = true, bool lockTangents = false)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
            {
                animationSet = new AnimationSet(gobject);
                animations.Add(gobject, animationSet);
            }
            Curve curve = animationSet.GetCurve(property);

            //Filter rotation
            if (property == AnimatableProperty.RotationX || property == AnimatableProperty.RotationY || property == AnimatableProperty.RotationZ)
            {
                AnimationKey previousKey = curve.GetPreviousKey(key.frame);
                if (null != previousKey)
                {
                    float delta = Mathf.DeltaAngle(previousKey.value, key.value);
                    key.value = previousKey.value + delta;
                }
                else
                {
                    float delta = Mathf.DeltaAngle(0, key.value);
                    key.value = delta;
                }
            }
            curve.AddKey(key, lockTangents);

            if (updateCurves) onChangeCurve.Invoke(gobject, property);
        }

        public void AddFilteredKeyframeZone(GameObject gobject, AnimatableProperty property, AnimationKey key, int startFrame, int endFrame, bool updateCurves = true)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            Curve curve = animationSet.GetCurve(property);

            // Filter rotation
            if (property == AnimatableProperty.RotationX || property == AnimatableProperty.RotationY || property == AnimatableProperty.RotationZ)
            {
                AnimationKey previousKey = curve.GetPreviousKey(key.frame);
                if (null != previousKey)
                {
                    float delta = Mathf.DeltaAngle(previousKey.value, key.value);
                    key.value = previousKey.value + delta;
                }
                else
                {
                    float delta = Mathf.DeltaAngle(0, key.value);
                    key.value = delta;
                }
            }
            curve.AddZoneKey(key, startFrame, endFrame);
            if (updateCurves) onChangeCurve.Invoke(gobject, property);
        }


        public void AddFilteredKeyframeTangent(GameObject gobjet, AnimatableProperty property, AnimationKey key, int start, int end, bool updateCurves = true)
        {
            AnimationSet animationSet = GetObjectAnimation(gobjet);
            Curve curve = animationSet.GetCurve(property);
            // Filter rotation
            if (property == AnimatableProperty.RotationX || property == AnimatableProperty.RotationY || property == AnimatableProperty.RotationZ)
            {
                AnimationKey previousKey = curve.GetPreviousKey(key.frame);
                if (null != previousKey)
                {
                    float delta = Mathf.DeltaAngle(previousKey.value, key.value);
                    key.value = previousKey.value + delta;
                }
                else
                {
                    float delta = Mathf.DeltaAngle(0, key.value);
                    key.value = delta;
                }
            }
            curve.AddTangentKey(start, end);
            if (updateCurves) onChangeCurve.Invoke(gobjet, property);
        }

        private void RemoveEmptyAnimationSet(GameObject gobject)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
                return;
            foreach (Curve curve in animationSet.curves.Values)
            {
                if (curve.keys.Count != 0)
                    return;
            }

            animations.Remove(gobject);
        }

        public void RemoveKeyframe(GameObject gobject, AnimatableProperty property, int frame, bool updateCurves = true, bool lockTangents = false)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
                return;
            Curve curve = animationSet.GetCurve(property);
            curve.RemoveKey(frame, lockTangents);

            RemoveEmptyAnimationSet(gobject);

            if (!IsAnimating())
                EvaluateAnimations();
            if (updateCurves) onChangeCurve.Invoke(gobject, property);
        }

        public void Record()
        {
            if (animationState != AnimationState.Stopped)
                return;

            animationState = AnimationState.Preroll;
            countdown.gameObject.SetActive(true);
        }
        public void OnToggleStartVideoOutput(bool record)
        {
            if (record)
            {
                animationState = AnimationState.VideoOutput;
                Selection.enabled = false;
                onAnimationStateEvent.Invoke(animationState);

                // Force rendering the first frame
                CurrentFrame = currentFrame;
            }
            else
            {
                Pause();
            }
        }

        public void TogglePlayePause()
        {
            if (animationState == AnimationState.Playing) OnTogglePlayPause(false);
            else OnTogglePlayPause(true);
        }

        public void OnTogglePlayPause(bool play)
        {
            if (play) { Play(); }
            else { Pause(); }
        }

        public void Play()
        {
            animationState = AnimationState.Playing;
            onAnimationStateEvent.Invoke(animationState);

            playStartFrame = currentFrame;
            playStartTime = Time.time;
        }

        public void Pause()
        {
            switch (animationState)
            {
                case AnimationState.Preroll:
                    countdown.gameObject.SetActive(false);
                    break;
                case AnimationState.AnimationRecording:
                    StopRecording();
                    countdown.gameObject.SetActive(false);
                    break;
                case AnimationState.VideoOutput:
                    Selection.enabled = true;
                    break;
            }
            animationState = AnimationState.Stopped;
            onAnimationStateEvent.Invoke(animationState);
        }

        public void StartRecording()
        {
            playStartFrame = currentFrame;
            playStartTime = Time.time;
            animationState = AnimationState.AnimationRecording;
            onAnimationStateEvent.Invoke(animationState);
            preRecordInterpolation = GlobalState.Settings.interpolation;
            GlobalState.Settings.interpolation = Interpolation.Linear;
        }

        void RecordFrame()
        {
            foreach (var selected in Selection.ActiveObjects)
            {
                if (!recordingObjects.TryGetValue(selected, out AnimationSet animationSet))
                {
                    oldAnimations.Add(selected, GetObjectAnimation(selected));

                    // Remove existing animation
                    animations.Remove(selected);
                    onRemoveAnimation.Invoke(selected);

                    // Create new one
                    animationSet = new AnimationSet(selected);
                    recordingObjects.Add(selected, animationSet);
                }

                foreach (Curve curve in animationSet.curves.Values)
                {
                    Vector3 position = selected.transform.localPosition;
                    Vector3 rotation = selected.transform.localEulerAngles;
                    Vector3 scale = selected.transform.localScale;

                    float power = -1;
                    Color color = Color.white;
                    LightController lightController = selected.GetComponent<LightController>();
                    if (null != lightController)
                    {
                        power = lightController.Power;
                        color = lightController.Color;
                    }

                    float cameraFocal = -1;
                    float cameraFocus = -1;
                    float cameraAperture = -1;
                    CameraController cameraController = selected.GetComponent<CameraController>();
                    if (null != cameraController)
                    {
                        cameraFocal = cameraController.focal;
                        cameraFocus = cameraController.Focus;
                        cameraAperture = cameraController.aperture;
                    }

                    switch (curve.property)
                    {
                        case AnimatableProperty.PositionX: curve.AppendKey(new AnimationKey(currentFrame, position.x)); break;
                        case AnimatableProperty.PositionY: curve.AppendKey(new AnimationKey(currentFrame, position.y)); break;
                        case AnimatableProperty.PositionZ: curve.AppendKey(new AnimationKey(currentFrame, position.z)); break;

                        case AnimatableProperty.RotationX: AppendFilteredKey(curve, currentFrame, rotation.x); break;
                        case AnimatableProperty.RotationY: AppendFilteredKey(curve, currentFrame, rotation.y); break;
                        case AnimatableProperty.RotationZ: AppendFilteredKey(curve, currentFrame, rotation.z); break;

                        case AnimatableProperty.ScaleX: curve.AppendKey(new AnimationKey(currentFrame, scale.x)); break;
                        case AnimatableProperty.ScaleY: curve.AppendKey(new AnimationKey(currentFrame, scale.y)); break;
                        case AnimatableProperty.ScaleZ: curve.AppendKey(new AnimationKey(currentFrame, scale.z)); break;

                        case AnimatableProperty.Power: curve.AppendKey(new AnimationKey(currentFrame, power)); break;
                        case AnimatableProperty.ColorR: curve.AppendKey(new AnimationKey(currentFrame, color.r)); break;
                        case AnimatableProperty.ColorG: curve.AppendKey(new AnimationKey(currentFrame, color.g)); break;
                        case AnimatableProperty.ColorB: curve.AppendKey(new AnimationKey(currentFrame, color.b)); break;

                        case AnimatableProperty.CameraFocal: curve.AppendKey(new AnimationKey(currentFrame, cameraFocal)); break;
                        case AnimatableProperty.CameraFocus: curve.AppendKey(new AnimationKey(currentFrame, cameraFocus)); break;
                        case AnimatableProperty.CameraAperture: curve.AppendKey(new AnimationKey(currentFrame, cameraAperture)); break;
                    }
                }
            }
        }

        private void AppendFilteredKey(Curve curve, int frame, float value)
        {
            if (curve.keys.Count > 0)
            {
                AnimationKey previousKey = curve.keys[curve.keys.Count - 1];
                float delta = Mathf.DeltaAngle(previousKey.value, value);
                value = previousKey.value + delta;
            }
            curve.AppendKey(new AnimationKey(frame, value));
        }

        public void StopRecording()
        {
            CommandGroup recordGroup = new CommandGroup("Record");

            foreach (var animationSet in recordingObjects.Values)
            {
                GameObject gobject = animationSet.transform.gameObject;
                foreach (Curve curve in animationSet.curves.Values)
                    curve.ComputeCache();
                new CommandRecordAnimations(gobject, oldAnimations[gobject], animationSet).Submit();
            }

            recordGroup.Submit();

            recordingObjects.Clear();
            oldAnimations.Clear();
            GlobalState.Settings.interpolation = preRecordInterpolation;
        }

    }
}
