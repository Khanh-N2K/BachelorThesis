using System;
using Cysharp.Threading.Tasks;
using N2K;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private BackgroundBlockManager _backgroundBlockManager;

    public BackgroundBlockManager BackgroundBlockManager => _backgroundBlockManager;

    private BackgroundBlockSpawner _backgroundBlockSpawner => _backgroundBlockManager.Spawner;

    [SerializeField]
    private AttackerManager _attackerManager;

    public AttackerManager AttackerManager => _attackerManager;

    private AttackerSpawner _attackerSpawner => _attackerManager.Spawner;

    [SerializeField]
    private DefenderManager _defenderManager;

    public DefenderManager DefenderManager => _defenderManager;

    [SerializeField]
    private TowerManager _towerManager;

    public TowerManager TowerManager => _towerManager;

    [SerializeField]
    private EffectManager _effectManager;

    public EffectManager EffectManager => _effectManager;

    [SerializeField]
    private CardManager _cardManger;

    public CardManager CardManger => _cardManger;

    [Header("References - Map")]

    [SerializeField]
    private MapData _mapData;

    public MapData MapData => _mapData;

    [SerializeField]
    private UnitFormation _unitFormation;

    public UnitFormation UnitFormation => _unitFormation;

    #endregion ___

    #region ___ SETTINGS ___

    [Header("Settings")]

    [SerializeField]
    private RoundConfigSO _configSO;

    public float StepDuration => _configSO.StepDuration;

    #endregion ___

    #region ___ DATA ___

    private RoundState _state = RoundState.None;

    public RoundState State => _state;

    public Action onStateChanged;

    private int _currentWave;

    public int CurrentWave => _currentWave;

    private int _totalWave;

    public int TotalWave => _totalWave;

    public Action onWaveUpdated;

    // Currency

    private int _goldCount;

    public int GoldCount => _goldCount;

    private int _diamondCount;

    public int DiamondCount => _diamondCount;

    public Action<int> onGoldChanged;   // new gold

    public Action<int> onDiamondChanged;    // new diamond

    #endregion ___

    private void Awake()
    {
        _backgroundBlockManager.Initialize(this);
        _attackerManager.Initialize(this);
        _defenderManager.Initialize(this);
        _towerManager.Initialize(this);
    }

    public void StartNewRound()
    {
        // Setup data for round
        _currentWave = 1;
        _totalWave = _configSO.TotalWave;
        SetGoldCount(_configSO.StartGold);
        SetDiamondCount(_configSO.StartDiamond);
        _mapData.GenerateMapData();
        // Start animate
        GameManager.Instance.TopdownCam.SetSnapBackData(_mapData.GetCenterMapPos(), 60);
        GameManager.Instance.TopdownCam.StartFocusTo(_mapData.GetCenterMapPos(), 135f, isImmediately: true);
        UIManager.Instance.ShowPopup<InforPopup>(onHidden: () => ChangeState(RoundState.BackgroundSpawning))
            .SetText("Ready...?");
    }

    public void ChangeState(RoundState state)
    {
        _state = state;
        switch (_state)
        {
            case RoundState.BackgroundSpawning:
                HandleBackgroundSpawning().Forget();
                break;
            case RoundState.DefenderSpawning:
                HandleDefenderSpawning();
                break;
            case RoundState.AttackerSpawing:
                HandleAttackerSpawning().Forget();
                break;
            case RoundState.PlayerSetup:
                HandlePlayerSetup();
                break;
            case RoundState.WaveSimulating:
                HandleWaveSimulating().Forget();
                break;
            case RoundState.EndWave:
                HandleEndWave().Forget();
                break;
            case RoundState.Win:
                HandleWin();
                break;
            case RoundState.Lose:
                HandleLose();
                break;
        }
        onStateChanged?.Invoke();
    }

    #region ___ BACKGROUND SPAWNING STATE ___

    private async UniTask HandleBackgroundSpawning()
    {
        await _backgroundBlockSpawner.SpawnBackgroundObjs();
        ChangeState(RoundState.DefenderSpawning);
    }

    #endregion ___

    #region ___ DEFENDER SPAWNING STATE ___
    private void HandleDefenderSpawning()
    {
        GameManager.Instance.TopdownCam.StartFocusTo(_mapData.GetCenterMapPos(), 60f, onClosedToTargetFirstTime:
            () => SpawnDefenders().Forget());
    }

    private async UniTask SpawnDefenders()
    {
        await _defenderManager.Spawner.SpawnRandomDefenders();
        ChangeState(RoundState.AttackerSpawing);
    }
    #endregion ___

    #region ___ ATTACKER SPAWNING STATE ___
    private async UniTask HandleAttackerSpawning()
    {
        await _attackerSpawner.SpawnAttackers();
        onWaveUpdated?.Invoke();
        ChangeState(RoundState.PlayerSetup);
    }
    #endregion ___

    private void HandlePlayerSetup()
    {
        _attackerManager.FindPathAtStartOfWave().Forget();
        UIManager.Instance.ShowPopup<InforPopup>(onHidden: () =>
        {
            GameManager.Instance.TopdownCam.StopFocusToTarget();
            if (UIManager.Instance.TryGetCurrentScreen(out IngameScreen screen))
            {
                screen.SetActiveReadyBtn(true);
                if(_currentWave == 1)
                {
                    screen.InstructionText.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("Ingame screen should be found");
            }
        }).SetText($"Prepare for wave {_currentWave}!");
    }

    private async UniTask HandleWaveSimulating()
    {
        await UniTask.Delay(200);
        UIManager.Instance.ShowPopup<InforPopup>(onHidden: () =>
        {
            _attackerManager.StartWaveSimulating().Forget();
            _towerManager.ChangeTowersState(TowerState.Combat);
        }).SetText($"Zombies are starting to move!");
    }

    private async UniTask HandleEndWave()
    {
        GameManager.Instance.MouseSelector.LeaveSelectingObj();
        _towerManager.ChangeTowersState(TowerState.Inactive);
        _currentWave++;
        if (_defenderManager.DefenderCount == 0)
        {
            ChangeState(RoundState.Lose);
        }
        else if (_currentWave <= _totalWave)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            UIManager.Instance.ShowPopup<InforPopup>(onHidden: () => ChangeState(RoundState.AttackerSpawing))
                .SetText("Next wave's coming!");
        }
        else
        {
            ChangeState(RoundState.Win);
        }
    }

    private void HandleWin()
    {
        UIManager.Instance.ShowPopup<WinPopup>();
    }

    private void HandleLose()
    {
        UIManager.Instance.ShowPopup<LosePopup>();
    }

    #region ___ CURRENCY ___

    public void SetGoldCount(int count)
    {
        _goldCount = count;
        onGoldChanged?.Invoke(count);
    }

    public void AddGold(int amount)
    {
        _goldCount += amount;
        _goldCount = _goldCount < 0 ? 0 : _goldCount;
        onGoldChanged?.Invoke(_goldCount);
    }

    public void SetDiamondCount(int count)
    {
        _diamondCount = count;
        onDiamondChanged?.Invoke(count);
    }

    public void AddDiamond(int amount)
    {
        _diamondCount += amount;
        _diamondCount = _diamondCount < 0 ? 0 : _diamondCount;
        onDiamondChanged?.Invoke(_diamondCount);
    }

    #endregion ___
}
