using TMPro;
using UnityEngine;
using UnityEngine.UI;

// shows a slider's value as a percentage
public class SliderValueText : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI text;

    private void Awake()
    {
        slider.onValueChanged.AddListener(Refresh);
    }

    private void OnEnable()
    {
        Refresh(slider.value);
    }

    private void Refresh(float value)
    {
        text.text = Mathf.RoundToInt(Mathf.InverseLerp(slider.minValue, slider.maxValue, value) * 100f) + "%";
    }
}
