using System.IO;
using UnityEditor;
using UnityEngine;

public static class FixIdleAnimation
{
    [MenuItem("Playground/Fix Idle Animation (修正靜止姿勢與手部位置)")]
    public static void Run()
    {
        Debug.Log(">>> [FixIdleAnimation] Starting fix for Idle pose...");

        string fbxPath = "Assets/Character/Meshy_Character/Meshy_AI_Neutral_Studio_Stance_biped_Animation_Running_withSkin.fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (prefab == null)
        {
            Debug.LogError("Failed to load FBX prefab: " + fbxPath);
            return;
        }

        GameObject inst = Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;

        Animator anim = inst.GetComponent<Animator>();
        if (anim == null || anim.avatar == null)
        {
            Debug.LogError("Model has no animator or avatar!");
            Object.DestroyImmediate(inst);
            return;
        }

        HumanPoseHandler handler = new HumanPoseHandler(anim.avatar, inst.transform);
        HumanPose pose = new HumanPose();
        handler.GetHumanPose(ref pose);

        Debug.Log($"Bind Pose BodyPosition: {pose.bodyPosition}, BodyRotation: {pose.bodyRotation}");

        // Create new Idle AnimationClip
        AnimationClip idleClip = new AnimationClip();
        idleClip.name = "Idle";
        idleClip.frameRate = 30;
        idleClip.wrapMode = WrapMode.Loop;

        float duration = 2.0f;

        // 1. Root Position (Prevent sinking into ground!)
        // Ensure RootT.y maintains natural hip height so character does not sink
        float hipHeight = Mathf.Max(0.9f, pose.bodyPosition.y);
        Debug.Log($"Setting Idle RootT.y to {hipHeight}");

        Keyframe[] rootYKeys = new Keyframe[]
        {
            new Keyframe(0f, hipHeight),
            new Keyframe(duration * 0.5f, hipHeight + 0.015f), // Subtle breathing lift
            new Keyframe(duration, hipHeight)
        };
        AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.y"), new AnimationCurve(rootYKeys));
        AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x"), AnimationCurve.Constant(0f, duration, 0f));
        AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.z"), AnimationCurve.Constant(0f, duration, 0f));

        // Root Rotation (Identity)
        AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x"), AnimationCurve.Constant(0f, duration, 0f));
        AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.y"), AnimationCurve.Constant(0f, duration, 0f));
        AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.z"), AnimationCurve.Constant(0f, duration, 0f));
        AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.w"), AnimationCurve.Constant(0f, duration, 1f));

        // 2. Adjust Muscles for Natural Idle:
        // Hands resting at sides of thighs (手放在大腿兩側), not in front clasped together!
        for (int i = 0; i < HumanTrait.MuscleCount; i++)
        {
            string muscle = HumanTrait.MuscleName[i];
            float baseVal = (i < pose.muscles.Length) ? pose.muscles[i] : 0f;

            // Arms: Down and slightly back/out so they fall naturally at the sides of the thighs
            if (muscle == "Left Arm Down-Up" || muscle == "Right Arm Down-Up")
            {
                // -0.75 drops arm straight down to the side
                baseVal = -0.72f;
            }
            else if (muscle == "Left Arm Front-Back" || muscle == "Right Arm Front-Back")
            {
                // Pull arm back slightly so it does NOT point forward/hands clasp in front!
                baseVal = -0.22f;
            }
            else if (muscle == "Left Arm In-Out" || muscle == "Right Arm In-Out")
            {
                // Keep arm slightly away from torso so hands rest right against the outside of thighs
                baseVal = 0.08f;
            }
            else if (muscle == "Left Forearm Stretch" || muscle == "Right Forearm Stretch")
            {
                // Relax elbow straight down (not bent forward towards the belly)
                baseVal = -0.05f;
            }
            else if (muscle == "Left Forearm In-Out" || muscle == "Right Forearm In-Out")
            {
                baseVal = 0f;
            }
            else if (muscle == "Left Hand Down-Up" || muscle == "Right Hand Down-Up")
            {
                baseVal = 0f;
            }
            else if (muscle == "Left Hand In-Out" || muscle == "Right Hand In-Out")
            {
                baseVal = 0f;
            }
            else if (muscle == "Left Hand Twist" || muscle == "Right Hand Twist")
            {
                // Neutral palm facing the thigh
                baseVal = 0f;
            }
            // Spine breathing
            else if (muscle == "Spine Front-Back")
            {
                Keyframe[] spineKeys = new Keyframe[]
                {
                    new Keyframe(0f, 0.02f),
                    new Keyframe(duration * 0.5f, 0.05f),
                    new Keyframe(duration, 0.02f)
                };
                AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), new AnimationCurve(spineKeys));
                continue;
            }

            // Create constant or slight breathing curve
            AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), AnimationCurve.Constant(0f, duration, baseVal));
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(idleClip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(idleClip, settings);

        string idlePath = "Assets/Character/Meshy_Character/Idle.anim";
        AssetDatabase.CreateAsset(idleClip, idlePath);
        Debug.Log($"Saved new Idle clip to {idlePath}, isHumanMotion: {idleClip.isHumanMotion}");

        Object.DestroyImmediate(inst);

        // Also update scenes with new Idle clip
        UpdateScenesAndAnimator();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green><b>[FixIdleAnimation] 靜止姿勢與大腿兩側手部位置修正完成！</b></color>");
    }

    private static void UpdateScenesAndAnimator()
    {
        // Re-assign Idle clip in Meshy_Animator.controller
        string animPath = "Assets/Character/Meshy_Character/Meshy_Animator.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(animPath);
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character/Meshy_Character/Idle.anim");

        if (controller != null && idleClip != null)
        {
            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.name == "Idle")
                    {
                        state.state.motion = idleClip;
                        Debug.Log("Updated Idle state motion in AnimatorController.");
                    }
                }
            }
            EditorUtility.SetDirty(controller);
        }

        // Also check Player in active scene
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            // Ensure CharacterController center and height
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.height = 1.7f;
                cc.radius = 0.3f;
                cc.center = new Vector3(0, 0.85f, 0);
            }

            Transform model = player.transform.Find("Meshy_Model");
            if (model != null)
            {
                model.localPosition = Vector3.zero;
                model.localRotation = Quaternion.identity;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        }
    }
}
