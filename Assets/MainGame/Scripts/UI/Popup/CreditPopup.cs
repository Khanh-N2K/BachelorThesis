using N2K;
using UnityEngine;

public class CreditPopup : PopupBase
{
    public void OpenFacebookURL()
    {
        Application.OpenURL("https://www.facebook.com/Khanh.N2K");
    } 

    public void OpenGitHubURL()
    {
        Application.OpenURL("https://github.com/Khanh-N2K");
    }

    public void OpenYoutubeURL()
    {
        Application.OpenURL("https://www.youtube.com/%40Khanh.N2K");
    }

}
