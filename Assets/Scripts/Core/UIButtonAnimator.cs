using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace SobGameJam.UI
{
    public class UIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Animation Settings")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float clickScale = 0.9f;
        [SerializeField] private float animationDuration = 0.2f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip hoverOutSound;

        private Vector3 originalScale;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverSound != null)
            {
                audioSource.PlayOneShot(hoverSound);
            }
            transform.DOKill();
            transform.DOScale(originalScale * hoverScale, animationDuration).SetEase(Ease.OutQuad);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hoverOutSound != null)
            {
                audioSource.PlayOneShot(hoverOutSound);
            }
            transform.DOKill();
            transform.DOScale(originalScale, animationDuration).SetEase(Ease.OutQuad);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(originalScale * clickScale, 0.1f).SetEase(Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(originalScale * hoverScale, 0.1f).SetEase(Ease.OutBack);
        }
    }
}