using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class LooseEffect : MonoBehaviour
{
    private Image _image;
    [SerializeField]private float _targetAlpha = .4f;
    

    private void Awake()
    {
        _image = GetComponent<Image>();
        
        
        // Ensure it starts fully transparent
        Color c = _image.color;
        c.a = 0;
        _image.color = c;
    }

    /// <summary>
    /// Plays the red loose effect by fading in and then fading out.
    /// </summary>
    /// <param name="duration">Total duration of the fade in and fade out effect.</param>
    public void PlayLoseEffect(float duration = 1.0f)
    {
        // Kill any ongoing tweens on this image to prevent overlapping effects
        _image.DOKill();
        
        // Reset to transparent before starting
        Color startColor = _image.color;
        startColor.a = 0;
        _image.color = startColor;

        // Create a sequence to fade in to target alpha, then back to 0
        float peakAlpha = _targetAlpha > 0 ? _targetAlpha : 1f;
        Sequence seq = DOTween.Sequence();
        seq.Append(_image.DOFade(peakAlpha, duration / 2f).SetEase(Ease.OutQuad));
        seq.Append(_image.DOFade(0f, duration / 2f).SetEase(Ease.InQuad));
    }
}
