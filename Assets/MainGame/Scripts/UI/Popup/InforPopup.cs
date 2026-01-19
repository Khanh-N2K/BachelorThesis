using Cysharp.Threading.Tasks;
using N2K;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class InforPopup : PopupBase
{
    [Header("=== INFOR POPUP ===")]

    #region ___ REFERENCES ___
    [Header("References")]

    [SerializeField]
    private TMP_Text _text;
    #endregion ___

    #region ___ SETTINGS ___
    [Header("Settings")]

    [SerializeField]
    private float _autoHideTime = 1;
    #endregion ___

    #region ___ DATA ___
    private CancellationTokenSource _cts;
    #endregion ___

    private void OnEnable()
    {
        // Countdown
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        CountdownAutoHide().Forget();
        // Random pos
        GetComponent<RectTransform>().anchoredPosition = new Vector2(
            Random.Range(-50, 50),
            Random.Range(-50, 50)
        );
    }

    public void SetText(string content, float size = 50)
    {
        _text.text = content;
        _text.fontSize = size;
    }

    public void SetAutoHideTime(float autoHideTime)
    {
        _autoHideTime = autoHideTime;
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        CountdownAutoHide().Forget();
    }

    async UniTask CountdownAutoHide()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(_autoHideTime), cancellationToken: _cts.Token);
        Hide();
    }
}
