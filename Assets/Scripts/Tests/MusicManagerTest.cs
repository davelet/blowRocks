using UnityEngine;

/// <summary>
/// 测试 MusicManager 是否正确工作的简单脚本
/// 挂载到场景中的任意对象上，运行后查看 Console 输出
/// </summary>
public class MusicManagerTest : MonoBehaviour
{
    private void Update()
    {
        // 按 M 键测试音乐切换
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("[Test] 按下 M 键 - 切换到游戏音乐");
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlayMusic(MusicManager.GameState.Playing);
            }
            else
            {
                Debug.LogError("[Test] MusicManager.Instance 为 null!");
            }
        }

        // 按 B 键切换到暂停音乐
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("[Test] 按下 B 键 - 切换到暂停音乐");
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlayMusic(MusicManager.GameState.Paused);
            }
        }

        // 按 V 键切换到游戏结束音乐
        if (Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("[Test] 按下 V 键 - 切换到游戏结束音乐");
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlayMusic(MusicManager.GameState.GameOver);
            }
        }

        // 按 Escape 键退出
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}