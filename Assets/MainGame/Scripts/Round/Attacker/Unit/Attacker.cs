using Cysharp.Threading.Tasks;
using DG.Tweening;
using N2K;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

public class Attacker : MonoBehaviour
{
    #region ___ REFERENCES ___

    // EXTERNAL REFERENCES
    private AttackerManager _attackerManager;

    private AttackerGroup _group;

    [Header("=== ATTACKER ===")]

    [Header("References")]

    [SerializeField]
    private HealthBar _healthBar;

    [SerializeField]
    private EffectHandler _effectHandler;

    [Header("References - Anim")]

    [SerializeField]
    private AnimatorController _animController;

    [SerializeField]
    private AnimationClip _idleAnim;

    [SerializeField]
    private AnimationClip _idleAnim1;

    [SerializeField]
    private AnimationClip _idleAnim2;

    [SerializeField]
    private AnimationClip _walkAnim;

    [SerializeField]
    private AnimationClip _attackAnim;

    [SerializeField]
    private AnimationClip _dieAnim;

    #endregion ___

    #region ___ SETTINGS ___

    private float _rotationSpeed = 720f;

    #endregion ___

    #region ___ DATA ___

    private int _idInGroup;

    private AttackerState _state;

    public AttackerState State => _state;

    private CancellationTokenSource _cts = new();

    // COMBAT

    private float _currentHealth;

    private float _maxHealth;

    public Action<Attacker> onDead;

    private int _goldDrop;

    // MOVEMENT

    private float _baseSpeed = 1;

    private float _finalSpeed = 1;

    #endregion ___

    private void OnEnable()
    {
        _effectHandler.onEffectStarted += OnEffectStarted;
        _effectHandler.onEffectTriggered += OnEffectTriggered;
        _effectHandler.onEffectEnded += OnEffectEnded;
    }

    private void OnDisable()
    {
        _effectHandler.onEffectStarted -= OnEffectStarted;
        _effectHandler.onEffectTriggered -= OnEffectTriggered;
        _effectHandler.onEffectEnded -= OnEffectEnded;
        _cts?.Cancel();
        _cts = null;
    }

    public void Initialize(AttackerManager attackerManager, AttackerGroup group, float health, int goldDrop)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _attackerManager = attackerManager;
        _group = group;
        _currentHealth = health;
        _maxHealth = health;
        _goldDrop = goldDrop;
        _state = AttackerState.Inactive;
        _healthBar.RefreshVisual(1);
        PlayRandomIdleAnim();
    }

    public void SetGroup(AttackerGroup group)
    {
        _group = group;
    }

    public void SetIdInGroup(int id)
    {
        _idInGroup = id;
    }

    #region ___ STATE ___

    public void ChangeState(AttackerState state)
    {
        _cts?.Cancel();
        _cts = new();
        _state = state;
        switch (state)
        {
            case AttackerState.Moving:
                HandleMovingStep().Forget();
                break;
            case AttackerState.Attacking:
                HandleAttackTarget().Forget();
                break;
            case AttackerState.Idle:
                HandleIdle();
                break;
            default:
                Debug.LogError($"Not implementd state {state}");
                break;
        }
    }

    public void DestroySelf()
    {
        _cts?.Cancel();
        _cts = new();
        _state = AttackerState.None;
        _group.UnregisterAttacker(this);
        onDead?.Invoke(this);
        _animController.PlayAnimation(_dieAnim, onFinished: () =>
        {
            Destroy(gameObject);
        });
    }

    #endregion ___

    #region ___ COMBAT ___

    public async UniTask HandleAttackTarget()
    {
        var defender = _group?.TargetDefender;

        // Rotate toward
        transform.DOLookAt(defender.transform.position, 0.5f);

        _animController.PlayAnimation(_attackAnim);
        AudioManager.Instance.PlayOneShot(AudioNameType.ZombieAttackSound.ToString(), 0.5f);

        float timeToSendDamage = 1f;
        await UniTask.Delay(TimeSpan.FromSeconds(timeToSendDamage), cancellationToken: _cts.Token);

        if (_state != AttackerState.Attacking)
            return;

        defender = _group?.TargetDefender;
        if (defender == null || defender.State != DefenderState.Alive)
            return;

        defender.KillSelf();

        // Wait finish attack anim
        float remainingTime = _attackAnim.length - timeToSendDamage;
        await UniTask.Delay(TimeSpan.FromSeconds(remainingTime), cancellationToken: _cts.Token);
    }

    public void TakeDamage(float damage)
    {
        // Play audio
        if (damage > 0)
        {
            int rand = Random.Range(1, 4);
            AudioManager.Instance.PlayOneShot("ZombieHurt" + rand, 0.2f);
        }

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(0, _currentHealth);
        _healthBar.RefreshVisual(_currentHealth / _maxHealth);
        // Show text damage fly out
        UIManager.Instance.TryGetCurrentScreen(out IngameScreen ingameScreen);
        if (ingameScreen != null)
        {
            TextFlyOutPopup.ShowPopupFromWorldPos(transform.position, $"-{damage}", ingameScreen.TextFlyHolderBack, color: Color.red, offset: new Vector2(Random.Range(-50, 50), Random.Range(0, 50)));
        }
        // Check dead
        if (_currentHealth == 0)
        {
            DestroySelf();
            _attackerManager.RoundManager.AddGold(_goldDrop);
        }
    }

    #endregion ___

    #region ___ MOVEMENT ___

    public async UniTask HandleMovingStep()
    {
        _animController.PlayAnimation(_walkAnim);
        List<Int2> path = _group.Path;
        if (path == null)
        {
            Debug.LogError("Path is null");
            return;
        }

        // Already at or beyond last waypoint
        int wayPointId = _group.WayPointId;
        if (wayPointId >= path.Count - 1)
        {
            Debug.LogError("Already at target");
            return;
        }

        // Move
        Int2 next = path[wayPointId + 1];
        Vector3 centerPos = MapData.GetWorldPosOfCoord(next.x, next.y);
        Vector3 target = _attackerManager.RoundManager.UnitFormation.GetUnitPos(centerPos, _group.AttackerCount, _idInGroup);
        await MoveStepAsync(target, _attackerManager.RoundManager.StepDuration);
    }

    private async UniTask MoveStepAsync(Vector3 to, float baseDuration)
    {
        Vector3 from = transform.position;
        float totalDistance = Vector3.Distance(from, to);

        if (totalDistance <= 0.001f)
        {
            PlayRandomIdleAnim();
            return;
        }

        _baseSpeed = totalDistance / baseDuration;
        RefreshFinalSpeed();

        float traveled = 0f;

        Vector3 moveDir = (to - from);
        moveDir.y = 0f;
        moveDir.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(moveDir);

        while (traveled < totalDistance)
        {
            _cts.Token.ThrowIfCancellationRequested();

            //Move
            traveled += _finalSpeed * Time.deltaTime;
            float t = Mathf.Clamp01(traveled / totalDistance);
            transform.position = Vector3.Lerp(from, to, t);

            //Rotate smoothly
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime
            );

            _animController.SetSpeed(_finalSpeed / _baseSpeed * 0.4f);

            await UniTask.WaitForEndOfFrame(cancellationToken: _cts.Token);
        }

        transform.position = to;
        transform.rotation = targetRotation;

        PlayRandomIdleAnim();
    }

    #endregion ___

    #region ___ IDLE ___

    private void HandleIdle()
    {
        _animController.PlayAnimation(_idleAnim);
    }

    #endregion ___

    #region ___ EFFECT ___

    private void OnEffectStarted(EffectInstance effect)
    {
        if (effect.Type == EffectType.Freeze || effect.Type == EffectType.SpeedModify)
        {
            RefreshFinalSpeed();
            if (effect.Type == EffectType.Freeze)
            {
                var vfx = ObjectPoolAtlas.Instance.Get(_attackerManager.FreezeVfx);
                vfx.transform.parent = transform;
                vfx.transform.localPosition = Vector3.zero;
            }
            else if (effect.Type == EffectType.SpeedModify)
            {
                var vfx = ObjectPoolAtlas.Instance.Get(_attackerManager.SlowVfx);
                vfx.transform.parent = transform;
                vfx.transform.localPosition = Vector3.zero;
            }
        }
        else if (effect.Type == EffectType.DamageOverTime)
        {
            TakeDamage((effect.Config as DamageOverTimeEffectConfig).dps);
            var vfx = ObjectPoolAtlas.Instance.Get(_attackerManager.BurnVfx);
            vfx.transform.parent = transform;
            vfx.transform.localPosition = Vector3.zero;
            vfx.SetTimeOut(effect.Config.duration);
        }
        else if (effect.Type == EffectType.Kill)
        {
            float ceilHealthRateToKill = (effect.Config as KillEffectConfig).ceilThreshold_HealthRateNormalized;
            if (_currentHealth / _maxHealth <= ceilHealthRateToKill)
            {
                TakeDamage(_currentHealth);
                var vfx = ObjectPoolAtlas.Instance.Get(_attackerManager.KillVfx);
                vfx.transform.parent = transform;
                vfx.transform.localPosition = Vector3.zero;
            }
        }
    }

    private void OnEffectTriggered(EffectInstance effect)
    {
        if (effect.Type == EffectType.DamageOverTime)
        {
            TakeDamage((effect.Config as DamageOverTimeEffectConfig).dps);
        }
    }

    private void OnEffectEnded(EffectInstance effect)
    {
        if (effect.Type == EffectType.Freeze || effect.Type == EffectType.SpeedModify)
        {
            RefreshFinalSpeed();
        }
    }

    private void RefreshFinalSpeed()
    {
        float additive = 0f;
        foreach (EffectInstance effect in _effectHandler.EffectSet)
        {
            if (effect.Type == EffectType.Freeze)
            {
                _finalSpeed = 0;
                return;
            }
            else if (effect.Type == EffectType.SpeedModify)
            {
                additive += (effect.Config as SpeedModifyEffectConfig).increaseRateNormalized;
            }
        }
        additive = Mathf.Max(additive, -1);
        _finalSpeed = _baseSpeed * (1 + additive);
    }

    #endregion ___

    #region ___ ANIMATION ___

    private void PlayRandomIdleAnim()
    {
        int rand = Random.Range(0, 3);
        if (rand == 0)
        {
            _animController.PlayAnimation(_idleAnim);
        }
        else if (rand == 1)
        {
            _animController.PlayAnimation(_idleAnim1);
        }
        else
        {
            _animController.PlayAnimation(_idleAnim2);
        }
    }

    #endregion ___

    #region ___ CONTEXT MENU ___

    [ContextMenu("Refresh renderers for card target")]
    public void RefreshRenderersForCardTarget()
    {
        ClearRenderersForCardTarget();
        CardTarget cardTarget = GetComponentInChildren<CardTarget>();
        Renderer[] rendererArr = GetComponentsInChildren<Renderer>();
        cardTarget.AddRenderers(rendererArr);
    }

    [ContextMenu("Clear renderes for card target")]
    public void ClearRenderersForCardTarget()
    {
        CardTarget cardTarget = GetComponentInChildren<CardTarget>();
        cardTarget.ClearAllRendereres();
    }

    #endregion ___
}
