using UnityEngine;
using TMPro;
using DG.Tweening;
using SobGameJam.MiniGames;

public class NuclearNode : MonoBehaviour
{
    [Header("2D Elements")]
    public Transform innerCircle;
    public Transform outerCircle;
    public TextMeshProUGUI letterText;
    public SpriteRenderer innerSprite;

    [Header("Hit Windows")]
    public float perfectHitRange = 0.15f;
    public float validHitRange = 0.35f;
    public float biggeningScale = 3f;

    [HideInInspector] public KeyCode targetKey;

    // Stores which handle this node will animate (0, 1, or 2)
    [HideInInspector] public int assignedHandleIndex;

    private Tween shrinkTween;
    private bool isHandled = false;

    public void Setup(KeyCode key, float duration)
    {
        targetKey = key;
        letterText.text = key.ToString();

        outerCircle.localScale = innerCircle.localScale * biggeningScale;

        // Animate the outer circle shrinking towards 0
        shrinkTween = outerCircle.DOScale(Vector3.zero, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                // If the circle completely vanishes before the player presses the key, it's a timeout fail
                if (!isHandled)
                {
                    FailNode();
                }
            });
    }

    public void CheckHitTiming()
    {
        if (isHandled) return;

        float currentScale = outerCircle.localScale.x;

        // 1f is the critical overlap point (when outer circle perfectly matches inner circle)
        float distanceToCriticalPoint = Mathf.Abs(currentScale - 1f);

        if (distanceToCriticalPoint <= validHitRange)
        {
            // Calculate accuracy from 0.0 to 1.0 using InverseLerp
            // 0 = barely hit the edge of valid range
            // 1 = absolute perfection (distance == 0)
            float accuracy = Mathf.InverseLerp(validHitRange, 0f, distanceToCriticalPoint);

            bool isPerfect = distanceToCriticalPoint <= perfectHitRange;

            SuccessNode(accuracy, isPerfect);
        }
        else
        {
            // Pressed the right key, but at the wrong time (too early or too late)
            FailNode();
        }
    }

    private void SuccessNode(float accuracy, bool isPerfect)
    {
        isHandled = true;
        shrinkTween?.Kill(); // Stop the shrinking animation

        // 1. Tell the manager to animate our specific handle based on how accurate this hit was
        NuclearGameManager.Instance.AnimateHandle(assignedHandleIndex, accuracy);

        // 2. Remove from active play
        NuclearGameManager.Instance.RemoveNode(this);

        // 3. Play local node visual effects
        Sequence seq = DOTween.Sequence();

        if (isPerfect)
        {
            seq.Join(innerCircle.DOPunchScale(Vector3.one * 0.5f, 0.3f, 10, 1f));
            seq.Join(innerSprite.DOColor(Color.green, 0.3f));
        }
        else
        {
            seq.Join(innerSprite.DOColor(Color.yellow, 0.3f));
        }

        // Fade everything out smoothly
        seq.Join(letterText.DOFade(0, 0.3f));
        seq.Join(innerSprite.DOFade(0, 0.3f));
        seq.Join(outerCircle.GetComponent<SpriteRenderer>().DOFade(0, 0.3f));

        // Destroy only after visual feedback completes
        seq.OnComplete(() => Destroy(gameObject));
    }

    private void FailNode()
    {
        isHandled = true;
        shrinkTween?.Kill();

        // 1. Trigger the cinematic WarioWare catastrophic failure in the GameManager
        NuclearGameManager.Instance.PlayWrongKeyExplosion();

        // 2. Show local failure (red flash and harsh vibration)
        // We do NOT destroy the node immediately so the player can see WHICH node they failed on
        // while the camera shake and explosion play out.
        innerSprite.DOColor(Color.red, 0.15f);
        transform.DOShakePosition(0.5f, 0.5f, 30, 90, false, true);
    }

    private void OnDestroy()
    {
        // Safety check to ensure we don't leak tweens if the game resets abruptly
        shrinkTween?.Kill();
        transform.DOKill();
        innerCircle.DOKill();
        innerSprite.DOKill();
    }
}