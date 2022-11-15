using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace VRtist
{

    public class MoviePlayer : MonoBehaviour
    {
        private VideoPlayer player;
        public Dopesheet dopesheet;

        public void OnEnable()
        {
            player = GetComponent<VideoPlayer>();
            string path = GlobalState.Settings.assetBankDirectory;
            if (Directory.Exists(path))
            {
                string[] movs = Directory.GetFiles(path, "*.mov");
                if (movs.Length == 0)
                {
                    dopesheet.ShowVideoPlayer = false;
                    return;
                }
                else
                {
                    player.url = movs[0];
                    dopesheet.ShowVideoPlayer = true;
                }
            }
            player.Prepare();
            AnimationEngine.Instance.onAnimationStateEvent.AddListener(OnStateChange);
            AnimationEngine.Instance.onFrameEvent.AddListener(OnFrameChange);
        }

        private void OnFrameChange(int frame)
        {
            if (!player.isPlaying)
            {
                player.frame = frame;
                player.Play();
            }
        }

        public void Update()
        {
            if (AnimationEngine.Instance.animationState != AnimationState.Playing && player.isPlaying)
            {
                player.Pause();
            }
        }

        public void OnStateChange(AnimationState state)
        {
            switch (state)
            {
                case AnimationState.Playing:
                    player.frame = AnimationEngine.Instance.CurrentFrame;
                    player.Play();
                    break;
                case AnimationState.Stopped:
                    player.Pause();
                    break;
            }
        }
    }

}