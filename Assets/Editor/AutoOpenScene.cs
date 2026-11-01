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

    [MenuItem("Playground/2. Fix Character Animation (修正待機、跑步與跳躍)", false, 2)]
    public static void TriggerFixIdle()
    {
        FixIdleAnimation.Run();
    }

    [MenuItem("Playground/3. Setup New Casual Character (安裝新角色與動畫)", false, 3)]
    public static void TriggerSetupNewCharacter()
    {
        SetupNewCharacter.Run();
    }

    private static void RunAutoSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsurePlaygroundSceneLoaded();
        FixIdleAnimation.Run();
        SetupNewCharacter.Run();
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
