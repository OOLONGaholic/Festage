using UnityEngine;
using UnityEngine.UI;

public class FeedbackManager : MonoBehaviour
{
    public Image feedbackImage; // 引用UI Image组件
    public Sprite greatSprite; // great图像
    public Sprite missSprite;  // miss图像
    public Sprite comboSprite; // combo图像

    // 方法根据分数或动作选择合适的反馈
    public void ShowFeedback(string feedbackType)
    {
        // 根据反馈类型选择对应的图片
        switch (feedbackType)
        {
            case "great":
                feedbackImage.sprite = greatSprite;
                Debug.LogWarning("goood");
                break;
            case "miss":
                feedbackImage.sprite = missSprite;
                break;
            case "combo":
                feedbackImage.sprite = comboSprite;
                break;
            default:
                feedbackImage.sprite = null; // 清空显示
                break;
        }

        // 显示图片，并在短暂时间后隐藏
        feedbackImage.enabled = true;
        Invoke("HideFeedback", 1f); // 1秒后隐藏
    }

    // 隐藏反馈图片
    private void HideFeedback()
    {
        feedbackImage.enabled = false;
    }
}
