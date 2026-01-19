using N2K;
using UnityEngine.SceneManagement;

public class LosePopup : PopupBase
{
    public void OpenMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    private void OnEnable()
    {
        AudioManager.Instance.PlayOneShot(AudioNameType.LoseGame.ToString(), 0.5f);
    }
}
