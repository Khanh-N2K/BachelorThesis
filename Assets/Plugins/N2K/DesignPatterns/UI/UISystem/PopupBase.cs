using System;
using System.Collections;
using UnityEngine;

namespace N2K
{
    public abstract class PopupBase : MonoBehaviour
    {
        [Header("=== POPUP BASE ===")]


        #region ___ REFERENCES ___
        [Header("References")]

        [SerializeField]
        private AnimationClip _onHideAnim;

        private Animation _animation;
        #endregion ___


        #region ___ SETTINGS ___
        [Header("Settings")]

        [SerializeField]
        private bool _destroyAfterHide = false;

        public bool DestroyAfterHide => _destroyAfterHide;
        #endregion ___


        #region ___ DATA ___
        private bool _isInitialized = false;

        private bool _isHiding = false;

        private Action _onShowed;
        
        private Action _onHidden;
        #endregion ___


        protected virtual void Initialize()
        {
            _animation = GetComponent<Animation>();
        }

        internal void SetCallbacks(Action onShowed, Action onHidden)
        {
            _onShowed = onShowed;
            _onHidden = onHidden;
        }

        public void Hide()
        {
            UIManager.Instance.HidePopup(this);
        }

        internal virtual void OnShow()
        {
            if (!_isInitialized)
            {
                Initialize();
                _isInitialized = true;
            }
            gameObject.SetActive(true);
            _onShowed?.Invoke();
        }

        internal virtual void OnHide(Action onFinished)
        {
            if (_isHiding)
            {
                return;
            }
            _isHiding = true;
            if (_onHideAnim != null)
            {
                StartCoroutine(PlayHideAnimation(onFinished));
            }
            else
            {
                FinishHide(onFinished);
            }
        }

        private IEnumerator PlayHideAnimation(Action onFinished)
        {
            if (_animation == null)
            {
                _animation = GetComponent<Animation>();
                if(_animation == null)
                {
                    _animation = gameObject.AddComponent<Animation>();
                }
            }
            if (!_animation.GetClip(_onHideAnim.name))
            {
                _animation.AddClip(_onHideAnim, _onHideAnim.name);
            }
            _animation.Play(_onHideAnim.name);
            yield return new WaitForSeconds(_onHideAnim.length);
            FinishHide(onFinished);
        }

        private void FinishHide(Action onFinished)
        {
            _isHiding = false;
            gameObject.SetActive(false);
            onFinished?.Invoke();

            // Invoke to outside UI System
            _onHidden?.Invoke();
            _onShowed = null;
            _onHidden = null;
        }
    }
}