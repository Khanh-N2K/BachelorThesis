using N2K;
using Ookii.Dialogs;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GraphicsBuffer;

public class TopdownCam : MonoBehaviour
{
    #region ___ REFERENECES ___

    [Header("References")]

    [SerializeField]
    private Camera _cam;

    public Camera Cam => _cam;

    private Transform _cameraTransform => _cam.transform;

    [SerializeField]
    private BoxCollider _cameraBounds;

    #endregion ___

    #region ___ SETTINGS ___

    [Header("Settings")]

    [SerializeField]
    private float _movementTime = 5f;

    [SerializeField]
    private float _rotationAmount = 0.8f;

    [SerializeField]
    private float _zoomAmount = -10;

    [SerializeField]
    private Vector2 _zoomRange = new Vector2(30, 200);

    [Header("Settings - Gizmos")]

    [SerializeField]
    private bool _showGizmosDebug = true;

    #endregion ___

    #region ___ DATA ___

    // ZOOM

    private Vector3 _zoomDir;

    private float _currentZoom => _cameraTransform.localPosition.y;

    // SNAP BACK TO DEFAULT POS

    private Vector3 _snapPos = Vector3.zero;

    private float _snapZoom = 160;

    // FOCUS TO TARGET

    private bool _isFocusingToTarget = false;

    private Vector3 _focusPos;

    private Transform _focusTransform;

    private Action _onClosedToTargetFirstTime;

    private bool _isInvokedOnCloseToTargetFirstTime;

    // MANUAL CONTROL

    private bool _isShowingIngameScreen = false;

    private int _nonControllableUICount = 0;

    private bool _isShowingMerchantMainPanel = false;

    private bool _isUIBlocking => !_isShowingIngameScreen || _nonControllableUICount > 0 || Card.draggingCard != null || _isShowingMerchantMainPanel;

    public bool IsManualControl => !_isFocusingToTarget && !_isUIBlocking;

    private Vector3 _dragStartPosition;

    private Vector3 _rotateStartPosition;

    // TARGET TRANSFORM

    private float _finalMovementTime;

    private Vector3 _targetPosition;

    private Quaternion _targetRotation;

    private float _targetZoom;

    #endregion ___

    private void Awake()
    {
        _targetPosition = transform.position;
        _targetRotation = transform.rotation;
        _zoomDir = _cameraTransform.localPosition / _cameraTransform.localPosition.y;
        _targetZoom = _currentZoom;
    }

    private void OnEnable()
    {
        UIManager.Instance.onScreenShown += OnScreenShown;
        UIManager.Instance.onScreenHidden += OnScreenHidden;
        UIManager.Instance.onPopupShown += OnPopupShown;
        UIManager.Instance.onPopupHidden += OnPopupHidden;

        EventVariances.MerchantUI.onShowMainPanel += OnShowMainMerchantPanel;
        EventVariances.MerchantUI.onHideMainPanel += OnHideMainMerchantPanel;
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.onScreenShown -= OnScreenShown;
            UIManager.Instance.onScreenHidden -= OnScreenHidden;
            UIManager.Instance.onPopupShown -= OnPopupShown;
            UIManager.Instance.onPopupHidden -= OnPopupHidden;
        }
        EventVariances.MerchantUI.onShowMainPanel -= OnShowMainMerchantPanel;
        EventVariances.MerchantUI.onHideMainPanel -= OnHideMainMerchantPanel;
    }

    private void OnScreenShown(ScreenBase newScreen)
    {
        if (newScreen is IngameScreen)
        {
            _isShowingIngameScreen = true;
        }
    }

    private void OnScreenHidden(ScreenBase oldScreen)
    {
        if (oldScreen is IngameScreen)
        {
            _isShowingIngameScreen = false;
        }
    }

    private void OnPopupShown(PopupBase newPopup)
    {
        if (newPopup is not TowerBuildingPopup && newPopup is not TowerOptionPopup && newPopup is not TextFlyOutPopup)
        {
            _nonControllableUICount++;
        }
    }

    private void OnPopupHidden(PopupBase oldPopup)
    {
        if (oldPopup is not TowerBuildingPopup && oldPopup is not TowerOptionPopup && oldPopup is not TextFlyOutPopup)
        {
            _nonControllableUICount--;
        }
    }

    private void OnShowMainMerchantPanel()
    {
        _isShowingMerchantMainPanel = true;
    }

    private void OnHideMainMerchantPanel()
    {
        _isShowingMerchantMainPanel = false;
    }

    private void Update()
    {
        if (_isFocusingToTarget)
        {
            HandleFocusToTarget();
            _finalMovementTime = _movementTime / 3;
        }
        else if (!_isUIBlocking)
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                HandleMouseInput();
            }
            _finalMovementTime = _movementTime;
        }

        // Clamp pos
        _targetPosition = ClampPositionXZ(_targetPosition);

        // Apply input
        transform.rotation = Quaternion.Lerp(transform.rotation, _targetRotation, Time.deltaTime * _finalMovementTime);
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _finalMovementTime);
        _cameraTransform.localPosition = Vector3.Lerp(_cameraTransform.localPosition, _targetZoom * _zoomDir, Time.deltaTime * _finalMovementTime);
    }

    #region ___ FOCUS TO TARGET ___

    public void StartFocusTo(Vector3 targetPos, bool isImmediately = false, Action onClosedToTargetFirstTime = null)
    {
        _isFocusingToTarget = true;
        _onClosedToTargetFirstTime = onClosedToTargetFirstTime;
        _isInvokedOnCloseToTargetFirstTime = false;
        _focusPos = targetPos;
        _focusTransform = null;
        if (isImmediately)
        {
            transform.position = targetPos;
            onClosedToTargetFirstTime?.Invoke();
            _isInvokedOnCloseToTargetFirstTime = true;
        }
    }

    public void StartFocusTo(Vector3 targetPos, float zoomSize, bool isImmediately = false, Action onClosedToTargetFirstTime = null)
    {
        _isFocusingToTarget = true;
        _onClosedToTargetFirstTime = onClosedToTargetFirstTime;
        _isInvokedOnCloseToTargetFirstTime = false;
        _focusPos = targetPos;
        _focusTransform = null;
        _targetZoom = zoomSize;
        if (isImmediately)
        {
            transform.position = targetPos;
            _cameraTransform.localPosition = _zoomDir * _targetZoom;
            onClosedToTargetFirstTime?.Invoke();
            _isInvokedOnCloseToTargetFirstTime = true;
        }
    }

    public void StartFocusTo(Transform target, bool isImmediately = false, Action onClosedToTargetFirstTime = null)
    {
        _isFocusingToTarget = true;
        _onClosedToTargetFirstTime = onClosedToTargetFirstTime;
        _isInvokedOnCloseToTargetFirstTime = false;
        _focusTransform = target;
        if (isImmediately)
        {
            transform.position = target.position;
            onClosedToTargetFirstTime?.Invoke();
            _isInvokedOnCloseToTargetFirstTime = true;
        }
    }

    public void StartFocusTo(Transform target, float zoomSize, bool isImmediately = false, Action onClosedToTargetFirstTime = null)
    {
        _isFocusingToTarget = true;
        _onClosedToTargetFirstTime = onClosedToTargetFirstTime;
        _isInvokedOnCloseToTargetFirstTime = false;
        _focusTransform = target;
        _targetZoom = zoomSize;
        if (isImmediately)
        {
            transform.position = target.position;
            _cameraTransform.localPosition = _zoomDir * _targetZoom;
            onClosedToTargetFirstTime?.Invoke();
            _isInvokedOnCloseToTargetFirstTime = true;
        }
    }

    public void StopFocusToTarget()
    {
        _isFocusingToTarget = false;
        _targetPosition = transform.position;
        _targetZoom = _currentZoom;
    }

    private void HandleFocusToTarget()
    {
        if (_focusTransform == null)
        {
            _targetPosition = _focusPos;
        }
        else
        {
            _targetPosition = _focusTransform.position;
        }
        if (!_isInvokedOnCloseToTargetFirstTime
            && Vector3.Distance(transform.position, _targetPosition) < 1f
            && Mathf.Abs(_currentZoom - _targetZoom) < 2f)
        {
            _onClosedToTargetFirstTime?.Invoke();
            _isInvokedOnCloseToTargetFirstTime = true;
        }
    }

    #endregion ___

    #region ___ MANUAL CONTROL ___

    public void SetSnapBackData(Vector3 position, float zoom)
    {
        _snapPos = position;
        _snapZoom = zoom;
    }

    private void HandleMouseInput()
    {
        // Scroll to zoom
        if (Input.mouseScrollDelta.y != 0)
        {
            _targetZoom += Input.mouseScrollDelta.y * _zoomAmount;
            _targetZoom = Mathf.Clamp(_targetZoom, _zoomRange.x, _zoomRange.y);
        }
        // Drag to move cam
        if (Input.GetMouseButtonDown(0))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            float entry;

            if (plane.Raycast(ray, out entry))
            {
                _dragStartPosition = ray.GetPoint(entry);
            }
        }
        if (Input.GetMouseButton(0))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            float entry;

            if (plane.Raycast(ray, out entry))
            {
                _targetPosition = transform.position + _dragStartPosition - ray.GetPoint(entry);
            }
        }
        // Right mouse to rotate
        if (Input.GetMouseButtonDown(1))
        {
            _rotateStartPosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(1))
        {
            Vector3 difference = _rotateStartPosition - Input.mousePosition;
            _rotateStartPosition = Input.mousePosition;
            _targetRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 5) * _rotationAmount);
        }
        // Middle mouse to snap back to default pos
        if (Input.GetMouseButton(2) || Input.GetKeyDown(KeyCode.Space))
        {
            // Snap back
            _targetPosition = _snapPos;
            _targetZoom = _snapZoom;
        }
    }

    #endregion ___

    #region ___ CLAMP ___

    private Vector3 ClampPositionXZ(Vector3 targetPos)
    {
        if (_cameraBounds == null)
            return targetPos;

        Bounds bounds = _cameraBounds.bounds;

        targetPos.x = Mathf.Clamp(targetPos.x, bounds.min.x, bounds.max.x);
        targetPos.z = Mathf.Clamp(targetPos.z, bounds.min.z, bounds.max.z);
        // Y is NOT clamped

        return targetPos;
    }

    #endregion ___

    #region ___ GIZMOS ___

    private void OnDrawGizmos()
    {
        if(!_showGizmosDebug)
        {
            return;
        }    

        if (_cameraBounds == null)
            return;

        Bounds bounds = _cameraBounds.bounds;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // Optional: draw camera target position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_targetPosition, 0.5f);
    }

    #endregion ___
}
