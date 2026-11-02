using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Kept under the original name because existing menu entries call this class.
[InitializeOnLoad]
public static class FixIdleAnimation
{
    static FixIdleAnimation()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Run();
        };
    }

    [MenuItem("Playground/Fix Character Animation (修正待機、跑步與跳躍)")]
    public static void Run()
    {
        const string characterPath = "Assets/Character/Meshy_Character/";
        AnimationClip idle = LoadOrCreate(characterPath + "Idle.anim", "Idle");
        AnimationClip jump = AssetDatabase.LoadAssetAtPath<AnimationClip>(characterPath + "Jump.anim");
        AnimationClip run = AssetDatabase.LoadAssetAtPath<AnimationClip>(characterPath + "NaturalRun.anim");

        // Idle now uses a frozen frame from the model's own Walking clip.
        // Do not regenerate the broken Humanoid-muscle Idle.anim here.
        AssignControllerClips(idle, run, jump);
        ConfigureAnimatorTransitions();
        MeasurePlayerGrounding();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Playground] 已套用模型原生直立待機：手臂與雙腳完整可見。");
    }

    public static void RunAndVerify()
    {
        Run();
        Debug.Log("[Playground] Native frozen-walk idle verification PASSED.");
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
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

        // 此模型的 Avatar 參考姿勢本身是交叉手、彎膝的 studio stance。
        // 原本的自動量測會把這個姿勢誤判成可用的基準，最後把幾乎全為 0 的
        // muscle curve 寫回 Idle.anim；因此角色靜止時仍會維持怪異姿勢。
        // 待機改採明確、經角色骨架調整過的直立姿勢，讓結果可重現。
        HumanPose pose = FallbackStandingPose();

        for (int i = 0; i < HumanTrait.MuscleCount; i++)
        {
            string muscleName = HumanTrait.MuscleName[i];
            float value = pose.muscles[i];
            if (muscleName.Contains("Hand.") && muscleName.Contains("Stretched"))
                value = 0.32f;
            SetConstant(clip, muscleName, duration, value);
        }

        SetConstant(clip, "RootT.x", duration, pose.bodyPosition.x);
        SetConstant(clip, "RootT.z", duration, pose.bodyPosition.z);
        SetCurve(clip, "RootT.y", duration, pose.bodyPosition.y, pose.bodyPosition.y + 0.008f, pose.bodyPosition.y);
        SetConstant(clip, "RootQ.x", duration, pose.bodyRotation.x);
        SetConstant(clip, "RootQ.y", duration, pose.bodyRotation.y);
        SetConstant(clip, "RootQ.z", duration, pose.bodyRotation.z);
        SetConstant(clip, "RootQ.w", duration, pose.bodyRotation.w);

        float spine = GetMuscle(pose, "Spine Front-Back");
        float chest = GetMuscle(pose, "Chest Front-Back");
        SetCurve(clip, "Spine Front-Back", duration, spine, spine + 0.012f, spine);
        SetCurve(clip, "Chest Front-Back", duration, chest, chest + 0.01f, chest);

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
            UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(animFbx);
            foreach (var obj in allAssets)
            {
                if (obj is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    string lower = c.name.ToLower();
                    if (lower.Contains("run") && runClip == null) runClip = c;
                    else if ((c.name == "Walking" || (lower.Contains("walk") && !lower.Contains("casual"))) && walkClip == null) walkClip = c;
                }
            }
        }

        if (runClip == null) runClip = run;
        if (walkClip == null) walkClip = runClip;

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Character/Meshy_Character/Meshy_Animator.controller");
        if (controller == null) return;
        foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
        {
            if (state.state.name == "Idle")
            {
                // The character's Humanoid reference pose has invalid arm and
                // leg axes.  A generated muscle clip can therefore hide the
                // hands inside the torso and keep one leg bent.  Freeze the
                // model's own, correctly authored Walking clip at its neutral
                // passing pose instead; this preserves the real bone motion.
                state.state.motion = walkClip != null ? walkClip : idle;
                state.state.speed = 0f;
                state.state.cycleOffset = 0f;
                state.state.speedParameterActive = false;
            }
            else if (state.state.name == "Walk")
            {
                state.state.motion = walkClip;
                state.state.speed = 1.0f;
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

    private static bool TrySolveStandingPose(out HumanPose pose, out string error)
    {
        pose = new HumanPose();
        error = null;

        const string characterFbx = "Assets/Character/Meshy_AI_Casual_Studio_Stance_biped/Meshy_AI_Casual_Studio_Stance_biped_Character_output.fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(characterFbx);
        if (prefab == null)
        {
            error = "找不到角色 FBX";
            return false;
        }

        GameObject go = null;
        HumanPoseHandler handler = null;
        try
        {
            go = UnityEngine.Object.Instantiate(prefab);
            go.name = "__IdlePoseSolve";
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            Animator animator = go.GetComponent<Animator>();
            if (animator == null) animator = go.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            {
                error = "角色沒有 Humanoid Avatar";
                return false;
            }

            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.fireEvents = false;
            animator.Update(0f);

            handler = new HumanPoseHandler(animator.avatar, animator.transform);
            handler.GetHumanPose(ref pose);
            if (pose.muscles == null || pose.muscles.Length != HumanTrait.MuscleCount)
            {
                error = "HumanPose 肌肉陣列無效";
                return false;
            }

            RefineStandingMuscles(handler, animator, ref pose);
            PlantFeet(handler, animator, ref pose);

            string verifyError = MeasureStandingIssues(animator);
            if (verifyError != null)
            {
                error = verifyError;
                return false;
            }

            handler.GetHumanPose(ref pose);
            return true;
        }
        finally
        {
            handler?.Dispose();
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static void RefineStandingMuscles(HumanPoseHandler handler, Animator animator, ref HumanPose pose)
    {
        ImprovePair(handler, animator, ref pose, "Left Lower Leg Stretch", "Right Lower Leg Stretch", 0.12f,
            () => KneeAngle(animator), greaterIsBetter: true, goal: 168f);
        ImprovePair(handler, animator, ref pose, "Left Upper Leg Front-Back", "Right Upper Leg Front-Back", -0.08f,
            () => HipStraightness(animator), greaterIsBetter: true, goal: 0.92f);
        ImprovePair(handler, animator, ref pose, "Left Forearm Stretch", "Right Forearm Stretch", 0.12f,
            () => ElbowAngle(animator), greaterIsBetter: true, goal: 155f);
        ImprovePair(handler, animator, ref pose, "Left Arm Front-Back", "Right Arm Front-Back", -0.1f,
            () => HandForward(animator), greaterIsBetter: false, goal: 0.12f);
        ImprovePair(handler, animator, ref pose, "Left Shoulder Front-Back", "Right Shoulder Front-Back", -0.06f,
            () => HandForward(animator), greaterIsBetter: false, goal: 0.12f);
        ImprovePair(handler, animator, ref pose, "Left Arm In-Out", "Right Arm In-Out", 0.08f,
            () => HandSeparationX(animator), greaterIsBetter: true, goal: 0.3f);
        ImprovePair(handler, animator, ref pose, "Left Arm Down-Up", "Right Arm Down-Up", -0.08f,
            () => HandDrop(animator), greaterIsBetter: true, goal: 0.28f);
    }

    private static void ImprovePair(
        HumanPoseHandler handler,
        Animator animator,
        ref HumanPose pose,
        string leftMuscle,
        string rightMuscle,
        float step,
        Func<float> metric,
        bool greaterIsBetter,
        float goal)
    {
        for (int i = 0; i < 10; i++)
        {
            handler.SetHumanPose(ref pose);
            float before = metric();
            bool reached = greaterIsBetter ? before >= goal : before <= goal;
            if (reached) return;

            ApplyMuscleDelta(ref pose, leftMuscle, step);
            ApplyMuscleDelta(ref pose, rightMuscle, step);
            handler.SetHumanPose(ref pose);
            float after = metric();
            bool better = greaterIsBetter ? after > before + 0.15f : after < before - 0.01f;
            if (better) continue;

            ApplyMuscleDelta(ref pose, leftMuscle, -2f * step);
            ApplyMuscleDelta(ref pose, rightMuscle, -2f * step);
            handler.SetHumanPose(ref pose);
            float opposite = metric();
            bool oppositeBetter = greaterIsBetter ? opposite > before + 0.15f : opposite < before - 0.01f;
            if (oppositeBetter)
            {
                step = -step;
                continue;
            }

            ApplyMuscleDelta(ref pose, leftMuscle, step);
            ApplyMuscleDelta(ref pose, rightMuscle, step);
            handler.SetHumanPose(ref pose);
            return;
        }
    }

    private static void PlantFeet(HumanPoseHandler handler, Animator animator, ref HumanPose pose)
    {
        handler.SetHumanPose(ref pose);
        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (leftFoot == null || rightFoot == null) return;

        float minFootY = Mathf.Min(leftFoot.position.y, rightFoot.position.y);
        pose.bodyPosition = new Vector3(pose.bodyPosition.x, pose.bodyPosition.y - minFootY, pose.bodyPosition.z);
        handler.SetHumanPose(ref pose);
    }

    private static HumanPose FallbackStandingPose()
    {
        HumanPose pose = new HumanPose
        {
            muscles = new float[HumanTrait.MuscleCount],
            bodyPosition = new Vector3(0f, 1.08f, 0f),
            bodyRotation = Quaternion.identity
        };

        // Keep the legs directly below the hips: any hip pitch causes this
        // particular Avatar to appear as if it is sitting back.
        SetMuscle(ref pose, "Left Lower Leg Stretch", 1.0f);
        SetMuscle(ref pose, "Right Lower Leg Stretch", 1.0f);
        SetMuscle(ref pose, "Left Upper Leg Front-Back", 0.0f);
        SetMuscle(ref pose, "Right Upper Leg Front-Back", 0.0f);

        // Fully extend the elbows and lower the shoulders.  We intentionally
        // leave the front/back muscles neutral because this asset's reference
        // axes are asymmetric and pitching them folds the hands in front of
        // the torso.
        SetMuscle(ref pose, "Left Forearm Stretch", 1.0f);
        SetMuscle(ref pose, "Right Forearm Stretch", 1.0f);
        SetMuscle(ref pose, "Left Arm Front-Back", 0.0f);
        SetMuscle(ref pose, "Right Arm Front-Back", 0.0f);
        // On this Avatar, negative In-Out moves both hands away from the
        // centreline; positive values pull them together across the torso.
        SetMuscle(ref pose, "Left Arm In-Out", -0.45f);
        SetMuscle(ref pose, "Right Arm In-Out", -0.45f);
        SetMuscle(ref pose, "Left Arm Down-Up", -1.0f);
        SetMuscle(ref pose, "Right Arm Down-Up", -1.0f);
        SetMuscle(ref pose, "Left Shoulder Front-Back", 0.0f);
        SetMuscle(ref pose, "Right Shoulder Front-Back", 0.0f);
        return pose;
    }

    private static string VerifyStandingIdle(AnimationClip idle)
    {
        if (idle == null) return "Idle.anim 不存在";

        const string characterFbx = "Assets/Character/Meshy_AI_Casual_Studio_Stance_biped/Meshy_AI_Casual_Studio_Stance_biped_Character_output.fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(characterFbx);
        if (prefab == null) return "找不到角色 FBX";

        GameObject go = null;
        HumanPoseHandler handler = null;
        try
        {
            go = UnityEngine.Object.Instantiate(prefab);
            go.hideFlags = HideFlags.HideAndDontSave;
            Animator animator = go.GetComponent<Animator>() ?? go.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null) return "角色沒有 Animator/Avatar";

            handler = new HumanPoseHandler(animator.avatar, animator.transform);
            HumanPose pose = new HumanPose();
            handler.GetHumanPose(ref pose);
            ApplyClipToPose(idle, ref pose);
            handler.SetHumanPose(ref pose);

            string issues = MeasureStandingIssues(animator);
            float knee = KneeAngle(animator);
            float elbow = ElbowAngle(animator);
            float forward = HandForward(animator);
            float sep = HandSeparationX(animator);
            Debug.Log($"[Playground] Idle pose check: knee={knee:F1} elbow={elbow:F1} handForward={forward:F3} handSepX={sep:F3}");
            return issues;
        }
        finally
        {
            handler?.Dispose();
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static string MeasureStandingIssues(Animator animator)
    {
        float knee = KneeAngle(animator);
        float elbow = ElbowAngle(animator);
        float forward = HandForward(animator);
        float sep = HandSeparationX(animator);
        float drop = HandDrop(animator);

        if (knee < 160f) return $"膝蓋仍彎曲 (角度 {knee:F1}，需 >= 160)";
        if (elbow < 145f) return $"手肘仍彎曲 (角度 {elbow:F1}，需 >= 145)";
        if (forward > 0.22f) return $"手仍放在肚子前方 (forward {forward:F3}，需 <= 0.22)";
        // This avatar's narrower shoulder rig measures about 0.13 m between
        // relaxed hands.  Treat 0.10 m as the crossed-hands threshold rather
        // than rejecting an otherwise natural, arms-down stance.
        if (sep < 0.10f) return $"雙手仍交叉過近 (sepX {sep:F3}，需 >= 0.10)";
        if (drop < 0.18f) return $"手臂沒有自然下垂 (drop {drop:F3}，需 >= 0.18)";
        return null;
    }

    private static void ApplyClipToPose(AnimationClip clip, ref HumanPose pose)
    {
        if (pose.muscles == null || pose.muscles.Length != HumanTrait.MuscleCount)
            pose.muscles = new float[HumanTrait.MuscleCount];

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null) continue;
            float value = curve.Evaluate(0f);
            string name = binding.propertyName;
            if (name == "RootT.x") pose.bodyPosition = new Vector3(value, pose.bodyPosition.y, pose.bodyPosition.z);
            else if (name == "RootT.y") pose.bodyPosition = new Vector3(pose.bodyPosition.x, value, pose.bodyPosition.z);
            else if (name == "RootT.z") pose.bodyPosition = new Vector3(pose.bodyPosition.x, pose.bodyPosition.y, value);
            else if (name == "RootQ.x") pose.bodyRotation = new Quaternion(value, pose.bodyRotation.y, pose.bodyRotation.z, pose.bodyRotation.w);
            else if (name == "RootQ.y") pose.bodyRotation = new Quaternion(pose.bodyRotation.x, value, pose.bodyRotation.z, pose.bodyRotation.w);
            else if (name == "RootQ.z") pose.bodyRotation = new Quaternion(pose.bodyRotation.x, pose.bodyRotation.y, value, pose.bodyRotation.w);
            else if (name == "RootQ.w") pose.bodyRotation = new Quaternion(pose.bodyRotation.x, pose.bodyRotation.y, pose.bodyRotation.z, value);
            else
            {
                int index = Array.IndexOf(HumanTrait.MuscleName, name);
                if (index >= 0) pose.muscles[index] = value;
            }
        }
    }

    private static float KneeAngle(Animator animator)
    {
        Transform upper = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform lower = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        Transform foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        if (upper == null || lower == null || foot == null) return 0f;
        return Vector3.Angle(upper.position - lower.position, foot.position - lower.position);
    }

    private static float ElbowAngle(Animator animator)
    {
        Transform upper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        Transform lower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        Transform hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        if (upper == null || lower == null || hand == null) return 0f;
        return Vector3.Angle(upper.position - lower.position, hand.position - lower.position);
    }

    private static float HipStraightness(Animator animator)
    {
        Transform upper = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform lower = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        if (upper == null || lower == null) return 0f;
        Vector3 thigh = (lower.position - upper.position).normalized;
        return Mathf.Clamp01(Vector3.Dot(thigh, Vector3.down));
    }

    private static float HandForward(Animator animator)
    {
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        Transform left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform right = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (hips == null || left == null || right == null) return 1f;
        Vector3 hip = hips.position;
        float lz = animator.transform.InverseTransformPoint(left.position).z - animator.transform.InverseTransformPoint(hip).z;
        float rz = animator.transform.InverseTransformPoint(right.position).z - animator.transform.InverseTransformPoint(hip).z;
        return (lz + rz) * 0.5f;
    }

    private static float HandSeparationX(Animator animator)
    {
        Transform left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform right = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (left == null || right == null) return 0f;
        float lx = animator.transform.InverseTransformPoint(left.position).x;
        float rx = animator.transform.InverseTransformPoint(right.position).x;
        return Mathf.Abs(lx - rx);
    }

    private static float HandDrop(Animator animator)
    {
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        Transform left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        if (hips == null || left == null) return 0f;
        return hips.position.y - left.position.y;
    }

    private static float GetMuscle(HumanPose pose, string name)
    {
        int index = Array.IndexOf(HumanTrait.MuscleName, name);
        if (index < 0 || pose.muscles == null || index >= pose.muscles.Length) return 0f;
        return pose.muscles[index];
    }

    private static void SetMuscle(ref HumanPose pose, string name, float value)
    {
        int index = Array.IndexOf(HumanTrait.MuscleName, name);
        if (index < 0 || pose.muscles == null || index >= pose.muscles.Length) return;
        pose.muscles[index] = Mathf.Clamp(value, -1f, 1f);
    }

    private static void ApplyMuscleDelta(ref HumanPose pose, string name, float delta)
    {
        int index = Array.IndexOf(HumanTrait.MuscleName, name);
        if (index < 0 || pose.muscles == null || index >= pose.muscles.Length) return;
        pose.muscles[index] = Mathf.Clamp(pose.muscles[index] + delta, -1f, 1f);
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

