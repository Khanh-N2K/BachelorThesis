using System;
using System.Collections.Generic;
using UnityEngine;

namespace N2K
{
    public class UIManager : Singleton<UIManager>
    {
        [Header("=== UI MANAGER ===")]


        #region ___ REFERENCES ___
        [Header("References - Holders")]

        [SerializeField]
        private Transform _screenHolder;

        [SerializeField]
        private Transform _popupHolder;

        [Header("References - Prefabs")]

        [SerializeField]
        private List<ScreenBase> _screenPrefabs;

        [SerializeField]
        private List<PopupBase> _popupPrefabs;
        #endregion ___


        #region ___ SETTINGS ___
        protected override bool IsDontDestroyOnLoad => false;
        #endregion ___


        #region ___ DATA ___
        private Dictionary<Type, ScreenBase> _screenPrefabDict = new();

        private Dictionary<Type, PopupBase> _popupPrefabDict = new();

        private ScreenBase _currentScreen;

        private List<ScreenBase> _inactiveScreens = new List<ScreenBase>();

        private List<PopupBase> _activePopups = new List<PopupBase>();

        private List<PopupBase> _inactivePopups = new List<PopupBase>();

        public int ActivePopupCount { get; private set; } = 0;

        // ACTION

        public Action<ScreenBase> onScreenShown;

        public Action<ScreenBase> onScreenHidden;

        public Action<PopupBase> onPopupShown;

        public Action<PopupBase> onPopupHidden;
        #endregion ___


        protected override void OnSingletonAwake()
        {
            _screenPrefabDict = new Dictionary<Type, ScreenBase>(_screenPrefabs.Count);
            _popupPrefabDict = new Dictionary<Type, PopupBase>(_popupPrefabs.Count);
            foreach (ScreenBase screen in _screenPrefabs)
            {
                if (_screenPrefabDict.ContainsKey(screen.GetType()))
                {
                    Debug.LogError($"There's more than 1 instance {screen.GetType().Name}");
                }
                else
                {
                    _screenPrefabDict.Add(screen.GetType(), screen);
                }
            }
            foreach (PopupBase popup in _popupPrefabs)
            {
                if (_popupPrefabDict.ContainsKey(popup.GetType()))
                {
                    Debug.LogError($"There's more than 1 instance of {popup.GetType().Name}");
                }
                else
                {
                    _popupPrefabDict.Add(popup.GetType(), popup);
                }
            }
        }


        #region ___ SCREEN ___
        public T ShowScreen<T>(Action onShowed = null, Action onHidden = null) where T : ScreenBase
        {
            if (!_screenPrefabDict.TryGetValue(typeof(T), out var prefab))
            {
                Debug.LogError($"{typeof(T).Name} not found in dictionary!");
                return null;
            }
            HideCurrentScreen();

            // Get new screen from unused screen or newly instantiated
            ScreenBase newScreen = null;
            // Try reuse
            for(int i = _inactiveScreens.Count - 1; i >= 0; i--)
            {
                if (_inactiveScreens[i].GetType() == typeof(T))
                {
                    newScreen = _inactiveScreens[i];
                    _inactiveScreens.RemoveAt(i);
                    newScreen.gameObject.SetActive(true);
                    break;
                }
            }
            // Intantiated if needed
            if (newScreen == null)
            {
                newScreen = Instantiate(prefab, _screenHolder);
            }

            // Show new screen
            newScreen.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            newScreen.SetCallbacks(onShowed, onHidden);
            newScreen.OnShow();
            _currentScreen = newScreen;
            onScreenShown?.Invoke(newScreen);
            return _currentScreen as T;
        }

        public void HideCurrentScreen()
        {
            if (_currentScreen == null)
            {
                return;
            }
            ScreenBase cachedScreen = _currentScreen;
            _currentScreen = null;
            cachedScreen.OnHide(() =>
            {
                onScreenHidden?.Invoke(cachedScreen);
                if (cachedScreen.DestroyAfterHide)
                {
                    Destroy(cachedScreen.gameObject);
                }
                else
                {
                    _inactiveScreens.Add(cachedScreen);
                }
            });
        }

        public bool IsScreenOpen<T>() where T : ScreenBase
        {
            return _currentScreen != null && _currentScreen.GetType() == typeof(T);
        }

        public bool TryGetCurrentScreen<T>(out T screen) where T : ScreenBase
        {
            if (_currentScreen != null && _currentScreen.GetType() == typeof(T))
            {
                screen = (T)_currentScreen;
                return true;
            }
            screen = null;
            return false;
        }
        #endregion ___


        #region ___ POPUP ___
        public T ShowPopup<T>(Action onShowed = null, Action onHidden = null) where T : PopupBase
        {
            if (!_popupPrefabDict.TryGetValue(typeof(T), out var prefab))
            {
                Debug.LogError($"{typeof(T).Name} not found in dictionary!");
                return null;
            }

            // Get new popup from unused popup or newly created
            PopupBase newPopup = null;
            // Try reuse
            for(int i = _inactivePopups.Count - 1; i >= 0; i--)
            {
                if (_inactivePopups[i].GetType() == typeof(T))
                {
                    newPopup = _inactivePopups[i];
                    _inactivePopups.RemoveAt(i);
                    newPopup.gameObject.SetActive(true);
                    break;
                }
            }
            // Instantiated if needed
            if (newPopup == null)
            {
                newPopup = Instantiate(prefab, _popupHolder);
            }

            // Show new popup
            newPopup.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            newPopup.SetCallbacks(onShowed, onHidden);
            newPopup.transform.SetAsLastSibling();
            newPopup.OnShow();
            _activePopups.Add(newPopup);
            ActivePopupCount++;
            onPopupShown?.Invoke(newPopup);
            return newPopup as T;
        }

        public void HideTopPopup()
        {
            if (_activePopups.Count == 0)
            {
                return;
            }
            int lastIndex = _activePopups.Count - 1;
            PopupBase topPopup = _activePopups[lastIndex];
            HidePopupInternal(topPopup);
        }

        public void HidePopup(PopupBase targetPopup)
        {
            if (targetPopup == null || !_activePopups.Contains(targetPopup))
                return;
            HidePopupInternal(targetPopup);
        }

        public void HideAllPopups()
        {
            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                PopupBase popup = _activePopups[i];
                HidePopupInternal(popup);
            }
            _activePopups.Clear();
        }

        public void HideAllPopups<T>() where T : PopupBase
        {
            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                PopupBase popup = _activePopups[i];
                if (popup.GetType() == typeof(T))
                {
                    HidePopupInternal(popup);
                }
            }
        }

        private void HidePopupInternal(PopupBase popup)
        {
            if (popup == null)
            {
                return;
            }
            _activePopups.Remove(popup);
            ActivePopupCount--;
            popup.OnHide(() =>
            {
                onPopupHidden?.Invoke(popup);
                if (popup.DestroyAfterHide)
                {
                    Destroy(popup.gameObject);
                }
                else
                {
                    _inactivePopups.Add(popup);
                }
            });
        }

        // GETTERS

        public int GetActivePopupCount<T>() where T : PopupBase
        {
            int count = 0;
            foreach (var popup in _activePopups)
            {
                if (popup.GetType() == typeof(T))
                    count++;
            }
            return count;
        }

        public List<T> GetAllActivePopups<T>() where T : PopupBase
        {
            List<T> result = new List<T>();
            foreach (var popup in _activePopups)
            {
                if (popup.GetType() == typeof(T))
                    result.Add(popup as T);
            }
            return result;
        }

        public bool HasPopup<T>() where T : PopupBase
        {
            foreach (var popup in _activePopups)
            {
                if (popup.GetType() == typeof(T))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion ___
    }
}