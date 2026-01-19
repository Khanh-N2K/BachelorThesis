using Ookii.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace N2K
{
    public class AnimatorController : MonoBehaviour
    {
        [Header("References")]

        [SerializeField] private Animator _animator;

        [Header("Data")]

        private int _playSessionId = 0; // increment each time PlayAnimation is called

        public void PlayAnimation(AnimationClip clip, Action onFinished = null, float transitionDuration = 0.1f, float speed = 1f, List<AnimationEventData> events = null)
        {
            _animator.speed = speed;
            _playSessionId++; // new play session
            int sessionId = _playSessionId;

            StopAllCoroutines();

            StartCoroutine(PlayingAnimationIE(sessionId, clip, onFinished, transitionDuration, events));
        }

        private IEnumerator PlayingAnimationIE(int sessionId, AnimationClip clip, Action onFinished, float transitionDuration, List<AnimationEventData> events)
        {
            // wait until ready to crossfade
            while (_animator.IsInTransition(0))
                yield return null;

            if (!_animator.HasState(0, Animator.StringToHash(clip.name)))
            {
                Debug.LogError($"Can't find state name: {clip.name}");
                yield break;
            }
            _animator.CrossFadeInFixedTime(clip.name, transitionDuration, 0);

            // wait for animator to actually start this clip
            while (!_animator.GetCurrentAnimatorStateInfo(0).IsName(clip.name))
            {
                if (sessionId != _playSessionId) yield break; // newer animation started
                yield return null;
            }

            yield return TrackAnimationIE(sessionId, clip.name, onFinished, events);
        }

        private IEnumerator TrackAnimationIE(int sessionId, string animationName, Action onFinished, List<AnimationEventData> events)
        {
            var triggered = new HashSet<AnimationEventData>();
            bool finishedInvoked = false;
            int lastLoopId = 0;

            while (true)
            {
                if (sessionId != _playSessionId)
                    yield break; // a newer PlayAnimation started, stop tracking

                var state = _animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName(animationName))
                    break; // transitioned out

                float time = state.normalizedTime;
                int currentLoopId = Mathf.FloorToInt(time);
                float normalizedCycleTime = time % 1;

                if (currentLoopId > lastLoopId)
                {
                    triggered.Clear();
                    lastLoopId = currentLoopId;
                }

                if (events != null)
                    TriggerEvents(triggered, events, normalizedCycleTime);

                if (time >= 1f && !finishedInvoked)
                {
                    onFinished?.Invoke();
                    finishedInvoked = true;
                }

                if (!state.loop && time >= 1)
                    break;

                yield return null;
            }
        }

        private void TriggerEvents(HashSet<AnimationEventData> triggered, List<AnimationEventData> events, float currentTime)
        {
            foreach (var e in events)
            {
                if (!triggered.Contains(e) && currentTime >= e.normalizedTime)
                {
                    e.callback?.Invoke();
                    triggered.Add(e);
                }
            }
        }

        public void SetSpeed(float speed)
        {
            _animator.speed = speed;
        }
    }
}