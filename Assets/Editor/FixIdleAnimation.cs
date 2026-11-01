using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Kept under the original name because existing menu entries call this class.
public static class FixIdleAnimation
{
    [MenuItem("Playground/Fix Character Animation (修正待機、跑步與跳躍)")]
    public static void Run()
    {
        const string characterPath = "Assets/Character/Meshy_Character/";
        AnimationClip idle = LoadOrCreate(characterPath + "Idle.anim", "Idle");
        AnimationClip jump = LoadOrCreate(characterPath + "Jump.anim", "Jump");
        AnimationClip run = LoadOrCreate(characterPath + "NaturalRun.anim", "NaturalRun");

        BuildIdle(idle);
        BuildJump(jump);
        BuildRun(run);
        AssignControllerClips(idle, run, jump);
        ConfigureAnimatorTransitions();
        MeasurePlayerGrounding();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Playground] 已套用直立待機、自然跑步及空中跳躍動畫，並已優化狀態機過渡。");
    }

    private static AnimationClip LoadOrCreate(string path, string clipName)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.name = clipName;
        clip.frameRate = 30;
        return clip;
    }

    private static void BuildIdle(AnimationClip clip)
    {
        const float duration = 2.0f;
        clip.wrapMode = WrapMode.Loop;

        // Humanoid Root Transform: 臀部高度設為 1.014m，使雙腳剛好穩穩平貼地面 y=0，徹底解決待機沉入地底問題
        SetCurve(clip, "RootT.y", duration, 1.014f, 1.025f, 1.014f);
        SetConstant(clip, "RootT.x", duration, 0.0f);
        SetConstant(clip, "RootT.z", duration, 0.0f);
        SetConstant(clip, "RootQ.x", duration, 0.0f);
        SetConstant(clip, "RootQ.y", duration, 0.0f);
        SetConstant(clip, "RootQ.z", duration, 0.0f);
        SetConstant(clip, "RootQ.w", duration, 1.0f);

        // 1. 下半身：直立站姿，膝蓋伸直不彎曲（徹底解決跪姿問題）
        SetConstant(clip, "Left Upper Leg Front-Back", duration, 0.0f);
        SetConstant(clip, "Right Upper Leg Front-Back", duration, 0.0f);
        SetConstant(clip, "Left Upper Leg In-Out", duration, 0.0f);
        SetConstant(clip, "Right Upper Leg In-Out", duration, 0.0f);
        SetConstant(clip, "Left Lower Leg Stretch", duration, 0.0f);
        SetConstant(clip, "Right Lower Leg Stretch", duration, 0.0f);
        SetConstant(clip, "Left Foot Up-Down", duration, 0.0f);
        SetConstant(clip, "Right Foot Up-Down", duration, 0.0f);

        // 2. 上半身：雙手自然垂放於身體大腿兩側（徹底解決雙手擺在胸前問題）
        // 上臂自然下垂
        SetCurve(clip, "Left Arm Down-Up", duration, -0.74f, -0.72f, -0.74f);
        SetCurve(clip, "Right Arm Down-Up", duration, -0.74f, -0.72f, -0.74f);
        // 上臂稍微偏後，對齊身體側面褲縫，不前凸
        SetConstant(clip, "Left Arm Front-Back", duration, -0.06f);
        SetConstant(clip, "Right Arm Front-Back", duration, -0.06f);
        // 略微外展以防穿透大腿與骨盆
        SetConstant(clip, "Left Arm In-Out", duration, 0.06f);
        SetConstant(clip, "Right Arm In-Out", duration, 0.06f);
        // 前臂伸直放鬆（關鍵：0 為手臂放鬆伸直，不曲臂擺在胸前）
        SetConstant(clip, "Left Forearm Stretch", duration, 0.0f);
        SetConstant(clip, "Right Forearm Stretch", duration, 0.0f);

        // 3. 脊椎微幅呼吸起伏，讓角色待機具備生動感
        SetCurve(clip, "Spine Front-Back", duration, 0.0f, 0.02f, 0.0f);
        SetCurve(clip, "Chest Front-Back", duration, 0.0f, 0.015f, 0.0f);

        SetLoop(clip, true);
    }

    private static void BuildJump(AnimationClip clip)
    {
        const float duration = 0.7f;
        clip.wrapMode = WrapMode.ClampForever;

        // Humanoid Root Transform for Jump (起跳與滯空維持正常骨架高度)
        SetCurve(clip, "RootT.y", duration, 1.014f, 1.06f, 1.09f, 1.05f, 1.014f);
        SetConstant(clip, "RootT.x", duration, 0.0f);
        SetConstant(clip, "RootT.z", duration, 0.0f);
        SetConstant(clip, "RootQ.x", duration, 0.0f);
        SetConstant(clip, "RootQ.y", duration, 0.0f);
        SetConstant(clip, "RootQ.z", duration, 0.0f);
        SetConstant(clip, "RootQ.w", duration, 1.0f);

        // 完整的空中跳躍動作：起跳揮臂 -> 空中屈膝抬腿維持平衡 -> 下降準備著地
        // 雙臂起跳強力向上揮動，隨後在空中張開保持平衡，降落前收回
        SetCurve(clip, "Left Arm Down-Up", duration, -0.72f, 0.10f, -0.15f, -0.50f, -0.70f);
        SetCurve(clip, "Right Arm Down-Up", duration, -0.72f, 0.10f, -0.15f, -0.50f, -0.70f);
        SetCurve(clip, "Left Arm Front-Back", duration, -0.05f, 0.25f, 0.15f, 0.05f, -0.05f);
        SetCurve(clip, "Right Arm Front-Back", duration, -0.05f, 0.25f, 0.15f, 0.05f, -0.05f);
        SetCurve(clip, "Left Arm In-Out", duration, 0.06f, 0.20f, 0.25f, 0.15f, 0.06f);
        SetCurve(clip, "Right Arm In-Out", duration, 0.06f, 0.20f, 0.25f, 0.15f, 0.06f);
        SetCurve(clip, "Left Forearm Stretch", duration, 0.0f, -0.25f, -0.35f, -0.15f, 0.0f);
        SetCurve(clip, "Right Forearm Stretch", duration, 0.0f, -0.25f, -0.35f, -0.15f, 0.0f);

        // 下肢：起跳蹬地 -> 空中屈膝收腿 -> 下落伸展準備接觸地面
        SetCurve(clip, "Left Lower Leg Stretch", duration, 0.0f, -0.20f, -0.38f, -0.15f, 0.0f);
        SetCurve(clip, "Right Lower Leg Stretch", duration, 0.0f, -0.20f, -0.38f, -0.15f, 0.0f);
        SetCurve(clip, "Left Upper Leg Front-Back", duration, 0.0f, 0.15f, 0.12f, 0.05f, 0.0f);
        SetCurve(clip, "Right Upper Leg Front-Back", duration, 0.0f, 0.15f, 0.12f, 0.05f, 0.0f);
        SetCurve(clip, "Left Foot Up-Down", duration, 0.0f, -0.25f, -0.35f, -0.10f, 0.0f);
        SetCurve(clip, "Right Foot Up-Down", duration, 0.0f, -0.25f, -0.35f, -0.10f, 0.0f);

        // 軀幹展胸
        SetCurve(clip, "Spine Front-Back", duration, 0.0f, 0.05f, 0.08f, 0.03f, 0.0f);

        SetLoop(clip, false);
    }

    private static void BuildRun(AnimationClip clip)
    {
        const float duration = 0.8f;
        clip.wrapMode = WrapMode.Loop;

        // Humanoid Root Transform for Run
        SetCurve(clip, "RootT.y", duration, 1.014f, 0.98f, 1.014f, 0.98f, 1.014f);
        SetConstant(clip, "RootT.x", duration, 0.0f);
        SetConstant(clip, "RootT.z", duration, 0.0f);
        SetConstant(clip, "RootQ.x", duration, 0.0f);
        SetConstant(clip, "RootQ.y", duration, 0.0f);
        SetConstant(clip, "RootQ.z", duration, 0.0f);
        SetConstant(clip, "RootQ.w", duration, 1.0f);

        SetCurve(clip, "Left Upper Leg Front-Back", duration, .30f, -.30f, .30f, -.30f, .30f);
        SetCurve(clip, "Right Upper Leg Front-Back", duration, -.30f, .30f, -.30f, .30f, -.30f);
        SetCurve(clip, "Left Lower Leg Stretch", duration, -.04f, -.22f, -.04f, -.22f, -.04f);
        SetCurve(clip, "Right Lower Leg Stretch", duration, -.22f, -.04f, -.22f, -.04f, -.22f);
        SetCurve(clip, "Left Arm Down-Up", duration, -.62f, -.56f, -.62f, -.68f, -.62f);
        SetCurve(clip, "Right Arm Down-Up", duration, -.62f, -.68f, -.62f, -.56f, -.62f);
        SetCurve(clip, "Left Arm Front-Back", duration, -.20f, .20f, -.20f, .20f, -.20f);
        SetCurve(clip, "Right Arm Front-Back", duration, .20f, -.20f, .20f, -.20f, .20f);
        SetCurve(clip, "Left Forearm Stretch", duration, -.45f, -.55f, -.45f, -.55f, -.45f);
        SetCurve(clip, "Right Forearm Stretch", duration, -.55f, -.45f, -.55f, -.45f, -.55f);
        SetCurve(clip, "Spine Left-Right", duration, .02f, -.02f, .02f, -.02f, .02f);
        SetLoop(clip, true);
    }

    private static void AssignControllerClips(AnimationClip idle, AnimationClip run, AnimationClip jump)
    {
        string animFbx = "Assets/Character/Meshy_AI_Casual_Studio_Stance_biped/Meshy_AI_Casual_Studio_Stance_biped_Meshy_Merged_Animations.fbx";
        AnimationClip walkClip = null;
        AnimationClip runClip = null;

        if (System.IO.File.Exists(animFbx))
        {
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(animFbx);
            foreach (var obj in allAssets)
            {
                if (obj is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    string lower = c.name.ToLower();
                    if (lower.Contains("run") && runClip == null) runClip = c;
                    else if (lower.Contains("walk") && walkClip == null) walkClip = c;
                }
            }
        }

        if (runClip == null) runClip = run;
        if (walkClip == null) walkClip = runClip;

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Character/Meshy_Character/Meshy_Animator.controller");
        if (controller == null) return;
        foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
        {
            if (state.state.name == "Idle") state.state.motion = idle;
            else if (state.state.name == "Walk")
            {
                state.state.motion = walkClip;
                state.state.speedParameterActive = true;
                state.state.speedParameter = "MotionSpeed";
            }
            else if (state.state.name == "Run")
            {
                state.state.motion = runClip;
                state.state.speedParameterActive = true;
                state.state.speedParameter = "MotionSpeed";
            }
            else if (state.state.name == "Jump") state.state.motion = jump;
        }
        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureAnimatorTransitions()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Character/Meshy_Character/Meshy_Animator.controller");
        if (controller == null) return;
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // 確保 AnyState -> Jump 不會自循環（避免跳躍在空中每幀被重置回第0幀）
        foreach (var tr in sm.anyStateTransitions)
        {
            if (tr.destinationState != null && tr.destinationState.name == "Jump")
            {
                tr.canTransitionToSelf = false;
                tr.hasExitTime = false;
                tr.duration = 0.1f;
            }
        }

        // 尋找 Jump 狀態並完善落地過渡（落地時根據移動速度平滑轉入 Walk 或 Idle）
        AnimatorState jumpState = null;
        AnimatorState idleState = null;
        AnimatorState walkState = null;
        foreach (var cs in sm.states)
        {
            if (cs.state.name == "Jump") jumpState = cs.state;
            else if (cs.state.name == "Idle") idleState = cs.state;
            else if (cs.state.name == "Walk") walkState = cs.state;
        }

        if (jumpState != null && idleState != null && walkState != null)
        {
            // 清理既有過渡重構
            jumpState.transitions = new AnimatorStateTransition[0];

            // 落地且靜止 -> Idle
            var toIdle = jumpState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.12f;
            toIdle.canTransitionToSelf = false;
            toIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            // 落地且奔跑/走動 -> Walk
            var toWalk = jumpState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.12f;
            toWalk.canTransitionToSelf = false;
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
        }

        EditorUtility.SetDirty(controller);
    }

    private static void MeasurePlayerGrounding()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) return;

        SkinnedMeshRenderer smr = player.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null)
        {
            Bounds b = smr.bounds;
            Debug.Log($"[Playground] Player Mesh Bounds: center={b.center}, min={b.min}, max={b.max}, lowestY={b.min.y}");
        }
    }

    private static void SetConstant(AnimationClip clip, string muscle, float duration, float value) =>
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), AnimationCurve.Constant(0f, duration, value));

    private static void SetCurve(AnimationClip clip, string muscle, float duration, params float[] values)
    {
        Keyframe[] keys = new Keyframe[values.Length];
        for (int i = 0; i < values.Length; i++)
            keys[i] = new Keyframe(duration * i / (values.Length - 1), values[i]);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), new AnimationCurve(keys));
    }

    private static void SetLoop(AnimationClip clip, bool loop)
    {
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.keepOriginalPositionY = true;
        settings.keepOriginalPositionXZ = true;
        settings.keepOriginalOrientation = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }
}

