using Cysharp.Threading.Tasks;
using DG.Tweening;
using N2K;
using System.Collections;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    protected override bool IsDontDestroyOnLoad => false;

    [SerializeField]
    private RoundManager _roundManager;

    public RoundManager RoundManager => _roundManager;

    [SerializeField]
    private TopdownCam _topdownCam;

    public TopdownCam TopdownCam => _topdownCam;

    [SerializeField]
    private MouseSelector _mouseSelector;

    public MouseSelector MouseSelector => _mouseSelector;

    protected override void OnSingletonAwake()
    {
        DOTween.SetTweensCapacity(1500, 50);
    }

    void Start()
    {
        UIManager.Instance.ShowScreen<MainMenu>();
        AudioManager.Instance.PlayMusic(AudioNameType.BackgroundMusic.ToString(), 0.4f, true);
    }

    public void SetNewGame()
    {
        _roundManager.StartNewRound();
    }
}
