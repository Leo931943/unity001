using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AutoOpenScene
{
    static AutoOpenScene()
    {
        EditorApplication.delayCall += RunAutoSetup;
    }

    [MenuItem("Playground/1. Open Playground Scene (開啟操場場景)", false, 1)]
    public static void OpenPlaygroundScene()
    {
        string scenePath = "Assets/Scenes/PlaygroundScene.unity";
        if (System.IO.File.Exists(scenePath))
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            FixIdleAnimation.Run();
            Debug.Log("<color=green><b>[Playground]</b> 已成功開啟操場與綠草地足球場場景！按下 Unity 頂部的 Play (▶) 即可開始遊玩。</color>");
        }
        else
        {
            Debug.LogError("PlaygroundScene.unity 不存在！請點選 Playground > 2. Rebuild Scene 重新建立。");
        }
    }

    [MenuItem("Playground/2. Fix Idle Pose & Hands (修正靜止沉地與手部位置)", false, 2)]
    public static void TriggerFixIdle()
    {
        FixIdleAnimation.Run();
    }

    private static void RunAutoSetup()
    {
        EnsurePlaygroundSceneLoaded();
        FixIdleAnimation.Run();
    }

    private static void EnsurePlaygroundSceneLoaded()
    {
        Scene current = SceneManager.GetActiveScene();
        if (current.name != "PlaygroundScene" || current.rootCount < 3)
        {
            string scenePath = "Assets/Scenes/PlaygroundScene.unity";
            if (System.IO.File.Exists(scenePath))
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Debug.Log("<color=cyan><b>[Playground 自動引導]</b> 已自動為您載入「PlaygroundScene」操場足球場！</color>");
            }
        }
    }
}
