using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace N2K
{
    public class ObjectPoolAtlas : Singleton<ObjectPoolAtlas>
    {
        #region ___ SETTINGS ___
        protected override bool IsDontDestroyOnLoad => false;
        #endregion ___


        #region ___ DATA ___
        private readonly Dictionary<PoolMember, ObjectPool<PoolMember>> _poolMapping = new();

        private readonly Dictionary<ObjectPool<PoolMember>, Transform> _poolHolderMapping = new();
        #endregion ___


        protected override void OnSingletonAwake()
        {
            
        }

        public T Get<T>(T prefab, Transform holder = null) where T : PoolMember
        {
            _poolMapping.TryGetValue(prefab, out ObjectPool<PoolMember> pool);
            if (pool == null)
            {
                pool = CreatePool(prefab, holder);
                _poolMapping[prefab] = pool;
            }
            return pool.Get() as T;
        }

        private ObjectPool<PoolMember> CreatePool(PoolMember prefab, Transform holder)
        {
            PoolMember prefabPoolMember = prefab.GetComponent<PoolMember>();
            ObjectPool<PoolMember> pool = new(
                createFunc: () => CreatePoolMember(prefab),
                actionOnGet: (PoolMember poolMember) => poolMember.OnGetFromPool(),
                actionOnRelease: (PoolMember poolMember) =>
                {
                    poolMember.OnReleaseToPool();
                    poolMember.transform.SetParent(_poolHolderMapping[poolMember.Pool]);
                },
                actionOnDestroy: (PoolMember poolMember) => poolMember.OnDestroyFromPool(),
                collectionCheck: false,
                defaultCapacity: prefabPoolMember.DefaultCapacity,
                maxSize: prefabPoolMember.MaxSize);

            if (holder == null)
            {
                GameObject holderObj = new GameObject($"{prefab.name} holder");
                holder = holderObj.transform;
                holder.SetParent(transform);
            }
            _poolHolderMapping[pool] = holder;

            return pool;
        }

        private PoolMember CreatePoolMember(PoolMember prefab)
        {
            _poolMapping.TryGetValue(prefab, out ObjectPool<PoolMember> pool);
            if (pool == null)
            {
                Debug.LogError("No pool found");
                return null;
            }

            PoolMember poolMember = Instantiate(prefab, _poolHolderMapping[pool]).GetComponent<PoolMember>();
            poolMember.SetPool(pool);
            return poolMember;
        }
    }
}