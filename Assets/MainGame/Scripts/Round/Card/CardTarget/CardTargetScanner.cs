using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardTargetScanner : MonoBehaviour
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private SphereCollider _triggerCol;

    [SerializeField]
    private Projector _projector;

    [SerializeField]
    private Material _attackerHighlightMat;

    [SerializeField]
    private Material _towerHighlightMat;

    #endregion ___

    #region ___ DATA ___

    private bool _isFocusing = false;

    private float _radius = 3;

    private TagNameType[] _targetTagArr;

    private HashSet<CardTarget> _targetSet = new();

    #endregion ___

    private void Awake()
    {
        _triggerCol.radius = 0;
        _projector.orthographicSize = 0;
    }

    private void OnEnable()
    {
        EventVariances.CardSystem.onCardClicked += OnCardClicked;
        EventVariances.CardSystem.onCardReleased += OnCardReleased;
        EventVariances.CardSystem.onStartHoveredOnMap += OnCardTargeting;
        EventVariances.CardSystem.onEndHoveredOnMap += OnCardNotTargeting;
    }

    private void OnDisable()
    {
        EventVariances.CardSystem.onCardClicked -= OnCardClicked;
        EventVariances.CardSystem.onCardReleased -= OnCardReleased;
        EventVariances.CardSystem.onStartHoveredOnMap -= OnCardTargeting;
        EventVariances.CardSystem.onEndHoveredOnMap -= OnCardNotTargeting;
        foreach(var target in _targetSet.ToList())
        {
            RemoveFromSet(target);
        }
    }

    private void OnCardClicked(Card card)
    {
        _radius = card.EffectRadius;
        _targetTagArr = card.TagNameArr;
    }

    private void OnCardReleased(Card card)
    {
        bool hasTarget = _targetSet.Count > 0;
        foreach(var cardTarget in _targetSet.ToList())
        {
            cardTarget.ApplyEffects(card.EffectConfigArr);
            RemoveFromSet(cardTarget);
        }
        if (hasTarget)
        {
            card.SetDiscared();

            // Play audio
            foreach(var effectConfig in card.EffectConfigArr)
            {
                if(effectConfig.Type == EffectType.DamageOverTime)
                {
                    AudioManager.Instance.PlayOneShot(AudioNameType.Card_FireSound.ToString(), 0.5f);
                }
                else if(effectConfig.Type == EffectType.Freeze)
                {
                    AudioManager.Instance.PlayOneShot(AudioNameType.Card_Ice.ToString(), 0.5f);
                }
                else if (effectConfig.Type == EffectType.SpeedModify)
                {
                    AudioManager.Instance.PlayOneShot(AudioNameType.Card_Slow.ToString(), 0.5f);
                }
                else if (effectConfig.Type == EffectType.DamageModify || effectConfig.Type == EffectType.FireRateModify)
                {
                    AudioManager.Instance.PlayOneShot(AudioNameType.Card_TowerBoostSound.ToString(), 0.5f);
                }
            }
        }
    }

    private void OnCardTargeting(Vector2 screenPos)
    {
        SetFocusTo(screenPos);
    }

    private void OnCardNotTargeting()
    {
        SetNotFocus();
    }

    private void OnTriggerEnter(Collider other)
    {
        foreach (var tag in _targetTagArr)
        {
            if (other.CompareTag(tag.ToString()))
            {
                AddToSet(other.GetComponent<CardTarget>());
                return;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        foreach (var tag in _targetTagArr)
        {
            if (other.CompareTag(tag.ToString()))
            {
                RemoveFromSet(other.GetComponent<CardTarget>());
                return;
            }
        }
    }

    private void AddToSet(CardTarget target)
    {
        // Hightlight target
        if (target.CompareTag(TagNameType.Attacker.ToString()))
        {
            target.GetComponent<CardTarget>().Highlight(_attackerHighlightMat);
        }
        else if (target.CompareTag(TagNameType.Tower.ToString()))
        {
            target.GetComponent<CardTarget>().Highlight(_towerHighlightMat);
        }
        else
        {
            Debug.LogError("Not implemented");
            return;
        }

        _targetSet.Add(target);
    }

    private void RemoveFromSet(CardTarget target)
    {
        // Un-highlight target
        if (target.CompareTag(TagNameType.Attacker.ToString()) || target.CompareTag(TagNameType.Tower.ToString()))
        {
            target.Unhighlight();
        }
        else
        {
            Debug.LogError("Not implemented");
            return;
        }

        _targetSet.Remove(target);
    }

    private void SetFocusTo(Vector2 screenPos)
    {
        // Try to scale range
        if (!_isFocusing)
        {
            _isFocusing = true;

            DOTween.Kill(_projector);
            DOTween.To(
                () => _projector.orthographicSize,
                x =>
                {
                    _projector.orthographicSize = x;
                    _triggerCol.radius = x;
                },
                _radius,
                0.1f
            );
        }

        // Snap position to y = 0
        Ray ray = GameManager.Instance.TopdownCam.Cam.ScreenPointToRay(screenPos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 worldPos = ray.GetPoint(enter);
            transform.position = worldPos;
        }
    }

    private void SetNotFocus()
    {
        if (_isFocusing)
        {
            _isFocusing = false;

            DOTween.Kill(_projector);
            DOTween.To(
                () => _projector.orthographicSize,
                x =>
                {
                    _projector.orthographicSize = x;
                    _triggerCol.radius = x;
                },
                0f,
                0.1f
            ).SetEase(Ease.InQuad);
        }
    }
}
