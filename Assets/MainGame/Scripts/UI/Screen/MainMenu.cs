using Cysharp.Threading.Tasks;
using N2K;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : ScreenBase
{
    [Header("=== MAIN MENU ===")]

    [SerializeField]
    private Button _startBtn;

    [SerializeField]
    private Button _settingBtn;

    [SerializeField]
    private Button _creditBtn;

    [SerializeField]
    private Button _exitBtn;

    protected override void Initialize()
    {
        base.Initialize();
        _startBtn.onClick.AddListener(OnStartBtnClicked);
        _settingBtn.onClick.AddListener(OnSettingBtnClicked);
        _creditBtn.onClick.AddListener(OnCreditBtnClicked);
        _exitBtn.onClick.AddListener(OnExitBtnClicked);

        void OnStartBtnClicked()
        {
            AudioManager.Instance.PlayOneShot(AudioNameType.ClickButton.ToString(), 0.5f);
            UIManager.Instance.ShowScreen<IngameScreen>();
            GameManager.Instance.SetNewGame();
        }

        void OnSettingBtnClicked()
        {
            AudioManager.Instance.PlayOneShot(AudioNameType.ClickButton.ToString(), 0.5f);
            UIManager.Instance.ShowPopup<SettingPopup>();
        }
        
        void OnCreditBtnClicked()
        {
            AudioManager.Instance.PlayOneShot(AudioNameType.ClickButton.ToString(), 0.5f);
            UIManager.Instance.ShowPopup<CreditPopup>();
        }

        void OnExitBtnClicked()
        {
            AudioManager.Instance.PlayOneShot(AudioNameType.ClickButton.ToString(), 0.5f);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
