using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField]
    private Image _filler;

    public void RefreshVisual(float healthPercent)
    {
        _filler.fillAmount = healthPercent;
    }
}
