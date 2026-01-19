using N2K;
using UnityEngine.SceneManagement;

public class WinPopup : PopupBase
{
    public void OpenMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    private void OnEnable()
    {
        AudioManager.Instance.PlayOneShot(AudioNameType.WinGame.ToString(), 0.5f);
    }

}
