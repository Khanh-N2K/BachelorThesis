using UnityEngine;
using N2K;
using UnityEngine.SceneManagement;

public class PausePopup : PopupBase
{
    public void ResumeGame()
    {
        Time.timeScale = 1;
        Hide();
    }

    public void OpenMainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }
}
