using UnityEngine;
using UnityEngine.Pool;

namespace N2K
{
    public abstract class PoolMember : MonoBehaviour
    {
        [Header("=== POOL MEMBER ===")]


        #region ___ SETTINGS ___
        [Header("Settings")]

        protected abstract int defaultCapacity { get; }

        protected abstract int maxSize { get; }

        public int DefaultCapacity => defaultCapacity;

        public int MaxSize => maxSize;
        #endregion ___


        #region ___ DATA ___
        [Header("Data")]

        private ObjectPool<PoolMember> pool;

        public ObjectPool<PoolMember> Pool => pool;
        #endregion ___ DATA ___


        internal virtual void SetPool(ObjectPool<PoolMember> pool)
        {
            this.pool = pool;
        }

        #region ========================= POOL ACTION CALLBACKS ===================================

        internal virtual void OnGetFromPool()
        {
            gameObject.SetActive(true);
        }

        internal virtual void OnReleaseToPool()
        {
            gameObject.SetActive(false);
        }

        internal virtual void OnDestroyFromPool()
        {
            gameObject.SetActive(false);
        }
        #endregion -----------------------------------------------------------------------------------

        public void ReleaseToPool()
        {
            pool.Release(this);
        }
    }
}