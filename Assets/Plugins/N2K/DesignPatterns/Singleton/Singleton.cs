using UnityEngine;

namespace N2K
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected abstract bool IsDontDestroyOnLoad { get; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate singleton detected: {typeof(T).Name}. Destroying {name}");
                Destroy(gameObject);
            }
            else
            {
                Instance = (T)this;
                if (IsDontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
                OnSingletonAwake();
            }
        }

        protected abstract void OnSingletonAwake();

        protected virtual void OnDestroy()
        {
            if(Instance == this)
            {
                Instance = null;
            }
        }
    }
}