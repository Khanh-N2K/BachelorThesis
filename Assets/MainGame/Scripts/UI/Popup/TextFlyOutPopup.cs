using Cysharp.Threading.Tasks;
using N2K;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

public class TextFlyOutPopup : PopupBase
{
    #region ___ REFERENCES ___

    [Header("=== TEXT FLY OUT POPUP ===")]

    [Header("References")]

    [SerializeField]
    private TMP_Text _textPrefab;

    #endregion ___

    #region ___ DATA ___

    private List<TMP_Text> _textList = new();

    private CancellationTokenSource _cts = new();

    #endregion ___

    private void OnDisable()
    {
        _cts.Cancel();
        _cts = new();
        foreach(var text in _textList)
        {
            text.transform.SetParent(transform);
            text.gameObject.SetActive(false);
        }
        _textList.Clear();
    }

    protected override void Initialize()
    {
        base.Initialize();
        foreach (Transform text in transform)
        {
            text.gameObject.SetActive(false);
        }
    }

    public static void ShowPopupFromWorldPos(Vector3 source, string desc, Transform parent, Color? color = null, float size = 30f, float duration = 0.6f, Vector2? offset = null, float scale = 1.25f)
    {
        List<TextFlyOutPopup> popupList = UIManager.Instance.GetAllActivePopups<TextFlyOutPopup>();
        if (popupList.Count > 0)
        {
            popupList[0].ShowTextFromFromWorldPos(source, desc, parent, color, size, duration, offset, scale);
        }
        else
        {
            UIManager.Instance.ShowPopup<TextFlyOutPopup>().ShowTextFromFromWorldPos(source, desc, parent, color, size, duration, offset, scale);
        }
    }

    public static void ShowPopupFromUIPos(RectTransform source, string desc, Transform parent, Color? color = null, float size = 30f, float duration = 0.6f, Vector2? offset = null, float scale = 1.25f)
    {
        List<TextFlyOutPopup> popupList = UIManager.Instance.GetAllActivePopups<TextFlyOutPopup>();
        if (popupList.Count > 0)
        {
            popupList[0].ShowTextFromUIPos(source, desc, parent, color, size, duration, offset, scale);
        }
        else
        {
            UIManager.Instance
                .ShowPopup<TextFlyOutPopup>()
                .ShowTextFromUIPos(source, desc, parent, color, size, duration, offset, scale);
        }
    }

    public void ShowTextFromFromWorldPos(Vector3 source, string desc, Transform parent, Color? color = null, float size = 30f, float duration = 0.6f, Vector2? offset = null, float scale = 1.25f)
    {
        TMP_Text text = GetAvailableText();
        text.text = desc;
        text.color = color ?? Color.white;
        text.fontSize = size;
        text.transform.SetParent(parent);
        _textList.Add(text);

        Vector3 startScreenPos = GameManager.Instance.TopdownCam.Cam.WorldToScreenPoint(source);
        AnimateText(text, startScreenPos, duration, offset, scale, () =>
        {
            _textList.Remove(text);
            text.transform.SetParent(transform);
            text.gameObject.SetActive(false);
            if (_textList.Count == 0)
            {
                Hide();
            }
        }).Forget();
    }

    public void ShowTextFromUIPos(RectTransform source, string desc, Transform parent, Color? color = null, float size = 30f, float duration = 0.6f, Vector2? offset = null, float scale = 1.25f)
    {
        TMP_Text text = GetAvailableText();
        text.text = desc;
        text.color = color ?? Color.white;
        text.fontSize = size;
        text.transform.SetParent(parent);

        _textList.Add(text);

        Vector3 startScreenPos = source.position;

        AnimateText(text, startScreenPos, duration, offset, scale, () =>
        {
            _textList.Remove(text);
            text.transform.SetParent(transform);
            text.gameObject.SetActive(false);

            if (_textList.Count == 0)
                Hide();
        }).Forget();
    }

    private TMP_Text GetAvailableText()
    {
        TMP_Text text;
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                continue;
            }
            text = child.GetComponent<TMP_Text>();
            if (child != null)
            {
                child.gameObject.SetActive(true);
                return text;
            }
        }
        text = Instantiate(_textPrefab, transform);
        text.gameObject.SetActive(true);
        return text;
    }

    private async UniTask AnimateText(TMP_Text text, Vector3 startScreenPos, float duration, Vector2? uiOffset, float scale, Action onFinished)
    {
        Vector3 targetPos = startScreenPos + (Vector3)(uiOffset ?? Vector2.up * 50f);

        float timer = 0f;
        Color color;

        while (timer < duration)
        {
            float t = timer / duration;

            text.transform.position = Vector3.Lerp(startScreenPos, targetPos, t);

            color = text.color;
            color.a = Mathf.Lerp(1f, 0f, t);
            text.color = color;

            text.transform.localScale = Mathf.Lerp(1f, scale, t) * Vector3.one;

            timer += Time.deltaTime;
            await UniTask.DelayFrame(1, cancellationToken: _cts.Token);
        }

        onFinished?.Invoke();

    }
}
