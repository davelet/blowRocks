using UnityEngine;

[DisallowMultipleComponent]
public class BootLoader : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // 创建背景根对象，不随场景销毁
        var root = new GameObject("BootBackground");
        DontDestroyOnLoad(root);

        // 创建全屏画布
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // 最顶层显示

        // 创建背景图片
        var background = new GameObject("Background");
        background.transform.SetParent(root.transform, false);
        
        var image = background.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0f, 0f, 0f, 1f); // 纯黑背景，和启动界面保持一致
        
        var rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        // 主场景加载完成后延迟一帧销毁，保证第一帧已经渲染完成
        root.AddComponent<BootLoader>();
    }

    private void Start()
    {
        // 延迟到下一帧销毁，确保主场景第一帧已经渲染出来
        Destroy(gameObject, 0.1f);
    }
}
