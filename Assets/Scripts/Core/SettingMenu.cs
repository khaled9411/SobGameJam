using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UI;
using DG.Tweening;

public class SettingMenu : MonoBehaviour
{

    [SerializeField] private Slider volumeSlider;
    [SerializeField] private AudioMixer audioMixer;
    [Header("Animation Settings")]
    [SerializeField] private RectTransform background;
    [SerializeField] private float hiddenYPos = 400f;
    [SerializeField] private float shownYPos = 0f;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private bool isshown = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        volumeSlider.onValueChanged.AddListener(UpdateVolume);
        float savedVolume = PlayerPrefs.GetFloat("masterVolume", 0);
        volumeSlider.value = savedVolume;
        audioMixer.SetFloat("masterVolume", savedVolume);
    }
    private void UpdateVolume(float volume)
    {
        audioMixer.SetFloat("masterVolume", volume);
        PlayerPrefs.SetFloat("masterVolume", volume);
    }
    public void show()
    {
        gameObject.SetActive(true);
        if (background != null)
        {
            background.DOAnchorPosY(shownYPos, animationDuration).SetEase(Ease.OutBack);
        }
    }
    public void hide()
    {
        if (background != null)
        {
            background.DOAnchorPosY(hiddenYPos, animationDuration).SetEase(Ease.InBack).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void press()
    {
        if (isshown)
        {
            hide();
            
        }
        else
        {
            show();
        }
        isshown = !isshown;
    }
}
