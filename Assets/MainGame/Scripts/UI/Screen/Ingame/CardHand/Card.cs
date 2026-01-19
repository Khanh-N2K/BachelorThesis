using DG.Tweening;
using N2K;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Card : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region ___ REFERENCES ___

    // External references

    private CardHolder _holder;

    // Other 

    private Canvas _rootCanvas;

    [Header("References")]

    [SerializeField]
    private RectTransform _rectTransform;

    public RectTransform RectTransform => _rectTransform;

    [SerializeField]
    private RectTransform _localPosRoot;

    [SerializeField]
    private RectTransform _scaleRoot;

    [SerializeField]
    private Image _iconImg;

    [SerializeField]
    private TMP_Text _descriptionText;

    #endregion ___

    #region ___ SETTINGS ___

    [Header("Settings")]

    [SerializeField]
    private float followSpeed = 25f;

    private Vector2 _targetAnchoredPos;

    #endregion ___

    #region ___ DATA ___

    public static Card draggingCard;

    private bool _isInCardArea = true;

    private bool _isInSellArea = false;

    private Vector2 _startAnchoredPos;

    public Vector2 StartAnchorPos => _startAnchoredPos;

    // CONFIG

    private CardConfig _config;

    public float EffectRadius => _config.dragEffectRadius;

    public TagNameType[] TagNameArr => _config.targetTagArr;

    public EffectConfigBase[] EffectConfigArr => _config._effectArr;

    public int SellPrice => _config.sellPrice;

    #endregion ___

    private void Start()
    {
        _rootCanvas = GetComponentInParent<Canvas>();
        _descriptionText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (draggingCard != this)
            return;

        _rectTransform.anchoredPosition = Vector2.Lerp(
            _rectTransform.anchoredPosition,
            _targetAnchoredPos,
            followSpeed * Time.deltaTime
        );
    }

    #region ___ MOVEMENT ___

    public void SetStartAnchorPos(Vector2 anchorPos)
    {
        _startAnchoredPos = anchorPos;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform.parent as RectTransform
            , eventData.position
            , _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera
            , out Vector2 localMousePos);
        _rectTransform.DOKill();
        _rectTransform.DOAnchorPos(localMousePos, 0.2f);

        _isInCardArea = true;

        EventVariances.CardSystem.onCardClicked?.Invoke(this);

        AudioManager.Instance.PlayOneShot(AudioNameType.ClickButton.ToString(), 0.5f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _rectTransform.DOKill();
        _rectTransform.DOAnchorPos(_startAnchoredPos, 0.2f);

        _scaleRoot.DOKill();
        _scaleRoot.DOScale(1f, 0.2f);

        EventVariances.CardSystem.onEndHoveredOnMap?.Invoke();
        EventVariances.CardSystem.onCardReleased?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(draggingCard != null && draggingCard != this)
        {
            _localPosRoot.DOKill();
            _localPosRoot.DOAnchorPos(Vector2.zero, 0.2f);
            _descriptionText.gameObject.SetActive(false);
        }
        else
        {
            _localPosRoot.DOKill();
        _localPosRoot.DOAnchorPos(Vector2.up * 100f, 0.2f);
        _descriptionText.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _localPosRoot.DOKill();
        _localPosRoot.DOAnchorPos(Vector2.zero, 0.2f);
        _descriptionText.gameObject.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        draggingCard = this;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Follow mouse pos
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
           _rectTransform.parent as RectTransform,
           eventData.position,
           _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
               ? null
               : _rootCanvas.worldCamera,
           out _targetAnchoredPos
       );

        Vector2 mousePos = eventData.position;

        // Check drag on UI
        if (RaycastUtil.RaycastUI(mousePos, out var uiHits))
        {
            foreach (var hit in uiHits)
            {
                // Skip self
                if (hit.gameObject.transform.IsChildOf(transform))
                {
                    continue;
                }
                // Drop on card area
                if (hit.gameObject.CompareTag(TagNameType.Card.ToString()) || hit.gameObject.CompareTag(TagNameType.CardHolder.ToString()))
                {
                    if (!_isInCardArea)
                    {
                        _isInCardArea = true;

                        _scaleRoot.DOKill();
                        _scaleRoot.DOScale(1f, 0.3f);

                        EventVariances.CardSystem.onEndHoveredOnMap?.Invoke();
                    }
                    return;
                }
                if (hit.gameObject.CompareTag(TagNameType.CardSellArea.ToString()))
                {
                    if (!_isInSellArea)
                    {
                        _isInSellArea = true;

                        _scaleRoot.DOKill();
                        _scaleRoot.DOScale(0.3f, 0.2f);

                        EventVariances.CardSystem.onStartHoveredOnSellArea?.Invoke(this);
                    }
                    return;
                }
            }

            if (_isInSellArea)
            {
                _isInSellArea = false;

                _scaleRoot.DOKill();
                _scaleRoot.DOScale(1f, 0.2f);

                EventVariances.CardSystem.onEndHoveredOnSellArea?.Invoke();
            }

            return;
        }

        if (_isInSellArea)
        {
            _isInSellArea = false;

            _scaleRoot.DOKill();
            _scaleRoot.DOScale(1f, 0.2f);

            EventVariances.CardSystem.onEndHoveredOnSellArea?.Invoke();
        }

        // Check drag on world map
        if (RaycastUtil.RaycastWorld(Camera.main, mousePos, out RaycastHit worldHit, LayerMask.GetMask(LayerNameType.MouseSelection.ToString())))
        {
            if (_isInCardArea)
            {
                _isInCardArea = false;

                _scaleRoot.DOKill();
                _scaleRoot.DOScale(0f, 0.3f);
            }

            EventVariances.CardSystem.onStartHoveredOnMap?.Invoke(mousePos + Vector2.up * 20f);
            return;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        draggingCard = null;
    }

    #endregion ___

    public void Initialize(CardHolder holder, CardConfig config)
    {
        _holder = holder;
        _config = config;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        _iconImg.sprite = _config.icon;
        _descriptionText.text = _config.description;
    }

    public void SetDiscared()
    {
        _holder.UnregisterCard(this);
        draggingCard = null;
    }
}
