using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class SoundPlayer : MonoBehaviour
    {

        private AudioSource source;

        public void Start()
        {
            source = GetComponent<AudioSource>();
            source.spatialize = false;
            GlobalState.Animation.onAnimationStateEvent.AddListener(OnStateChange);
        }

        public void OnStateChange(AnimationState newState)
        {
            switch (newState)
            {
                case AnimationState.Playing:
                    source.time = AnimationEngine.Instance.FrameToTime(AnimationEngine.Instance.CurrentFrame);
                    source.Play();
                    Debug.Log("play " + AnimationEngine.Instance.FrameToTime(AnimationEngine.Instance.CurrentFrame));
                    break;
                case AnimationState.Stopped:
                    source.Stop();
                    Debug.Log("stop");
                    break;
            }
        }

    }
}