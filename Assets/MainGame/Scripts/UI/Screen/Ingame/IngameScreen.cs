using DG.Tweening;
using JetBrains.Annotations;
using N2K;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngameScreen : ScreenBase
{
    [Header("=== INGAME SCREEN ===")]

    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private Button _readyBtn;

    [SerializeField]
    private GameObject _defenderCountFrame;

    [SerializeField]
    private TMP_Text _defenderCountText;

    [SerializeField]
    private GameObject _attackerCountFrame;

    [SerializeField]
    private TMP_Text _attackerCountText;

    [SerializeField]
    private TMP_Text _waveCountText;

    [SerializeField]
    private TMP_Text _instructionText;

    public TMP_Text InstructionText => _instructionText;

    [Header("References - Text Fly Holder")]

    [SerializeField]
    private Transform _textFlyHolderBack;

    public Transform TextFlyHolderBack => _textFlyHolderBack;

    [SerializeField]
    private Transform _textFlyHolderFront;

    public Transform TextFlyHolderFront => _textFlyHolderFront;

    [Header("Referenes - Card Hand")]

    [SerializeField]
    private CardHolder _cardHolder;

    #endregion ___

    #region ___ DATA ___

    private int _originalDefenderCount;

    private int _originalAttackerCount;

    #endregion ___

    private void Awake()
    {
        _instructionText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (GameManager.Instance.RoundManager.State == RoundState.None
            || GameManager.Instance.RoundManager.State == RoundState.BackgroundSpawning)
        {
            DisableRoundInforVisual();
        }
        GameManager.Instance.RoundManager.onStateChanged += OnRoundStateChanged;
        GameManager.Instance.RoundManager.DefenderManager.Spawner.onDefenderSpawned += OnDefenderSpawned;
        GameManager.Instance.RoundManager.DefenderManager.onDefenderSetUpdated += OnDefenderSetUpdated;
        GameManager.Instance.RoundManager.onWaveUpdated += OnAttackerSpawned;
        EventVariances.onAttackerCountUpdated += OnAttackerCountUpdate;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RoundManager.onStateChanged -= OnRoundStateChanged;
            GameManager.Instance.RoundManager.DefenderManager.Spawner.onDefenderSpawned -= OnDefenderSpawned;
            GameManager.Instance.RoundManager.DefenderManager.onDefenderSetUpdated -= OnDefenderSetUpdated;
            GameManager.Instance.RoundManager.onWaveUpdated -= OnAttackerSpawned;
            EventVariances.onAttackerCountUpdated -= OnAttackerCountUpdate;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Time.timeScale != 0)
            {
                Time.timeScale = 0;
                UIManager.Instance.ShowPopup<PausePopup>();
            }
            else
            {
                Time.timeScale = 1;
                UIManager.Instance.HideAllPopups<PausePopup>();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (GameManager.Instance.RoundManager.State != RoundState.PlayerSetup
                && GameManager.Instance.RoundManager.State != RoundState.WaveSimulating)
            {
                return;
            }
            if (_instructionText.gameObject.activeSelf)
            {
                _instructionText.gameObject.SetActive(false);
            }
            else
            {
                _instructionText.gameObject.SetActive(true);
            }
        }
    }

    protected override void Initialize()
    {
        base.Initialize();
        _readyBtn.onClick.RemoveAllListeners();
        _readyBtn.onClick.AddListener(OnClickReadyBtn);
        _readyBtn.gameObject.SetActive(false);
    }

    public void ClearRoundUI()
    {
        _defenderCountFrame.SetActive(false);
        _waveCountText.gameObject.SetActive(false);
    }


    #region ___ READY BTN ___
    private void OnClickReadyBtn()
    {
        AudioManager.Instance.PlayOneShot(AudioNameType.ClickButton.ToString(), 0.5f);
        _readyBtn.gameObject.SetActive(false);
        GameManager.Instance.RoundManager.ChangeState(RoundState.WaveSimulating);
    }

    public void SetActiveReadyBtn(bool active)
    {
        _readyBtn.gameObject.SetActive(active);
    }
    #endregion ___

    private void OnRoundStateChanged()
    {
        if (GameManager.Instance.RoundManager.State == RoundState.BackgroundSpawning)
        {
            DisableRoundInforVisual();
        }
    }

    private void DisableRoundInforVisual()
    {
        _defenderCountFrame.SetActive(false);
        _attackerCountFrame.SetActive(false);
        _waveCountText.gameObject.SetActive(false);
    }

    private void OnDefenderSpawned()
    {
        _originalDefenderCount = GameManager.Instance.RoundManager.DefenderManager.DefenderCount;
        _defenderCountText.text = $"{_originalDefenderCount}/{_originalDefenderCount}";
        _defenderCountFrame.SetActive(true);
    }

    private void OnDefenderSetUpdated()
    {
        if (GameManager.Instance.RoundManager.State != RoundState.DefenderSpawning)
        {
            _defenderCountText.text = $"{GameManager.Instance.RoundManager.DefenderManager.DefenderCount}/{_originalDefenderCount}";
        }
    }

    private void OnAttackerSpawned()
    {
        // update wave count
        _waveCountText.text = $"Wave {GameManager.Instance.RoundManager.CurrentWave}/{GameManager.Instance.RoundManager.TotalWave}";
        _waveCountText.gameObject.SetActive(true);
        // Update attacker count
        _originalAttackerCount = GameManager.Instance.RoundManager.AttackerManager.AttackerCount;
        _attackerCountText.text = $"{_originalAttackerCount}/{_originalAttackerCount}";
        _attackerCountFrame.SetActive(true);
    }

    private void OnAttackerCountUpdate()
    {
        if (GameManager.Instance.RoundManager.State != RoundState.AttackerSpawing)
        {
            _attackerCountText.text = $"{GameManager.Instance.RoundManager.AttackerManager.AttackerCount}/{_originalAttackerCount}";
        }
    }
}
