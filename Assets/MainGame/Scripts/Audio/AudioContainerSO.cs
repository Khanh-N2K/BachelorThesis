using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    [CreateAssetMenu(fileName = "AudioContainer", menuName = "ScriptableObject/AudioContainer", order = 0)]
    public class AudioContainerSO : ScriptableObject
    {
        public AudioClip[] clips;

        public AudioClip GetClip(string clipName)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].name == clipName)
                {
                    return clips[i];
                }
            }

            return null;
        }
    }
