using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class PlaygroundBuilder
{
    private static Material matCharacter;
    private static Material matGrassTurf;
    private static Material matOuterGrass;
    private static Material matTrack;
    private static Material matWhiteLine;
    private static Material matGoalPost;
    private static Material matGoalNet;
    private static Material matSoccerBall;
    private static Material matConcrete;
    private static Material matSeatBlue;
    private static Material matSeatRed;
    private static Material matSeatYellow;
    private static Material matTrussMetal;
    private static Material matTreeFoliage;
    private static Material matTreeBark;

    private static PhysicsMaterial soccerBallPhysMat;

    [MenuItem("Playground/Rebuild Playground Scene")]
    public static void Build()
    {
        Debug.Log(">>> Starting Playground & Soccer Field Scene Generation...");

        EnsureTag("SoccerBall");
        ConfigureTextures();
        CreatePhysicsMaterials();
        CreateMaterials();
        AnimationClip idleClip = CreateIdleAnimationClip();
        AnimationClip jumpClip = CreateJumpAnimationClip();
        AnimationClip runningClip = CreateNaturalRunAnimationClip();
        AnimatorController animController = CreateAnimatorController(idleClip, runningClip, jumpClip);

        // Create new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Build Environment
        GameObject rootEnv = new GameObject("Environment");

        BuildLightingAndSky(rootEnv);
        BuildGroundAndOuterApron(rootEnv);
        BuildSoccerPitch(rootEnv);
        BuildAthleticsTrack(rootEnv);
        BuildSoccerGoals(rootEnv);
        BuildSoccerBalls(rootEnv);
        BuildGrandstands(rootEnv);
        BuildFloodlightTowers(rootEnv);
        BuildPerimeterDecorations(rootEnv);

        // Build Player & Camera
        GameObject playerObj = BuildPlayerCharacter(animController);
        BuildCamera(playerObj);
        BuildHUD(playerObj);

        // Save Scene
        string scenePath = "Assets/Scenes/PlaygroundScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($">>> Scene successfully saved to {scenePath}");

        // Add to EditorBuildSettings
        AddSceneToBuildSettings(scenePath);

        Debug.Log(">>> Playground & Soccer Field Build Completed Successfully!");
    }

    private static void EnsureTag(string tag)
    {
        UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset != null && asset.Length > 0)
        {
            SerializedObject so = new SerializedObject(asset[0]);
            SerializedProperty tags = so.FindProperty("tags");
            bool exists = false;
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
                so.ApplyModifiedProperties();
                Debug.Log($"Added tag '{tag}' to TagManager.");
            }
        }
    }

    private static void ConfigureTextures()
    {
        // Normal map
        string normPath = "Assets/Character/Meshy_Character/Meshy_AI_Neutral_Studio_Stance_biped_texture_0_normal.png";
        TextureImporter ni = AssetImporter.GetAtPath(normPath) as TextureImporter;
        if (ni != null && ni.textureType != TextureImporterType.NormalMap)
        {
            ni.textureType = TextureImporterType.NormalMap;
            ni.SaveAndReimport();
        }

        // Net transparency
        string netPath = "Assets/Textures/Net_Grid.png";
        TextureImporter nti = AssetImporter.GetAtPath(netPath) as TextureImporter;
        if (nti != null && !nti.alphaIsTransparency)
        {
            nti.alphaIsTransparency = true;
            nti.SaveAndReimport();
        }
    }

    private static void CreatePhysicsMaterials()
    {
        soccerBallPhysMat = new PhysicsMaterial("SoccerBallPhysicsMaterial");
        soccerBallPhysMat.bounciness = 0.28f;
        soccerBallPhysMat.dynamicFriction = 0.65f;
        soccerBallPhysMat.staticFriction = 0.65f;
        soccerBallPhysMat.bounceCombine = PhysicsMaterialCombine.Average;
        soccerBallPhysMat.frictionCombine = PhysicsMaterialCombine.Average;
        AssetDatabase.CreateAsset(soccerBallPhysMat, "Assets/Materials/SoccerBallPhysicsMaterial.physicMaterial");
    }

    private static void CreateMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
            urpLit = Shader.Find("Standard");

        // Character Material
        matCharacter = new Material(urpLit);
        matCharacter.name = "Mat_Character";
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Character/Meshy_Character/Meshy_AI_Neutral_Studio_Stance_biped_texture_0.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Character/Meshy_Character/Meshy_AI_Neutral_Studio_Stance_biped_texture_0_normal.png");
        Texture2D metSmooth = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Character/Meshy_Character/Meshy_AI_Neutral_Studio_Stance_biped_metallic_smoothness.png");
        if (albedo != null) matCharacter.SetTexture("_BaseMap", albedo);
        if (normal != null) { matCharacter.SetTexture("_BumpMap", normal); matCharacter.EnableKeyword("_NORMALMAP"); }
        if (metSmooth != null) { matCharacter.SetTexture("_MetallicGlossMap", metSmooth); matCharacter.EnableKeyword("_METALLICSPECGLOSSMAP"); }
        matCharacter.SetFloat("_Smoothness", 0.8f);
        AssetDatabase.CreateAsset(matCharacter, "Assets/Materials/Mat_Character.mat");

        // Grass Turf
        matGrassTurf = new Material(urpLit);
        matGrassTurf.name = "Mat_GrassTurf";
        Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Grass_Turf.png");
        if (grassTex != null)
        {
            matGrassTurf.SetTexture("_BaseMap", grassTex);
            matGrassTurf.SetTextureScale("_BaseMap", new Vector2(8, 12));
        }
        matGrassTurf.SetFloat("_Smoothness", 0.15f);
        AssetDatabase.CreateAsset(matGrassTurf, "Assets/Materials/Mat_GrassTurf.mat");

        // Outer Grass
        matOuterGrass = new Material(urpLit);
        matOuterGrass.name = "Mat_OuterGrass";
        matOuterGrass.SetColor("_BaseColor", new Color(0.22f, 0.45f, 0.18f));
        matOuterGrass.SetFloat("_Smoothness", 0.1f);
        AssetDatabase.CreateAsset(matOuterGrass, "Assets/Materials/Mat_OuterGrass.mat");

        // Track Polyurethane
        matTrack = new Material(urpLit);
        matTrack.name = "Mat_Track";
        Texture2D trackTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Track_Polyurethane.png");
        if (trackTex != null)
        {
            matTrack.SetTexture("_BaseMap", trackTex);
            matTrack.SetTextureScale("_BaseMap", new Vector2(10, 10));
        }
        matTrack.SetColor("_BaseColor", new Color(0.85f, 0.28f, 0.22f));
        matTrack.SetFloat("_Smoothness", 0.3f);
        AssetDatabase.CreateAsset(matTrack, "Assets/Materials/Mat_Track.mat");

        // White Line
        matWhiteLine = new Material(urpLit);
        matWhiteLine.name = "Mat_WhiteLine";
        matWhiteLine.SetColor("_BaseColor", new Color(0.98f, 0.98f, 0.98f));
        matWhiteLine.SetFloat("_Smoothness", 0.25f);
        AssetDatabase.CreateAsset(matWhiteLine, "Assets/Materials/Mat_WhiteLine.mat");

        // Goal Post
        matGoalPost = new Material(urpLit);
        matGoalPost.name = "Mat_GoalPost";
        matGoalPost.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.95f));
        matGoalPost.SetFloat("_Smoothness", 0.85f);
        matGoalPost.SetFloat("_Metallic", 0.2f);
        AssetDatabase.CreateAsset(matGoalPost, "Assets/Materials/Mat_GoalPost.mat");

        // Goal Net (Cutout/Alpha)
        matGoalNet = new Material(urpLit);
        matGoalNet.name = "Mat_GoalNet";
        Texture2D netTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Net_Grid.png");
        if (netTex != null)
        {
            matGoalNet.SetTexture("_BaseMap", netTex);
            matGoalNet.SetTextureScale("_BaseMap", new Vector2(15, 8));
        }
        matGoalNet.SetFloat("_Surface", 1); // Transparent
        matGoalNet.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        matGoalNet.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        matGoalNet.SetInt("_ZWrite", 0);
        matGoalNet.renderQueue = (int)RenderQueue.Transparent;
        AssetDatabase.CreateAsset(matGoalNet, "Assets/Materials/Mat_GoalNet.mat");

        // Soccer Ball
        matSoccerBall = new Material(urpLit);
        matSoccerBall.name = "Mat_SoccerBall";
        Texture2D ballTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/SoccerBall_Texture.png");
        if (ballTex != null) matSoccerBall.SetTexture("_BaseMap", ballTex);
        matSoccerBall.SetFloat("_Smoothness", 0.65f);
        AssetDatabase.CreateAsset(matSoccerBall, "Assets/Materials/Mat_SoccerBall.mat");

        // Concrete
        matConcrete = new Material(urpLit);
        matConcrete.name = "Mat_Concrete";
        Texture2D concTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Concrete_Bleachers.png");
        if (concTex != null)
        {
            matConcrete.SetTexture("_BaseMap", concTex);
            matConcrete.SetTextureScale("_BaseMap", new Vector2(12, 12));
        }
        matConcrete.SetFloat("_Smoothness", 0.2f);
        AssetDatabase.CreateAsset(matConcrete, "Assets/Materials/Mat_Concrete.mat");

        // Stadium Seats
        matSeatBlue = new Material(urpLit);
        matSeatBlue.SetColor("_BaseColor", new Color(0.12f, 0.42f, 0.88f));
        matSeatBlue.SetFloat("_Smoothness", 0.75f);
        AssetDatabase.CreateAsset(matSeatBlue, "Assets/Materials/Mat_SeatBlue.mat");

        matSeatRed = new Material(urpLit);
        matSeatRed.SetColor("_BaseColor", new Color(0.88f, 0.18f, 0.15f));
        matSeatRed.SetFloat("_Smoothness", 0.75f);
        AssetDatabase.CreateAsset(matSeatRed, "Assets/Materials/Mat_SeatRed.mat");

        matSeatYellow = new Material(urpLit);
        matSeatYellow.SetColor("_BaseColor", new Color(0.95f, 0.78f, 0.12f));
        matSeatYellow.SetFloat("_Smoothness", 0.75f);
        AssetDatabase.CreateAsset(matSeatYellow, "Assets/Materials/Mat_SeatYellow.mat");

        // Truss Metal
        matTrussMetal = new Material(urpLit);
        matTrussMetal.SetColor("_BaseColor", new Color(0.38f, 0.42f, 0.46f));
        matTrussMetal.SetFloat("_Metallic", 0.7f);
        matTrussMetal.SetFloat("_Smoothness", 0.5f);
        AssetDatabase.CreateAsset(matTrussMetal, "Assets/Materials/Mat_TrussMetal.mat");

        // Trees
        matTreeFoliage = new Material(urpLit);
        matTreeFoliage.SetColor("_BaseColor", new Color(0.18f, 0.42f, 0.15f));
        matTreeFoliage.SetFloat("_Smoothness", 0.2f);
        AssetDatabase.CreateAsset(matTreeFoliage, "Assets/Materials/Mat_TreeFoliage.mat");

        matTreeBark = new Material(urpLit);
        matTreeBark.SetColor("_BaseColor", new Color(0.32f, 0.22f, 0.14f));
        matTreeBark.SetFloat("_Smoothness", 0.15f);
        AssetDatabase.CreateAsset(matTreeBark, "Assets/Materials/Mat_TreeBark.mat");

        AssetDatabase.SaveAssets();
    }

    private static AnimationClip CreateIdleAnimationClip()
    {
        AnimationClip clip = new AnimationClip();
        clip.name = "Idle";
        clip.frameRate = 30;
        clip.wrapMode = WrapMode.Loop;

        const float duration = 2.0f;

        // 1. 下半身：直立站姿，膝蓋伸直不彎曲（徹底解決跪姿問題）
        SetMuscleConstant(clip, "Left Upper Leg Front-Back", duration, 0.0f);
        SetMuscleConstant(clip, "Right Upper Leg Front-Back", duration, 0.0f);
        SetMuscleConstant(clip, "Left Upper Leg In-Out", duration, 0.0f);
        SetMuscleConstant(clip, "Right Upper Leg In-Out", duration, 0.0f);
        SetMuscleConstant(clip, "Left Lower Leg Stretch", duration, 0.0f);
        SetMuscleConstant(clip, "Right Lower Leg Stretch", duration, 0.0f);
        SetMuscleConstant(clip, "Left Foot Up-Down", duration, 0.0f);
        SetMuscleConstant(clip, "Right Foot Up-Down", duration, 0.0f);

        // 2. 上半身：雙手自然垂放於身體大腿兩側（徹底解決雙手擺在胸前問題）
        SetMuscleCurve(clip, "Left Arm Down-Up", duration, -0.74f, -0.72f, -0.74f);
        SetMuscleCurve(clip, "Right Arm Down-Up", duration, -0.74f, -0.72f, -0.74f);
        SetMuscleConstant(clip, "Left Arm Front-Back", duration, -0.06f);
        SetMuscleConstant(clip, "Right Arm Front-Back", duration, -0.06f);
        SetMuscleConstant(clip, "Left Arm In-Out", duration, 0.06f);
        SetMuscleConstant(clip, "Right Arm In-Out", duration, 0.06f);
        SetMuscleConstant(clip, "Left Forearm Stretch", duration, 0.0f);
        SetMuscleConstant(clip, "Right Forearm Stretch", duration, 0.0f);

        // 3. 脊椎微幅呼吸起伏
        SetMuscleCurve(clip, "Spine Front-Back", duration, 0.0f, 0.02f, 0.0f);
        SetMuscleCurve(clip, "Chest Front-Back", duration, 0.0f, 0.015f, 0.0f);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = "Assets/Character/Meshy_Character/Idle.anim";
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void SetMuscleConstant(AnimationClip clip, string muscle, float duration, float value)
    {
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), AnimationCurve.Constant(0f, duration, value));
    }

    private static AnimationClip CreateJumpAnimationClip()
    {
        AnimationClip clip = new AnimationClip();
        clip.name = "Jump";
        clip.frameRate = 30;
        clip.wrapMode = WrapMode.ClampForever;

        const float duration = 0.7f;

        // 雙臂起跳強力向上揮動，隨後在空中張開保持平衡，降落前收回
        SetMuscleCurve(clip, "Left Arm Down-Up", duration, -0.72f, 0.10f, -0.15f, -0.50f, -0.70f);
        SetMuscleCurve(clip, "Right Arm Down-Up", duration, -0.72f, 0.10f, -0.15f, -0.50f, -0.70f);
        SetMuscleCurve(clip, "Left Arm Front-Back", duration, -0.05f, 0.25f, 0.15f, 0.05f, -0.05f);
        SetMuscleCurve(clip, "Right Arm Front-Back", duration, -0.05f, 0.25f, 0.15f, 0.05f, -0.05f);
        SetMuscleCurve(clip, "Left Arm In-Out", duration, 0.06f, 0.20f, 0.25f, 0.15f, 0.06f);
        SetMuscleCurve(clip, "Right Arm In-Out", duration, 0.06f, 0.20f, 0.25f, 0.15f, 0.06f);
        SetMuscleCurve(clip, "Left Forearm Stretch", duration, 0.0f, -0.25f, -0.35f, -0.15f, 0.0f);
        SetMuscleCurve(clip, "Right Forearm Stretch", duration, 0.0f, -0.25f, -0.35f, -0.15f, 0.0f);

        // 下肢：起跳蹬地 -> 空中屈膝收腿 -> 下落伸展準備接觸地面
        SetMuscleCurve(clip, "Left Lower Leg Stretch", duration, 0.0f, -0.20f, -0.38f, -0.15f, 0.0f);
        SetMuscleCurve(clip, "Right Lower Leg Stretch", duration, 0.0f, -0.20f, -0.38f, -0.15f, 0.0f);
        SetMuscleCurve(clip, "Left Upper Leg Front-Back", duration, 0.0f, 0.15f, 0.12f, 0.05f, 0.0f);
        SetMuscleCurve(clip, "Right Upper Leg Front-Back", duration, 0.0f, 0.15f, 0.12f, 0.05f, 0.0f);
        SetMuscleCurve(clip, "Left Foot Up-Down", duration, 0.0f, -0.25f, -0.35f, -0.10f, 0.0f);
        SetMuscleCurve(clip, "Right Foot Up-Down", duration, 0.0f, -0.25f, -0.35f, -0.10f, 0.0f);

        // 軀幹展胸
        SetMuscleCurve(clip, "Spine Front-Back", duration, 0.0f, 0.05f, 0.08f, 0.03f, 0.0f);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = "Assets/Character/Meshy_Character/Jump.anim";
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateNaturalRunAnimationClip()
    {
        const float duration = 0.8f;
        const string path = "Assets/Character/Meshy_Character/NaturalRun.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.ClearCurves();
        clip.name = "NaturalRun";
        clip.frameRate = 30;
        clip.wrapMode = WrapMode.Loop;

        // Opposite arms and legs make a compact, upright running cycle.
        SetMuscleCurve(clip, "Left Upper Leg Front-Back", duration, 0.30f, -0.30f, 0.30f, -0.30f, 0.30f);
        SetMuscleCurve(clip, "Right Upper Leg Front-Back", duration, -0.30f, 0.30f, -0.30f, 0.30f, -0.30f);
        SetMuscleCurve(clip, "Left Lower Leg Stretch", duration, -0.04f, -0.22f, -0.04f, -0.22f, -0.04f);
        SetMuscleCurve(clip, "Right Lower Leg Stretch", duration, -0.22f, -0.04f, -0.22f, -0.04f, -0.22f);
        SetMuscleCurve(clip, "Left Arm Down-Up", duration, -0.62f, -0.56f, -0.62f, -0.68f, -0.62f);
        SetMuscleCurve(clip, "Right Arm Down-Up", duration, -0.62f, -0.68f, -0.62f, -0.56f, -0.62f);
        SetMuscleCurve(clip, "Left Arm Front-Back", duration, -0.20f, 0.20f, -0.20f, 0.20f, -0.20f);
        SetMuscleCurve(clip, "Right Arm Front-Back", duration, 0.20f, -0.20f, 0.20f, -0.20f, 0.20f);
        SetMuscleCurve(clip, "Spine Left-Right", duration, 0.02f, -0.02f, 0.02f, -0.02f, 0.02f);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void SetMuscleCurve(AnimationClip clip, string muscle, float duration, params float[] values)
    {
        Keyframe[] keys = new Keyframe[values.Length];
        for (int i = 0; i < values.Length; i++)
            keys[i] = new Keyframe(duration * i / (values.Length - 1), values[i]);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), new AnimationCurve(keys));
    }

    private static AnimationClip GetFbxRunningClip()
    {
        string fbxPath = "Assets/Character/Meshy_Character/Meshy_AI_Neutral_Studio_Stance_biped_Animation_Running_withSkin.fbx";
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var a in assets)
        {
            if (a is AnimationClip c && c.name == "Running")
                return c;
        }
        foreach (var a in assets)
        {
            if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                return c;
        }
        return null;
    }

    private static AnimatorController CreateAnimatorController(AnimationClip idleClip, AnimationClip runClip, AnimationClip jumpClip)
    {
        string path = "Assets/Character/Meshy_Character/Meshy_Animator.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("MotionSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);

        AnimatorStateMachine rootSM = controller.layers[0].stateMachine;

        // States
        AnimatorState stateIdle = rootSM.AddState("Idle");
        stateIdle.motion = idleClip;

        AnimatorState stateWalk = rootSM.AddState("Walk");
        stateWalk.motion = runClip != null ? runClip : idleClip;
        stateWalk.speed = 0.55f;
        stateWalk.speedParameterActive = true;
        stateWalk.speedParameter = "MotionSpeed";

        AnimatorState stateRun = rootSM.AddState("Run");
        stateRun.motion = runClip != null ? runClip : idleClip;
        stateRun.speed = 1.0f;
        stateRun.speedParameterActive = true;
        stateRun.speedParameter = "MotionSpeed";

        AnimatorState stateJump = rootSM.AddState("Jump");
        stateJump.motion = jumpClip;

        rootSM.defaultState = stateIdle;

        // Transitions: Idle <-> Walk
        var toWalk = stateIdle.AddTransition(stateWalk);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.15f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");

        var toIdle = stateWalk.AddTransition(stateIdle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

        // Transitions: Walk <-> Run
        var toRun = stateWalk.AddTransition(stateRun);
        toRun.hasExitTime = false;
        toRun.duration = 0.15f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 5.0f, "Speed");

        var toWalkFromRun = stateRun.AddTransition(stateWalk);
        toWalkFromRun.hasExitTime = false;
        toWalkFromRun.duration = 0.15f;
        toWalkFromRun.AddCondition(AnimatorConditionMode.Less, 4.8f, "Speed");

        var toIdleFromRun = stateRun.AddTransition(stateIdle);
        toIdleFromRun.hasExitTime = false;
        toIdleFromRun.duration = 0.15f;
        toIdleFromRun.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

        // Transitions: AnyState -> Jump
        var anyToJump = rootSM.AddAnyStateTransition(stateJump);
        anyToJump.hasExitTime = false;
        anyToJump.duration = 0.1f;
        anyToJump.canTransitionToSelf = false;
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");

        // Jump -> Idle (落地且靜止)
        var jumpToIdle = stateJump.AddTransition(stateIdle);
        jumpToIdle.hasExitTime = false;
        jumpToIdle.duration = 0.12f;
        jumpToIdle.canTransitionToSelf = false;
        jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        jumpToIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

        // Jump -> Walk (落地且仍在移動)
        var jumpToWalk = stateJump.AddTransition(stateWalk);
        jumpToWalk.hasExitTime = false;
        jumpToWalk.duration = 0.12f;
        jumpToWalk.canTransitionToSelf = false;
        jumpToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        jumpToWalk.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");

        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void BuildLightingAndSky(GameObject parent)
    {
        GameObject sun = new GameObject("Directional Light");
        sun.transform.parent = parent.transform;
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1.0f, 0.96f, 0.88f);
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48f, -35f, 0);

        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.1f;
    }

    private static void BuildGroundAndOuterApron(GameObject parent)
    {
        // Outer Ground: 240m x 170m
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "OuterGroundApron";
        ground.transform.parent = parent.transform;
        ground.transform.position = new Vector3(0, -0.05f, 0);
        ground.transform.localScale = new Vector3(24f, 1f, 17f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = matOuterGrass;
    }

    private static void BuildSoccerPitch(GameObject parent)
    {
        GameObject pitchGroup = new GameObject("SoccerPitch");
        pitchGroup.transform.parent = parent.transform;

        // 1. Central Grass Infield (64m x 100m)
        GameObject grassCenter = GameObject.CreatePrimitive(PrimitiveType.Plane);
        grassCenter.name = "Turf_CenterRectangle";
        grassCenter.transform.parent = pitchGroup.transform;
        grassCenter.transform.position = new Vector3(0, 0f, 0);
        grassCenter.transform.localScale = new Vector3(6.4f, 1f, 10f); // Plane standard size is 10x10, so 6.4x10 = 64m x 100m
        grassCenter.GetComponent<MeshRenderer>().sharedMaterial = matGrassTurf;

        // 2. North & South Grass Semicircles inside track turns
        BuildSemicircleMesh("Turf_NorthSemicircle", pitchGroup.transform, new Vector3(0, 0, 50), 32.0f, matGrassTurf, 0f, 180f);
        BuildSemicircleMesh("Turf_SouthSemicircle", pitchGroup.transform, new Vector3(0, 0, -50), 32.0f, matGrassTurf, 180f, 360f);

        // 3. Official Pitch Lines (White Strip Meshes at Y = 0.005)
        float lineWidth = 0.12f;

        // Touchlines (Sides)
        CreateLineStrip(pitchGroup.transform, "Line_Touchline_East", new Vector3(32, 0.005f, 0), new Vector3(lineWidth, 0.001f, 100f));
        CreateLineStrip(pitchGroup.transform, "Line_Touchline_West", new Vector3(-32, 0.005f, 0), new Vector3(lineWidth, 0.001f, 100f));

        // Goal lines (Ends)
        CreateLineStrip(pitchGroup.transform, "Line_GoalLine_North", new Vector3(0, 0.005f, 50), new Vector3(64f, 0.001f, lineWidth));
        CreateLineStrip(pitchGroup.transform, "Line_GoalLine_South", new Vector3(0, 0.005f, -50), new Vector3(64f, 0.001f, lineWidth));

        // Halfway line
        CreateLineStrip(pitchGroup.transform, "Line_Halfway", new Vector3(0, 0.005f, 0), new Vector3(64f, 0.001f, lineWidth));

        // Center circle (radius 9.15m)
        BuildRingLineMesh("Line_CenterCircle", pitchGroup.transform, Vector3.zero, 9.15f, lineWidth, 48, 0f, 360f);

        // Center spot
        GameObject centerSpot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        centerSpot.name = "Spot_Center";
        centerSpot.transform.parent = pitchGroup.transform;
        centerSpot.transform.position = new Vector3(0, 0.006f, 0);
        centerSpot.transform.localScale = new Vector3(0.5f, 0.002f, 0.5f);
        Object.DestroyImmediate(centerSpot.GetComponent<Collider>());
        centerSpot.GetComponent<MeshRenderer>().sharedMaterial = matWhiteLine;

        // North Penalty Area (40.3m x 16.5m -> Z: 33.5 to 50, X: -20.15 to +20.15)
        CreateLineStrip(pitchGroup.transform, "Line_PenArea_N_Front", new Vector3(0, 0.005f, 33.5f), new Vector3(40.3f, 0.001f, lineWidth));
        CreateLineStrip(pitchGroup.transform, "Line_PenArea_N_Left", new Vector3(-20.15f, 0.005f, 41.75f), new Vector3(lineWidth, 0.001f, 16.5f));
        CreateLineStrip(pitchGroup.transform, "Line_PenArea_N_Right", new Vector3(20.15f, 0.005f, 41.75f), new Vector3(lineWidth, 0.001f, 16.5f));

        // North Goal Area (18.3m x 5.5m -> Z: 44.5 to 50, X: -9.15 to +9.15)
        CreateLineStrip(pitchGroup.transform, "Line_GoalArea_N_Front", new Vector3(0, 0.005f, 44.5f), new Vector3(18.3f, 0.001f, lineWidth));
        CreateLineStrip(pitchGroup.transform, "Line_GoalArea_N_Left", new Vector3(-9.15f, 0.005f, 47.25f), new Vector3(lineWidth, 0.001f, 5.5f));
        CreateLineStrip(pitchGroup.transform, "Line_GoalArea_N_Right", new Vector3(9.15f, 0.005f, 47.25f), new Vector3(lineWidth, 0.001f, 5.5f));

        // North Penalty Spot (0, 0.006, 39)
        GameObject penSpotN = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        penSpotN.name = "Spot_Penalty_North";
        penSpotN.transform.parent = pitchGroup.transform;
        penSpotN.transform.position = new Vector3(0, 0.006f, 39f);
        penSpotN.transform.localScale = new Vector3(0.45f, 0.002f, 0.45f);
        Object.DestroyImmediate(penSpotN.GetComponent<Collider>());
        penSpotN.GetComponent<MeshRenderer>().sharedMaterial = matWhiteLine;

        // North Penalty Arc (radius 9.15m from spot, arc outside box)
        BuildRingLineMesh("Line_PenArc_North", pitchGroup.transform, new Vector3(0, 0.005f, 39f), 9.15f, lineWidth, 24, 215f, 325f);

        // South Penalty Area
        CreateLineStrip(pitchGroup.transform, "Line_PenArea_S_Front", new Vector3(0, 0.005f, -33.5f), new Vector3(40.3f, 0.001f, lineWidth));
        CreateLineStrip(pitchGroup.transform, "Line_PenArea_S_Left", new Vector3(-20.15f, 0.005f, -41.75f), new Vector3(lineWidth, 0.001f, 16.5f));
        CreateLineStrip(pitchGroup.transform, "Line_PenArea_S_Right", new Vector3(20.15f, 0.005f, -41.75f), new Vector3(lineWidth, 0.001f, 16.5f));

        // South Goal Area
        CreateLineStrip(pitchGroup.transform, "Line_GoalArea_S_Front", new Vector3(0, 0.005f, -44.5f), new Vector3(18.3f, 0.001f, lineWidth));
        CreateLineStrip(pitchGroup.transform, "Line_GoalArea_S_Left", new Vector3(-9.15f, 0.005f, -47.25f), new Vector3(lineWidth, 0.001f, 5.5f));
        CreateLineStrip(pitchGroup.transform, "Line_GoalArea_S_Right", new Vector3(9.15f, 0.005f, -47.25f), new Vector3(lineWidth, 0.001f, 5.5f));

        // South Penalty Spot
        GameObject penSpotS = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        penSpotS.name = "Spot_Penalty_South";
        penSpotS.transform.parent = pitchGroup.transform;
        penSpotS.transform.position = new Vector3(0, 0.006f, -39f);
        penSpotS.transform.localScale = new Vector3(0.45f, 0.002f, 0.45f);
        Object.DestroyImmediate(penSpotS.GetComponent<Collider>());
        penSpotS.GetComponent<MeshRenderer>().sharedMaterial = matWhiteLine;

        // South Penalty Arc
        BuildRingLineMesh("Line_PenArc_South", pitchGroup.transform, new Vector3(0, 0.005f, -39f), 9.15f, lineWidth, 24, 35f, 145f);

        // Corner Arcs (1m radius at all four corners)
        BuildRingLineMesh("CornerArc_NE", pitchGroup.transform, new Vector3(32, 0.005f, 50), 1.0f, lineWidth, 12, 180f, 270f);
        BuildRingLineMesh("CornerArc_NW", pitchGroup.transform, new Vector3(-32, 0.005f, 50), 1.0f, lineWidth, 12, 270f, 360f);
        BuildRingLineMesh("CornerArc_SE", pitchGroup.transform, new Vector3(32, 0.005f, -50), 1.0f, lineWidth, 12, 90f, 180f);
        BuildRingLineMesh("CornerArc_SW", pitchGroup.transform, new Vector3(-32, 0.005f, -50), 1.0f, lineWidth, 12, 0f, 90f);
    }

    private static void BuildAthleticsTrack(GameObject parent)
    {
        GameObject trackGroup = new GameObject("AthleticsTrack_400m");
        trackGroup.transform.parent = parent.transform;

        float rInner = 32.0f;
        float rOuter = 40.0f;
        float trackWidth = rOuter - rInner; // 8.0m
        int laneCount = 6;
        float laneWidth = trackWidth / laneCount; // 1.333m

        // 1. East Straightaway (X: 32 to 40, Z: -50 to 50)
        GameObject eastStraight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eastStraight.name = "Track_EastStraight";
        eastStraight.transform.parent = trackGroup.transform;
        eastStraight.transform.position = new Vector3(36f, 0.001f, 0);
        eastStraight.transform.localScale = new Vector3(trackWidth, 0.002f, 100f);
        eastStraight.GetComponent<MeshRenderer>().sharedMaterial = matTrack;

        // 2. West Straightaway (X: -40 to -32, Z: -50 to 50)
        GameObject westStraight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        westStraight.name = "Track_WestStraight";
        westStraight.transform.parent = trackGroup.transform;
        westStraight.transform.position = new Vector3(-36f, 0.001f, 0);
        westStraight.transform.localScale = new Vector3(trackWidth, 0.002f, 100f);
        westStraight.GetComponent<MeshRenderer>().sharedMaterial = matTrack;

        // 3. North Curved Turn (Arc around (0, 0, 50) from 0 to 180 deg)
        BuildCurvedTrackSurface("Track_NorthTurn", trackGroup.transform, new Vector3(0, 0, 50), rInner, rOuter, 48, 0f, 180f);

        // 4. South Curved Turn (Arc around (0, 0, -50) from 180 to 360 deg)
        BuildCurvedTrackSurface("Track_SouthTurn", trackGroup.transform, new Vector3(0, 0, -50), rInner, rOuter, 48, 180f, 360f);

        // 5. White Lane Markings (7 lines dividing the 6 lanes)
        float trackLineW = 0.07f;
        for (int i = 0; i <= laneCount; i++)
        {
            float radius = rInner + i * laneWidth;

            // Straight lines
            CreateLineStrip(trackGroup.transform, $"LaneLine_East_{i}", new Vector3(radius, 0.006f, 0), new Vector3(trackLineW, 0.001f, 100f));
            CreateLineStrip(trackGroup.transform, $"LaneLine_West_{i}", new Vector3(-radius, 0.006f, 0), new Vector3(trackLineW, 0.001f, 100f));

            // Curved lines
            BuildRingLineMesh($"LaneLine_North_{i}", trackGroup.transform, new Vector3(0, 0.006f, 50), radius, trackLineW, 48, 0f, 180f);
            BuildRingLineMesh($"LaneLine_South_{i}", trackGroup.transform, new Vector3(0, 0.006f, -50), radius, trackLineW, 48, 180f, 360f);
        }

        // Finish Line on East Straight (Z = 0)
        GameObject finishLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finishLine.name = "Track_FinishLine";
        finishLine.transform.parent = trackGroup.transform;
        finishLine.transform.position = new Vector3(36f, 0.007f, 0);
        finishLine.transform.localScale = new Vector3(trackWidth, 0.002f, 0.35f);
        Object.DestroyImmediate(finishLine.GetComponent<Collider>());
        finishLine.GetComponent<MeshRenderer>().sharedMaterial = matWhiteLine;

        // Starting staggered lines
        for (int lane = 0; lane < laneCount; lane++)
        {
            float laneCenter = rInner + (lane + 0.5f) * laneWidth;
            float staggerZ = -lane * 3.8f; // Stagger compensation
            GameObject startLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            startLine.name = $"StartLine_Lane_{lane + 1}";
            startLine.transform.parent = trackGroup.transform;
            startLine.transform.position = new Vector3(laneCenter, 0.007f, staggerZ - 20f);
            startLine.transform.localScale = new Vector3(laneWidth * 0.9f, 0.002f, 0.15f);
            Object.DestroyImmediate(startLine.GetComponent<Collider>());
            startLine.GetComponent<MeshRenderer>().sharedMaterial = matWhiteLine;
        }
    }

    private static void BuildSoccerGoals(GameObject parent)
    {
        GameObject goalsGroup = new GameObject("SoccerGoals");
        goalsGroup.transform.parent = parent.transform;

        BuildSingleGoal(goalsGroup.transform, "SoccerGoal_North", new Vector3(0, 0, 50), 180f);
        BuildSingleGoal(goalsGroup.transform, "SoccerGoal_South", new Vector3(0, 0, -50), 0f);
    }

    private static void BuildSingleGoal(Transform parent, string name, Vector3 pos, float rotY)
    {
        GameObject goal = new GameObject(name);
        goal.transform.parent = parent;
        goal.transform.position = pos;
        goal.transform.rotation = Quaternion.Euler(0, rotY, 0);

        float goalWidth = 7.32f;
        float goalHeight = 2.44f;
        float goalDepth = 2.0f;
        float postRadius = 0.06f;

        // Left Post
        CreateCylinder(goal.transform, "Post_Left", new Vector3(-goalWidth * 0.5f, goalHeight * 0.5f, 0), new Vector3(postRadius * 2, goalHeight * 0.5f, postRadius * 2), matGoalPost);
        // Right Post
        CreateCylinder(goal.transform, "Post_Right", new Vector3(goalWidth * 0.5f, goalHeight * 0.5f, 0), new Vector3(postRadius * 2, goalHeight * 0.5f, postRadius * 2), matGoalPost);

        // Crossbar
        GameObject crossbar = CreateCylinder(goal.transform, "Crossbar", new Vector3(0, goalHeight, 0), new Vector3(postRadius * 2, goalWidth * 0.5f, postRadius * 2), matGoalPost);
        crossbar.transform.rotation = Quaternion.Euler(0, rotY, 90f);

        // Ground side back bars
        CreateCylinder(goal.transform, "GroundBar_Left", new Vector3(-goalWidth * 0.5f, 0.05f, -goalDepth * 0.5f), new Vector3(postRadius * 1.5f, goalDepth * 0.5f, postRadius * 1.5f), matGoalPost).transform.rotation = Quaternion.Euler(90f, rotY, 0);
        CreateCylinder(goal.transform, "GroundBar_Right", new Vector3(goalWidth * 0.5f, 0.05f, -goalDepth * 0.5f), new Vector3(postRadius * 1.5f, goalDepth * 0.5f, postRadius * 1.5f), matGoalPost).transform.rotation = Quaternion.Euler(90f, rotY, 0);
        // Ground back bar
        CreateCylinder(goal.transform, "GroundBar_Back", new Vector3(0, 0.05f, -goalDepth), new Vector3(postRadius * 1.5f, goalWidth * 0.5f, postRadius * 1.5f), matGoalPost).transform.rotation = Quaternion.Euler(0, rotY, 90f);

        // Slanted top support frame
        Vector3 topSlantMid = new Vector3(0, goalHeight * 0.5f, -goalDepth * 0.5f);
        float slantLength = Mathf.Sqrt(goalHeight * goalHeight + goalDepth * goalDepth);
        float slantAngle = Mathf.Atan2(goalDepth, goalHeight) * Mathf.Rad2Deg;

        var slantL = CreateCylinder(goal.transform, "SupportSlant_Left", new Vector3(-goalWidth * 0.5f, topSlantMid.y, topSlantMid.z), new Vector3(postRadius * 1.2f, slantLength * 0.5f, postRadius * 1.2f), matGoalPost);
        slantL.transform.rotation = Quaternion.Euler(rotY, 0, 0) * Quaternion.Euler(slantAngle, 0, 0);

        var slantR = CreateCylinder(goal.transform, "SupportSlant_Right", new Vector3(goalWidth * 0.5f, topSlantMid.y, topSlantMid.z), new Vector3(postRadius * 1.2f, slantLength * 0.5f, postRadius * 1.2f), matGoalPost);
        slantR.transform.rotation = Quaternion.Euler(rotY, 0, 0) * Quaternion.Euler(slantAngle, 0, 0);

        // Net Meshes
        // Back Net (slanted)
        GameObject netBack = GameObject.CreatePrimitive(PrimitiveType.Quad);
        netBack.name = "Net_Back";
        netBack.transform.parent = goal.transform;
        netBack.transform.localPosition = new Vector3(0, goalHeight * 0.5f, -goalDepth * 0.5f);
        netBack.transform.localRotation = Quaternion.Euler(slantAngle - 90f, 0, 0);
        netBack.transform.localScale = new Vector3(goalWidth, slantLength, 1f);
        netBack.GetComponent<MeshRenderer>().sharedMaterial = matGoalNet;

        // Left Net
        GameObject netLeft = GameObject.CreatePrimitive(PrimitiveType.Quad);
        netLeft.name = "Net_Left";
        netLeft.transform.parent = goal.transform;
        netLeft.transform.localPosition = new Vector3(-goalWidth * 0.5f, goalHeight * 0.5f, -goalDepth * 0.5f);
        netLeft.transform.localRotation = Quaternion.Euler(0, 90f, 0);
        netLeft.transform.localScale = new Vector3(goalDepth, goalHeight, 1f);
        netLeft.GetComponent<MeshRenderer>().sharedMaterial = matGoalNet;

        // Right Net
        GameObject netRight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        netRight.name = "Net_Right";
        netRight.transform.parent = goal.transform;
        netRight.transform.localPosition = new Vector3(goalWidth * 0.5f, goalHeight * 0.5f, -goalDepth * 0.5f);
        netRight.transform.localRotation = Quaternion.Euler(0, -90f, 0);
        netRight.transform.localScale = new Vector3(goalDepth, goalHeight, 1f);
        netRight.GetComponent<MeshRenderer>().sharedMaterial = matGoalNet;

        // Goal Trigger Volume
        GameObject goalTrigger = new GameObject("GoalTriggerZone");
        goalTrigger.transform.parent = goal.transform;
        goalTrigger.transform.localPosition = new Vector3(0, goalHeight * 0.5f, -goalDepth * 0.5f);
        BoxCollider triggerCol = goalTrigger.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector3(goalWidth * 0.95f, goalHeight * 0.95f, goalDepth * 0.9f);

        SoccerGoalDetector detector = goalTrigger.AddComponent<SoccerGoalDetector>();
        detector.goalName = (rotY > 90f) ? "北側球門" : "南側球門";
        detector.ballResetPosition = (rotY > 90f) ? new Vector3(0, 0.25f, 39f) : new Vector3(0, 0.25f, -39f);
    }

    private static void BuildSoccerBalls(GameObject parent)
    {
        GameObject ballGroup = new GameObject("SoccerBalls");
        ballGroup.transform.parent = parent.transform;

        CreateSingleSoccerBall(ballGroup.transform, "SoccerBall_Center", new Vector3(0, 0.22f, 0));
        CreateSingleSoccerBall(ballGroup.transform, "SoccerBall_NorthPenalty", new Vector3(0, 0.22f, 39f));
        CreateSingleSoccerBall(ballGroup.transform, "SoccerBall_SouthPenalty", new Vector3(0, 0.22f, -39f));
    }

    private static void CreateSingleSoccerBall(Transform parent, string name, Vector3 pos)
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = name;
        ball.tag = "SoccerBall";
        ball.transform.parent = parent;
        ball.transform.position = pos;
        ball.transform.localScale = new Vector3(0.44f, 0.44f, 0.44f);
        ball.GetComponent<MeshRenderer>().sharedMaterial = matSoccerBall;

        SphereCollider col = ball.GetComponent<SphereCollider>();
        col.sharedMaterial = soccerBallPhysMat;

        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.mass = 0.45f;
        rb.linearDamping = 1.1f;
        rb.angularDamping = 0.8f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private static void BuildGrandstands(GameObject parent)
    {
        GameObject grandstand = new GameObject("SpectatorGrandstand");
        grandstand.transform.parent = parent.transform;
        grandstand.transform.position = new Vector3(45f, 0, 0);

        int tiers = 8;
        float tierDepth = 1.6f;
        float tierHeight = 0.45f;
        float standLength = 70.0f;

        // Concrete Steps
        for (int i = 0; i < tiers; i++)
        {
            float stepX = (i + 0.5f) * tierDepth;
            float stepY = (i + 0.5f) * tierHeight;

            GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"Bleacher_Tier_{i + 1}";
            step.transform.parent = grandstand.transform;
            step.transform.localPosition = new Vector3(stepX, stepY, 0);
            step.transform.localScale = new Vector3(tierDepth, (i + 1) * tierHeight, standLength);
            step.GetComponent<MeshRenderer>().sharedMaterial = matConcrete;

            // Add seats
            Material seatMat = (i % 3 == 0) ? matSeatBlue : ((i % 3 == 1) ? matSeatRed : matSeatYellow);
            int seatCount = 28;
            float seatSpacing = standLength / (seatCount + 1);
            for (int s = 0; s < seatCount; s++)
            {
                if (s == 13 || s == 14) continue; // Leave aisle in middle
                float seatZ = -standLength * 0.5f + (s + 1) * seatSpacing;
                GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seat.name = $"Seat_{i}_{s}";
                seat.transform.parent = grandstand.transform;
                seat.transform.localPosition = new Vector3(stepX, (i + 1) * tierHeight + 0.15f, seatZ);
                seat.transform.localScale = new Vector3(0.55f, 0.3f, 0.55f);
                Object.DestroyImmediate(seat.GetComponent<Collider>());
                seat.GetComponent<MeshRenderer>().sharedMaterial = seatMat;
            }
        }

        // Canopy Roof over upper tiers
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Grandstand_CanopyRoof";
        roof.transform.parent = grandstand.transform;
        roof.transform.localPosition = new Vector3(tiers * tierDepth * 0.6f, tiers * tierHeight + 4.5f, 0);
        roof.transform.localRotation = Quaternion.Euler(0, 0, -8f);
        roof.transform.localScale = new Vector3(tiers * tierDepth * 1.3f, 0.25f, standLength + 4f);
        roof.GetComponent<MeshRenderer>().sharedMaterial = matTrussMetal;

        // Roof support pillars
        for (int p = -2; p <= 2; p++)
        {
            float pZ = p * (standLength * 0.22f);
            GameObject pillar = CreateCylinder(grandstand.transform, $"Pillar_{p}", new Vector3(tiers * tierDepth, (tiers * tierHeight + 4.5f) * 0.5f, pZ), new Vector3(0.35f, (tiers * tierHeight + 4.5f) * 0.5f, 0.35f), matTrussMetal);
        }
    }

    private static void BuildFloodlightTowers(GameObject parent)
    {
        GameObject lightsGroup = new GameObject("FloodlightTowers");
        lightsGroup.transform.parent = parent.transform;

        BuildSingleLightTower(lightsGroup.transform, "Tower_NE", new Vector3(45f, 0, 62f), new Vector3(-25f, -25f, 0));
        BuildSingleLightTower(lightsGroup.transform, "Tower_NW", new Vector3(-45f, 0, 62f), new Vector3(-25f, 25f, 0));
        BuildSingleLightTower(lightsGroup.transform, "Tower_SE", new Vector3(45f, 0, -62f), new Vector3(25f, -25f, 0));
        BuildSingleLightTower(lightsGroup.transform, "Tower_SW", new Vector3(-45f, 0, -62f), new Vector3(25f, 25f, 0));
    }

    private static void BuildSingleLightTower(Transform parent, string name, Vector3 pos, Vector3 targetAngles)
    {
        GameObject tower = new GameObject(name);
        tower.transform.parent = parent;
        tower.transform.position = pos;

        float towerH = 22.0f;
        // Mast
        GameObject mast = CreateCylinder(tower.transform, "Mast", new Vector3(0, towerH * 0.5f, 0), new Vector3(0.8f, towerH * 0.5f, 0.8f), matTrussMetal);

        // Platform Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "LightPlatform";
        head.transform.parent = tower.transform;
        head.transform.localPosition = new Vector3(0, towerH, 0);
        head.transform.localScale = new Vector3(3.5f, 0.3f, 2.0f);
        head.GetComponent<MeshRenderer>().sharedMaterial = matTrussMetal;

        // Spotlights
        GameObject spotObj = new GameObject("StadiumSpotlight");
        spotObj.transform.parent = tower.transform;
        spotObj.transform.localPosition = new Vector3(0, towerH - 0.2f, 0);
        spotObj.transform.localRotation = Quaternion.Euler(38f, Mathf.Atan2(-pos.x, -pos.z) * Mathf.Rad2Deg, 0);

        Light spot = spotObj.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(1f, 0.98f, 0.92f);
        spot.range = 80f;
        spot.spotAngle = 65f;
        spot.innerSpotAngle = 35f;
        spot.intensity = 2500f;
        spot.shadows = LightShadows.None;
    }

    private static void BuildPerimeterDecorations(GameObject parent)
    {
        GameObject decoGroup = new GameObject("PerimeterDecorations");
        decoGroup.transform.parent = parent.transform;

        // Flagpoles at North turn
        for (int f = -1; f <= 1; f++)
        {
            Vector3 polePos = new Vector3(f * 3.5f, 0, 72f);
            CreateCylinder(decoGroup.transform, $"Flagpole_{f}", new Vector3(polePos.x, 6f, polePos.z), new Vector3(0.12f, 6f, 0.12f), matGoalPost);

            // Flag cloth
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flag.name = $"Flag_{f}";
            flag.transform.parent = decoGroup.transform;
            flag.transform.position = new Vector3(polePos.x + 0.9f, 11.2f, polePos.z);
            flag.transform.localScale = new Vector3(1.8f, 1.1f, 1f);
            Material flagMat = (f == 0) ? matSeatRed : ((f < 0) ? matSeatBlue : matSeatYellow);
            flag.GetComponent<MeshRenderer>().sharedMaterial = flagMat;
        }

        // Perimeter Trees
        float[] treeX = new float[] { -62, -58, -55, 62, 60, 65 };
        float[] treeZ = new float[] { -70, -40, 0, 40, 70 };
        foreach (float x in treeX)
        {
            foreach (float z in treeZ)
            {
                BuildTree(decoGroup.transform, new Vector3(x, 0, z));
            }
        }
    }

    private static void BuildTree(Transform parent, Vector3 pos)
    {
        GameObject tree = new GameObject("Tree");
        tree.transform.parent = parent;
        tree.transform.position = pos;

        // Trunk
        CreateCylinder(tree.transform, "Trunk", new Vector3(0, 2f, 0), new Vector3(0.45f, 2f, 0.45f), matTreeBark);

        // Foliage Layers
        for (int i = 0; i < 3; i++)
        {
            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = $"Foliage_{i}";
            foliage.transform.parent = tree.transform;
            foliage.transform.localPosition = new Vector3(0, 3.8f + i * 1.5f, 0);
            float scale = 3.2f - i * 0.7f;
            foliage.transform.localScale = new Vector3(scale, scale * 1.1f, scale);
            foliage.GetComponent<MeshRenderer>().sharedMaterial = matTreeFoliage;
        }
    }

    private static GameObject BuildPlayerCharacter(AnimatorController animController)
    {
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 0.02f, -8f); // Away from centre-ball spawn
        player.transform.rotation = Quaternion.identity;

        // CharacterController
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.7f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.85f, 0);
        cc.stepOffset = 0.35f;
        cc.slopeLimit = 45f;
        cc.skinWidth = 0.02f;

        // PlayerController Script
        PlayerController pc = player.AddComponent<PlayerController>();
        pc.walkSpeed = 4.0f;
        pc.runSpeed = 8.5f;
        pc.jumpVelocity = 6.5f;
        pc.kickForce = 1.4f;

        // Instantiate Meshy 3D Model
        string fbxPath = "Assets/Character/Meshy_Character/Meshy_AI_Neutral_Studio_Stance_biped_Animation_Running_withSkin.fbx";
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (modelPrefab != null)
        {
            GameObject modelInstance = Object.Instantiate(modelPrefab, player.transform);
            modelInstance.name = "Meshy_Model";
            modelInstance.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            modelInstance.transform.localRotation = Quaternion.identity;

            // Apply Character Material
            SkinnedMeshRenderer[] smrs = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var smr in smrs)
            {
                smr.sharedMaterial = matCharacter;
            }

            // Setup Animator
            Animator anim = modelInstance.GetComponent<Animator>();
            if (anim == null)
                anim = modelInstance.AddComponent<Animator>();

            anim.runtimeAnimatorController = animController;
            anim.applyRootMotion = false;
            pc.animator = anim;
        }
        else
        {
            Debug.LogError("Failed to load model prefab from " + fbxPath);
        }

        return player;
    }

    private static void BuildCamera(GameObject player)
    {
        GameObject camObj = GameObject.FindWithTag("MainCamera");
        if (camObj == null)
        {
            camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        camObj.transform.position = player.transform.position + new Vector3(0, 2.5f, -5f);
        camObj.transform.LookAt(player.transform.position + new Vector3(0, 1.5f, 0));

        ThirdPersonCamera tpc = camObj.GetComponent<ThirdPersonCamera>();
        if (tpc == null)
            tpc = camObj.AddComponent<ThirdPersonCamera>();

        tpc.target = player.transform;
        tpc.targetOffset = new Vector3(0, 1.5f, 0);
        tpc.defaultDistance = 5.0f;
        tpc.minDistance = 2.0f;
        tpc.maxDistance = 14.0f;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
            pc.cameraTransform = camObj.transform;
    }

    private static void BuildHUD(GameObject player)
    {
        GameObject hudObj = new GameObject("HUDManager");
        PlaygroundHUD hud = hudObj.AddComponent<PlaygroundHUD>();
        hud.player = player.GetComponent<PlayerController>();
    }

    // Helper Mesh Builders
    private static void CreateLineStrip(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = name;
        strip.transform.parent = parent;
        strip.transform.position = pos;
        strip.transform.localScale = size;
        Object.DestroyImmediate(strip.GetComponent<Collider>());
        strip.GetComponent<MeshRenderer>().sharedMaterial = matWhiteLine;
    }

    private static GameObject CreateCylinder(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = name;
        cyl.transform.parent = parent;
        cyl.transform.localPosition = pos;
        cyl.transform.localScale = scale;
        cyl.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return cyl;
    }

    private static void BuildSemicircleMesh(string name, Transform parent, Vector3 center, float radius, Material mat, float startAngle, float endAngle)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = parent;
        obj.transform.position = center;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        int segments = 36;
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        verts.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            verts.Add(new Vector3(x, 0, z));
            uvs.Add(new Vector2(0.5f + (x / radius) * 0.5f, 0.5f + (z / radius) * 0.5f));
        }

        for (int i = 1; i <= segments; i++)
        {
            tris.Add(0);
            tris.Add(i + 1);
            tris.Add(i);
        }

        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;

        MeshCollider mc = obj.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
    }

    private static void BuildCurvedTrackSurface(string name, Transform parent, Vector3 center, float rInner, float rOuter, int segments, float startAngle, float endAngle)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = parent;
        obj.transform.position = center;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = matTrack;

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            verts.Add(new Vector3(cos * rInner, 0.001f, sin * rInner));
            verts.Add(new Vector3(cos * rOuter, 0.001f, sin * rOuter));

            uvs.Add(new Vector2(t * 8f, 0));
            uvs.Add(new Vector2(t * 8f, 1));
        }

        for (int i = 0; i < segments; i++)
        {
            int i0 = i * 2;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;

            tris.Add(i0);
            tris.Add(i2);
            tris.Add(i1);

            tris.Add(i1);
            tris.Add(i2);
            tris.Add(i3);
        }

        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;

        MeshCollider mc = obj.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
    }

    private static void BuildRingLineMesh(string name, Transform parent, Vector3 center, float radius, float lineWidth, int segments, float startAngle, float endAngle)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = parent;
        obj.transform.position = center;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = matWhiteLine;

        float rIn = radius - lineWidth * 0.5f;
        float rOut = radius + lineWidth * 0.5f;

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            verts.Add(new Vector3(cos * rIn, 0.006f, sin * rIn));
            verts.Add(new Vector3(cos * rOut, 0.006f, sin * rOut));
        }

        for (int i = 0; i < segments; i++)
        {
            int i0 = i * 2;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;

            tris.Add(i0);
            tris.Add(i2);
            tris.Add(i1);

            tris.Add(i1);
            tris.Add(i2);
            tris.Add(i3);
        }

        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in scenes)
        {
            if (s.path == scenePath)
            {
                s.enabled = true;
                found = true;
                break;
            }
        }
        if (!found)
        {
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
