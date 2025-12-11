using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GestureUIFeedback : MonoBehaviour
{
    public Image gestureImage;        // 拖进 Inspector
    public float showTime = 0.5f;     // 显示多久
    private Coroutine currentRoutine;

    public void ShowOnce(Sprite sprite)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        gestureImage.sprite = sprite;
        currentRoutine = StartCoroutine(ShowCoroutine());
    }

    private IEnumerator ShowCoroutine()
    {
        gestureImage.color = new Color(1, 1, 1, 1); // 显示
        yield return new WaitForSeconds(showTime);
        gestureImage.color = new Color(1, 1, 1, 0); // 隐藏
    }
}
