using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace VRtist
{

    public class MoviePlayer : MonoBehaviour
    {
        private VideoPlayer player;

        public void Start()
        {
            player = GetComponent<VideoPlayer>();
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

            //Debug.Log(frame);
            //player.frame = frame;
            //player.Play();
            //player.Stop();
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