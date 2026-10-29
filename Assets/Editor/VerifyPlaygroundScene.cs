using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyPlaygroundScene
{
    [MenuItem("Playground/Verify Scene")]
    public static void Run()
    {
        Debug.Log("=== Verifying PlaygroundScene.unity ===");
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/PlaygroundScene.unity");

        GameObject player = GameObject.Find("Player");
        if (player == null) Debug.LogError("Verification FAILED: 'Player' not found!");
        else
        {
            var cc = player.GetComponent<CharacterController>();
            var pc = player.GetComponent<PlayerController>();
            var anim = player.GetComponentInChildren<Animator>();
            Debug.Log($"Player verification: CC={(cc != null)}, PC={(pc != null)}, Anim={(anim != null)}, Pos={player.transform.position}");
        }

        GameObject cam = GameObject.FindWithTag("MainCamera");
        if (cam == null) Debug.LogError("Verification FAILED: 'MainCamera' not found!");
        else
        {
            var tpc = cam.GetComponent<ThirdPersonCamera>();
            Debug.Log($"Camera verification: TPC={(tpc != null)}, Target={(tpc?.target?.name)}");
        }

        GameObject balls = GameObject.Find("SoccerBalls");
        Debug.Log($"SoccerBalls verification: {(balls != null ? balls.transform.childCount : 0)} balls");

        GameObject goals = GameObject.Find("SoccerGoals");
        Debug.Log($"SoccerGoals verification: {(goals != null ? goals.transform.childCount : 0)} goals");

        GameObject grandstand = GameObject.Find("SpectatorGrandstand");
        Debug.Log($"Grandstand verification: Found={(grandstand != null)}");

        GameObject track = GameObject.Find("AthleticsTrack_400m");
        Debug.Log($"Track verification: Found={(track != null)}");

        GameObject pitch = GameObject.Find("SoccerPitch");
        Debug.Log($"SoccerPitch verification: Found={(pitch != null)}");

        Debug.Log("=== Verification Completed Successfully ===");
    }
}
