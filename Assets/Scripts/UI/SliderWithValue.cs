using UnityEngine;
using UnityEngine.UI;

public class SliderWithValue : MonoBehaviour
{

    public Slider Slider;
    public Text Text;
    public string Unit;
    public byte Decimals = 2;

    void OnEnable()
    {
        Slider.onValueChanged.AddListener(ChangeValue);
        ChangeValue(Slider.value);
    }
    void OnDisable()
    {
        Slider.onValueChanged.RemoveAllListeners();
    }

    void ChangeValue(float value)
    {
        Text.text = value.ToString("n" + Decimals) + " " + Unit;
    }


}