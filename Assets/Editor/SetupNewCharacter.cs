using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class SetupNewCharacter
{
    static SetupNewCharacter()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Run();
        };
    }

    [MenuItem("Playground/3. Setup New Casual Character (安裝新角色與動畫)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        string dir = "Assets/Character/Meshy_AI_Casual_Studio_Stance_biped/";
        string characterFbx = dir + "Meshy_AI_Casual_Studio_Stance_biped_Character_output.fbx";
        string animFbx = dir + "Meshy_AI_Casual_Studio_Stance_biped_Meshy_Merged_Animations.fbx";

        if (!System.IO.File.Exists(characterFbx) || !System.IO.File.Exists(animFbx))
        {
            Debug.LogWarning("[SetupNewCharacter] Character files not found in " + dir);
            return;
        }

        Debug.Log(">>> [SetupNewCharacter] 設定全新休閒站姿角色 (Casual Studio Stance)...");

        // 1. 設定 ModelImporter 為 Humanoid
        EnsureHumanoid(characterFbx);
        EnsureHumanoid(animFbx);

        // 2. 確保 FBX 動畫全數設定循環播放 (Loop Time = true)
        EnsureAnimationClipsLoop(animFbx);

        // 3. 建立新材質
        Material mat = CreateMaterial(dir);

        // 4. 搜尋並指派 FBX 內置的高品質循環動畫
        AssignMergedAnimations(characterFbx, animFbx);

        // 5. 替換場景中的 Player 模型並重設為地面 y=0 零偏移
        ReplacePlayerModel(characterFbx, mat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green><b>[SetupNewCharacter]</b> 全新角色與循環跑步動作已安裝並綁定成功！</color>");
    }

    private static void EnsureHumanoid(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            Debug.Log($"[SetupNewCharacter] 已將 {fbxPath} 設為 Humanoid 骨架。");
        }
    }

    private static void EnsureAnimationClipsLoop(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        bool needsReimport = false;
        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            ModelImporterClipAnimation[] defaultClips = importer.defaultClipAnimations;
            if (defaultClips != null && defaultClips.Length > 0)
            {
                clips = new ModelImporterClipAnimation[defaultClips.Length];
                for (int i = 0; i < defaultClips.Length; i++)
                {
                    clips[i] = new ModelImporterClipAnimation
                    {
                        name = defaultClips[i].name,
                        takeName = defaultClips[i].takeName,
                        firstFrame = defaultClips[i].firstFrame,
                        lastFrame = defaultClips[i].lastFrame,
                        wrapMode = WrapMode.Loop,
                        loopTime = true,
                        loopPose = true,
                        lockRootRotation = true,
                        lockRootHeightY = true,
                        lockRootPositionXZ = true,
                        keepOriginalOrientation = true,
                        keepOriginalPositionY = true,
                        keepOriginalPositionXZ = false,
                        heightFromFeet = false
                    };
                }
                importer.clipAnimations = clips;
                needsReimport = true;
            }
        }
        else
        {
            foreach (var clip in clips)
            {
                if (!clip.loopTime)
                {
                    clip.loopTime = true;
                    clip.loopPose = true;
                    clip.lockRootRotation = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootPositionXZ = true;
                    clip.keepOriginalPositionY = true;
                    needsReimport = true;
                }
            }
            if (needsReimport)
            {
                importer.clipAnimations = clips;
            }
        }

        if (needsReimport)
        {
            importer.SaveAndReimport();
            Debug.Log($"[SetupNewCharacter] 已將 {fbxPath} 內的動畫剪輯設為循環播放 (Loop Time = true)。");
        }
    }

    private static Material CreateMaterial(string dir)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        string matPath = "Assets/Materials/Mat_CasualCharacter.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(urpLit);
            mat.name = "Mat_CasualCharacter";
            AssetDatabase.CreateAsset(mat, matPath);
        }

        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Meshy_AI_Casual_Studio_Stance_biped_texture_0.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Meshy_AI_Casual_Studio_Stance_biped_texture_0_normal.png");
        Texture2D rough = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Meshy_AI_Casual_Studio_Stance_biped_texture_0_roughness.png");
        Texture2D metal = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Meshy_AI_Casual_Studio_Stance_biped_texture_0_metallic.png");

        if (normal != null)
        {
            string normalPath = AssetDatabase.GetAssetPath(normal);
            TextureImporter ni = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (ni != null && ni.textureType != TextureImporterType.NormalMap)
            {
                ni.textureType = TextureImporterType.NormalMap;
                ni.SaveAndReimport();
            }
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (albedo != null) mat.SetTexture("_BaseMap", albedo);
        if (metal != null)
        {
            mat.SetTexture("_MetallicGlossMap", metal);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        mat.SetFloat("_Smoothness", 0.65f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void AssignMergedAnimations(string characterFbx, string animFbx)
    {
        AnimationClip runClip = null;
        AnimationClip walkClip = null;
        AnimationClip idleClip = null;

        // 從 Merged_Animations 搜尋所有 Clips
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(animFbx);
        foreach (var obj in allAssets)
        {
            if (obj is AnimationClip c && !c.name.StartsWith("__preview__"))
            {
                string lower = c.name.ToLower();
                Debug.Log($"[SetupNewCharacter] 掃描到動畫剪輯: '{c.name}' (長度: {c.length:F2}s, isLooping: {c.isLooping})");
                if (lower.Contains("run") && runClip == null) runClip = c;
                else if (lower.Contains("walk") && walkClip == null) walkClip = c;
                else if (lower.Contains("idle") && idleClip == null) idleClip = c;
            }
        }

        // 如果只有一個 runClip，walk 也通用
        if (walkClip == null) walkClip = runClip;

        // 使用直立待機 Idle.anim (已設定 RootT.y 保持站立高度)
        if (idleClip == null)
            idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character/Meshy_Character/Idle.anim");

        AnimationClip jumpClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character/Meshy_Character/Jump.anim");

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Character/Meshy_Character/Meshy_Animator.controller");
        if (controller != null)
        {
            foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
            {
                if (state.state.name == "Idle" && idleClip != null) state.state.motion = idleClip;
                else if (state.state.name == "Walk" && walkClip != null)
                {
                    state.state.motion = walkClip;
                    state.state.speedParameterActive = true;
                    state.state.speedParameter = "MotionSpeed";
                }
                else if (state.state.name == "Run" && runClip != null)
                {
                    state.state.motion = runClip;
                    state.state.speedParameterActive = true;
                    state.state.speedParameter = "MotionSpeed";
                }
                else if (state.state.name == "Jump" && jumpClip != null) state.state.motion = jumpClip;
            }
            EditorUtility.SetDirty(controller);
            Debug.Log($"[SetupNewCharacter] 狀態機已指派: Idle={idleClip?.name}, Walk={walkClip?.name}, Run={runClip?.name}, Jump={jumpClip?.name}");
        }
    }

    private static void ReplacePlayerModel(string characterFbx, Material mat)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[SetupNewCharacter] 場景中未找到 'Player' 物件！");
            return;
        }

        // 移除舊的 Meshy_Model
        Transform oldModel = player.transform.Find("Meshy_Model");
        if (oldModel != null)
        {
            Object.DestroyImmediate(oldModel.gameObject);
        }

        // 生成全新模型
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(characterFbx);
        if (prefab == null)
        {
            Debug.LogError("[SetupNewCharacter] 無法載入角色 Prefab: " + characterFbx);
            return;
        }

        GameObject newModel = Object.Instantiate(prefab, player.transform);
        newModel.name = "Meshy_Model";
        // FBX 腳底網格基準即為 0，直接對齊 CharacterController 底部 y=0
        newModel.transform.localPosition = Vector3.zero;
        newModel.transform.localRotation = Quaternion.identity;

        // 套用材質
        if (mat != null)
        {
            foreach (var smr in newModel.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.sharedMaterial = mat;
            }
        }

        // 設定 Animator
        Animator anim = newModel.GetComponent<Animator>();
        if (anim == null) anim = newModel.AddComponent<Animator>();
        anim.applyRootMotion = false;

        Avatar newAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(characterFbx);
        if (newAvatar != null) anim.avatar = newAvatar;

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Character/Meshy_Character/Meshy_Animator.controller");
        if (controller != null) anim.runtimeAnimatorController = controller;

        // 更新 PlayerController 參考與偏移
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.animator = anim;
            pc.modelOffset = Vector3.zero;
        }

        if (player.scene.IsValid() && !EditorApplication.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        }
        Debug.Log("[SetupNewCharacter] 已成功將新角色模型放入場景 Player，且位置置中對齊地面！");
    }
}
