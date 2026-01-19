using System;

namespace N2K
{
    [Serializable]
    public class AnimationEventData
    {
        public float normalizedTime; // 0 to 1
        public Action callback;

        public AnimationEventData(float time, Action callback)
        {
            normalizedTime = time;
            this.callback = callback;
        }
    }
}