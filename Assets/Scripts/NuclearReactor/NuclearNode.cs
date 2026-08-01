using UnityEngine;
using TMPro;
using DG.Tweening;

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
    private Tween shrinkTween;
    private bool isHandled = false;

    public void Setup(KeyCode key, float duration)
    {
        targetKey = key;
        letterText.text = key.ToString();

        outerCircle.localScale = innerCircle.localScale * biggeningScale;

        shrinkTween = outerCircle.DOScale(Vector3.zero, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
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
        float distanceToCriticalPoint = Mathf.Abs(currentScale - 1f);

        if (distanceToCriticalPoint <= perfectHitRange)
        {
            SuccessNode(true);
        }
        else if (distanceToCriticalPoint <= validHitRange)
        {
            SuccessNode(false);
        }
        else
        {
            FailNode();
        }
    }

    private void SuccessNode(bool isPerfect)
    {
        isHandled = true;
        shrinkTween?.Kill();
        NuclearGameManager.Instance.RemoveNode(this);

        if (isPerfect)
        {
            innerCircle.DOPunchScale(Vector3.one * 0.5f, 0.3f, 10, 1f);
            innerSprite.DOColor(Color.green, 0.3f);
        }
        else
        {
            innerSprite.DOColor(Color.yellow, 0.3f);
        }

        letterText.DOFade(0, 0.3f);
        innerSprite.DOFade(0, 0.3f).OnComplete(() => Destroy(gameObject));
        outerCircle.GetComponent<SpriteRenderer>().DOFade(0, 0.3f);
    }

    private void FailNode()
    {
        isHandled = true;
        shrinkTween?.Kill();
        NuclearGameManager.Instance.RemoveNode(this);

        innerSprite.DOColor(Color.red, 0.3f);
        transform.DOShakePosition(0.4f, 0.5f);

        NuclearGameManager.Instance.LoseGame();
    }
}