using Ookii.Dialogs;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseSelector : MonoBehaviour
{
    #region ___ REFERENCES ___
    [Header("References")]

    [SerializeField]
    private TopdownCam _topdownCam;

    [SerializeField]
    private Material _hoveredMat;
    #endregion ___

    #region ___ SETTINGS ___
    private const float DRAG_THRESHOLD = 5f;

    [Header("Settings")]

    [SerializeField]
    private LayerMask _raycastLayer;
    #endregion ___

    #region ___ DATA ___
    private SelectablePartBase _hoveredObj;

    private SelectablePartBase _selectedObj;

    private Collider _raycastHitCol;

    // Mouse drag

    private Vector2 _mouseDownPos;

    private bool _isDragging;
    #endregion ___

    private void OnEnable()
    {
        EventVariances.CardSystem.onCardClicked += OnCardClicked;
        EventVariances.MerchantUI.onUIInteracted += OnMerchantUIInteracted;
    }

    private void OnDisable()
    {
        EventVariances.CardSystem.onCardClicked -= OnCardClicked;
        EventVariances.MerchantUI.onUIInteracted -= OnMerchantUIInteracted;
    }

    private void OnCardClicked(Card card)
    {
        LeaveSelectingObj();
    }

    private void OnMerchantUIInteracted()
    {
        LeaveSelectingObj();
    }

    private void Update()
    {
        if (!_topdownCam.IsManualControl)
        {
            _hoveredObj?.OnMouseLeaved();
            _hoveredObj = null;
            return;
        }
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        if (!Physics.Raycast(mousePosRay, out RaycastHit hit, 1000, _raycastLayer))
        {
            _raycastHitCol = null;
        }
        else
        {
            _raycastHitCol = hit.collider;
        }
        RefreshHoveredObj();
        CheckMouseInteract();
    }

    private Ray mousePosRay => _topdownCam.Cam.ScreenPointToRay(Input.mousePosition);
    private SelectablePartBase newHoveredObj;
    private void RefreshHoveredObj()
    {
        if (_raycastHitCol == null)
        {
            newHoveredObj = null;
        }
        else
        {
            if (_raycastHitCol.gameObject.tag == TagNameType.SelectablePart.ToString())
            {
                newHoveredObj = _raycastHitCol.GetComponent<SelectablePartBase>();
                if (newHoveredObj != null && newHoveredObj == _selectedObj)  // hover obj is not selected obj
                {
                    newHoveredObj = null;
                }
            }
            else
            {
                newHoveredObj = null;
            }
        }
        if (newHoveredObj != _hoveredObj)
        {
            _hoveredObj?.OnMouseLeaved();
            newHoveredObj?.OnMouseHovered(_hoveredMat);
            _hoveredObj = newHoveredObj;
        }
    }

    private void CheckMouseInteract()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            _mouseDownPos = Input.mousePosition;
            _isDragging = false;
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            if (!_isDragging)
            {
                float dist = ((Vector2)Input.mousePosition - _mouseDownPos).magnitude;
                if (dist > DRAG_THRESHOLD)
                    _isDragging = true;
            }
        }

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            if (_isDragging)
                return; // ignore drag release
            if (_raycastHitCol.CompareTag(TagNameType.Ground.ToString()))
            {
                LeaveSelectingObj();
            }
            else if (_hoveredObj != null)
            {
                _selectedObj?.OnMouseLeaved();
                _selectedObj = _hoveredObj;
                _hoveredObj = null;
                _selectedObj.OnMouseSelected();
            }
        }
    }

    public void SelectObj(SelectablePartBase obj)
    {
        LeaveSelectingObj();
        _selectedObj = obj;
        _selectedObj?.OnMouseSelected();
    }

    public void LeaveSelectingObj()
    {
        _selectedObj?.OnMouseLeaved();
        _selectedObj = null;
    }
}
