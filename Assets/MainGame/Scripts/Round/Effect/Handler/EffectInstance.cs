using N2K;
using UnityEngine;

public class EffectInstance : PoolMember
{
    #region ___ REFERENCES ___

    private EffectHandler _handler;

    #endregion ___

    #region ___ SETTINGS ___

    protected override int defaultCapacity => 10;

    protected override int maxSize => 20;

    #endregion ___

    #region ___ DATA ___

    private bool _isInitialized = false;

    private EffectConfigBase _config;

    public EffectConfigBase Config => _config;

    public EffectType Type => _config.Type;

    private float _countdownTimer;

    #endregion ___

    private void OnDisable()
    {
        _isInitialized = false;
    }

    private float triggerTimer;
    private void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

        if (_countdownTimer <= 0)
        {
            if (_handler == null)
            {
                Debug.LogError("null handleer");
                return;
            }
            _handler.RemoveEffect(this);
            ReleaseToPool();
            return;
        }
        _countdownTimer -= Time.deltaTime;
        if (_config.triggerInterval > 0)
        {
            if (triggerTimer >= _config.triggerInterval)
            {
                _handler.onEffectTriggered?.Invoke(this);
                triggerTimer = 0;
            }
            triggerTimer += Time.deltaTime;
        }
    }

    public void Initialize(EffectHandler hander, EffectConfigBase config)
    {
        // save data
        _handler = hander;
        _config = config.Clone();
        _countdownTimer = config.duration;

        // Reset data
        triggerTimer = 0;
        _isInitialized = true;
    }
}
