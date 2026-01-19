using Cysharp.Threading.Tasks;
using DG.Tweening;
using N2K;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

public class Tower : PoolMember
{
    #region ___ REFERENCES ___

    // External references

    private TowerManager _towerManager;

    [Header("References")]

    [SerializeField]
    private TowerSelectablePart _selectionPart;

    public TowerSelectablePart SelectablePart => _selectionPart;

    [SerializeField]
    private EffectHandler _effectHandler;

    [SerializeField]
    private TowerFireRange _fireRange;

    public TowerFireRange FireRange => _fireRange;

    [SerializeField]
    private Transform _bulletSpawnPos;

    [Header("References - Visual")]

    [SerializeField]
    private GameObject _mainVisual;

    [SerializeField]
    private GameObject _buildingVisual;

    #endregion ___

    #region ___ SETTINGS ___

    protected override int defaultCapacity => 15;

    protected override int maxSize => 20;

    #endregion ___

    #region ___ DATA ___

    private TowerType _type;

    public TowerType Type => _type;

    private TowerLevelConfig _config;

    private Obstacle _groundObstacle;

    public Obstacle GroundObstacle => _groundObstacle;

    private TowerState _state;

    private CancellationTokenSource _cts;

    // Config

    public TowerLevelConfig Config => _config;

    private float _baseDamage;

    private float _finalDamage;

    private float _baseFireRate;

    private float _finalFireRate;

    #endregion ___

    private void OnEnable()
    {
        _effectHandler.onEffectStarted += OnEffectStarted;
        _effectHandler.onEffectTriggered += OnEffectTriggered;
        _effectHandler.onEffectEnded += OnEffectEnded;
    }

    private void OnDisable()
    {
        _effectHandler.onEffectStarted += OnEffectStarted;
        _effectHandler.onEffectTriggered += OnEffectTriggered;
        _effectHandler.onEffectEnded += OnEffectEnded;
    }

    public void Initialize(TowerType type, TowerLevelConfig config, TowerManager towerManager, Obstacle obstacle)
    {
        _towerManager = towerManager;
        _groundObstacle = obstacle;
        _type = type;
        _config = config;
        _baseDamage = _config.combatData.damage;
        _baseFireRate = config.combatData.fireRate;
        RefreshFinalDamage();
        RefreshFinalFireRate();

        _selectionPart.Initialize();
        _selectionPart.Initialize(this);
        ChangeState(TowerState.Spawning);
    }

    public void DestroySelf()
    {
        ChangeState(TowerState.None);
        if (_groundObstacle != null)
        {
            _groundObstacle.UnregisterTower();
            _groundObstacle = null;
        }
        _towerManager.UnregisterTower(this);
        ReleaseToPool();
    }

    public void ChangeState(TowerState state, bool forcedChangeState = false)
    {
        if (!forcedChangeState)
        {
            if (_state == TowerState.Spawning && state != TowerState.None)
            {
                return;     // execpt destroying tower, don't stop them from spawning
            }
        }
        _cts?.Cancel();
        _cts = new();
        _state = state;
        switch (_state)
        {
            case TowerState.Spawning:
                HandleSpawningState().Forget();
                break;
            case TowerState.Combat:
                HandleCombatState().Forget();
                break;
        }
    }

    private async UniTask HandleSpawningState()
    {
        _mainVisual.SetActive(false);
        _buildingVisual.SetActive(true);
        _buildingVisual.transform.localScale = Vector3.one;
        _fireRange.transform.localScale = Vector3.zero;
        // Animate building visual show up
        _buildingVisual.transform.localPosition = Vector3.down;
        Vector3 start = Vector3.down;
        float timer = 0;
        float moveDuration = 0.3f;
        while (timer < moveDuration)
        {
            _buildingVisual.transform.localPosition = Vector3.Lerp(start, Vector3.zero, timer / moveDuration);
            timer += Time.deltaTime;
            await UniTask.DelayFrame(1, cancellationToken: _cts.Token);
        }
        _buildingVisual.transform.localPosition = Vector3.zero;
        // Wait 0.3s
        await UniTask.Delay(300, cancellationToken: _cts.Token);
        // Animate main visual show down
        _mainVisual.transform.localPosition = Vector3.up * 1.5f;
        _mainVisual.SetActive(true);
        start = Vector3.up * 1.5f;
        timer = 0;
        moveDuration = 0.3f;
        while (timer < moveDuration)
        {
            _mainVisual.transform.localPosition = Vector3.Lerp(start, Vector3.zero, timer / moveDuration);
            timer += Time.deltaTime;
            await UniTask.DelayFrame(1, cancellationToken: _cts.Token);
        }
        _mainVisual.transform.localPosition = Vector3.zero;
        _buildingVisual.transform.DOScale(0, 0.2f)
            .OnKill(() => _buildingVisual.SetActive(false));
        _fireRange.transform.DOScale(_config.combatData.range * 2, 1);
        if (_towerManager.RoundManager.State == RoundState.WaveSimulating)
        {
            ChangeState(TowerState.Combat, true);
        }
        else
        {
            ChangeState(TowerState.Inactive, true);
        }
    }

    #region ___ COMBAT STATE ___

    private async UniTask HandleCombatState()
    {
        while (true)
        {
            Attacker attacker = _fireRange.GetClosestActiveAttacker();
            if (attacker != null)
            {
                Shoot(attacker);
            }

            float interval = 1f / Mathf.Max(_finalFireRate, 0.01f);

            await UniTask.Delay(
                TimeSpan.FromSeconds(interval),
                cancellationToken: _cts.Token
            );
        }
    }

    private void Shoot(Attacker attacker)
    {
        BulletBase bullet = ObjectPoolAtlas.Instance.Get<BulletBase>(_config.bulletPrefab);
        bullet.transform.position = _bulletSpawnPos.position;
        bullet.Initialize(_config.combatData.bulletSpeed, _finalDamage, attacker);
    }

    #endregion ___

    #region ___ EFFECT ___

    private void OnEffectStarted(EffectInstance effect)
    {
        if (effect.Type == EffectType.DamageModify)
        {
            RefreshFinalDamage();
            var vfx = ObjectPoolAtlas.Instance.Get(_towerManager.DamageBoostVfxPrefab);
            vfx.transform.parent = transform;
            vfx.transform.localPosition = Vector3.zero;
            vfx.SetTimeOut(effect.Config.duration);
        }
        else if (effect.Type == EffectType.FireRateModify)
        {
            RefreshFinalFireRate();
            var vfx = ObjectPoolAtlas.Instance.Get(_towerManager.FireRateBoostedVfxPrefab);
            vfx.transform.parent = transform;
            vfx.transform.localPosition = Vector3.zero;
            vfx.SetTimeOut(effect.Config.duration);
        }
    }

    private void OnEffectTriggered(EffectInstance effect)
    {

    }

    private void OnEffectEnded(EffectInstance effect)
    {
        if (effect.Type == EffectType.DamageModify)
        {
            RefreshFinalDamage();
        }
        else if (effect.Type == EffectType.FireRateModify)
        {
            RefreshFinalFireRate();
        }
    }

    private void RefreshFinalDamage()
    {
        float additive = 0;
        foreach (var effect in _effectHandler.EffectSet)
        {
            if (effect.Type == EffectType.DamageModify)
            {
                additive += (effect.Config as DamageModifyEffectConfig).increaseRateNormalized;
            }
        }
        additive = Mathf.Max(additive, -1);
        _finalDamage = _baseDamage * (1 + additive);
    }

    private void RefreshFinalFireRate()
    {
        float additive = 0f;

        foreach (var effect in _effectHandler.EffectSet)
        {
            if (effect.Type == EffectType.FireRateModify)
            {
                additive += (effect.Config as FireRateModifyEffectConfig).increaseRateNormalized;
            }
        }

        // Prevent zero or negative fire rate
        additive = Mathf.Max(additive, -0.9f);

        _finalFireRate = _baseFireRate * (1f + additive);
    }

    #endregion ___

    #region ___ CONTEXT MENU ___

    [ContextMenu("Refresh references")]
    private void RefreshReferences()
    {
        _effectHandler = GetComponentInChildren<EffectHandler>();
    }

    #endregion ___
}
