using System.Collections.Generic;
using UnityEngine;

namespace ARPG.Pooling
{
    /// <summary>
    /// 简易对象池：减少频繁 Instantiate/Destroy 带来的 GC。
    /// Profiler 对比点：优化前 SkillExecutor 直接 Instantiate；优化后走 Get/Release。
    /// </summary>
    public class SimpleObjectPool : MonoBehaviour
    {
        [SerializeField] GameObject prefab;
        [SerializeField] int prewarmCount = 8;
        [SerializeField] bool expandable = true;

        readonly Queue<GameObject> _available = new();
        readonly HashSet<GameObject> _leased = new();
        bool _warmed;

        public GameObject Prefab => prefab;
        public int AvailableCount => _available.Count;
        public int LeasedCount => _leased.Count;

        void Awake() => TryWarmup();

        /// <summary>运行时注入预制体并预热（DemoBootstrap 用）。</summary>
        public void Configure(GameObject poolPrefab, int warmCount = 8)
        {
            prefab = poolPrefab;
            prewarmCount = warmCount;
            TryWarmup();
        }

        void TryWarmup()
        {
            if (_warmed || prefab == null)
                return;
            _warmed = true;
            for (int i = 0; i < prewarmCount; i++)
                _available.Enqueue(CreateInstance());
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            TryWarmup();
            GameObject go = null;
            while (_available.Count > 0 && go == null)
                go = _available.Dequeue();

            // 优化前：这里会 new 一个对象；优化后优先复用队列里的实例
            if (go == null)
            {
                if (!expandable || prefab == null)
                    return null;
                go = CreateInstance();
            }

            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.SetParent(null, true);
            go.SetActive(true);
            _leased.Add(go);
            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null || !_leased.Remove(go))
                return;
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            _available.Enqueue(go);
        }

        GameObject CreateInstance()
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            var pooled = go.GetComponent<PooledLifetime>();
            if (pooled == null)
                pooled = go.AddComponent<PooledLifetime>();
            pooled.Bind(this);
            return go;
        }
    }

    /// <summary>
    /// 挂在池化预制体上：存活一段时间后自动归还。
    /// </summary>
    public class PooledLifetime : MonoBehaviour
    {
        [SerializeField] float lifetime = 0.25f;
        SimpleObjectPool _pool;
        float _dieAt;

        public void Bind(SimpleObjectPool pool) => _pool = pool;

        public void SetLifetime(float seconds)
        {
            lifetime = seconds;
            _dieAt = Time.time + lifetime;
        }

        void OnEnable()
        {
            _dieAt = Time.time + lifetime;
        }

        void Update()
        {
            if (Time.time >= _dieAt && _pool != null)
                _pool.Release(gameObject);
        }
    }
}
