using SobGameJam.Events;
using UnityEngine;
using UnityEngine.UI;

public class BatterySlider : MonoBehaviour
{
    [SerializeField] private IntEventChannelSO OnRoundStartEvent;
    [SerializeField] private FloatEventChannelSO OnTimeChangeEvent;

    [SerializeField] private Slider batterySlider;
    [SerializeField] private Gradient batterySliderColor;
    [SerializeField] private Image fillImage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        if(OnRoundStartEvent!= null)
        {
            OnRoundStartEvent.OnEventRaised += ShowSlilder;
        }

        if(OnTimeChangeEvent!= null)
        {
            OnTimeChangeEvent.OnEventRaised += UpdateSlider;
        }
    }

    private void UpdateSlider(float arg0)
    {
        if (batterySlider != null)
        {
            batterySlider.value = arg0;
            fillImage.color = batterySliderColor.Evaluate(arg0);
        }
        else
        {
            Debug.Log("Drag the slider");
        }
    }

    private void OnDisable()
    {
        if (OnRoundStartEvent != null)
        {
            OnRoundStartEvent.OnEventRaised -= ShowSlilder;
        }
        if (OnTimeChangeEvent != null)
        {
            OnTimeChangeEvent.OnEventRaised -= UpdateSlider;
        }
    }
    void ShowSlilder(int _)
    {
        gameObject.SetActive(true);
    }
    void HideSlider(int _)
    {
        gameObject.SetActive(false);
    }
   
}
