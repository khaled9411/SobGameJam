using UnityEngine;

public class OpenURLButton : MonoBehaviour
{
    [Tooltip("Paste the link you want this button to open here")]
    [SerializeField] private string url;

    // Call this from the Button's OnClick() event in the Inspector
    public void OpenLink()
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
        else
        {
            Debug.LogWarning("No URL set on " + gameObject.name);
        }
    }
}