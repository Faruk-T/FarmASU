#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FarmASU.Editor
{
    /// <summary>
    /// Utility to configure the newly imported Peasant Man FBX model and animations.
    /// Sets Humanoid rigs, enables looping on Walk/Run/Idle clips, non-looping on Jump,
    /// maps the URP Lit material, and generates a Locomotion & Jump Animator Controller.
    /// </summary>
    public static class CharacterSetupUtility
    {
        private const string CharactersFolder = "Assets/Art/Characters";
        private const string ModelPath = CharactersFolder + "/Peasant Man.fbx";
        private const string IdlePath = CharactersFolder + "/Peasant Man@Happy Idle.fbx";
        private const string WalkPath = CharactersFolder + "/Peasant Man@Dwarf Walk.fbx";
        private const string RunPath = CharactersFolder + "/Peasant Man@Fast Run.fbx";
        private const string JumpPath = CharactersFolder + "/Peasant Man@Jump.fbx";
        private const string ControllerPath = CharactersFolder + "/PlayerAnimator.controller";
        private const string MaterialPath = CharactersFolder + "/M_PeasantMan.mat";

        [MenuItem("FarmASU/Setup Character & Animations")]
        public static void SetupCharacter()
        {
            Debug.Log("[CharacterSetup] Starting Peasant Man Rig, Material & Animation Setup...");

            // 1. Configure Main Character Model as Humanoid
            if (!File.Exists(ModelPath))
            {
                Debug.LogError($"[CharacterSetup] Model not found at: {ModelPath}");
                return;
            }

            ModelImporter modelImporter = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (modelImporter != null)
            {
                modelImporter.animationType = ModelImporterAnimationType.Human;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

                // Remap Peasant_Man_02 material to M_PeasantMan.mat
                Material peasantMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (peasantMat != null)
                {
                    modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Peasant_Man_02"), peasantMat);
                }

                modelImporter.SaveAndReimport();
                Debug.Log("[CharacterSetup] Main character configured as Humanoid with M_PeasantMan material remap.");
            }

            Avatar mainAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(ModelPath);
            if (mainAvatar == null)
            {
                Debug.LogError("[CharacterSetup] Failed to generate Humanoid Avatar for Peasant Man.");
                return;
            }

            // 2. Configure Animation Clips (Humanoid, Copy From Other Avatar, Loop Time)
            ConfigureAnimation(IdlePath, mainAvatar, "HappyIdle", true);
            ConfigureAnimation(WalkPath, mainAvatar, "DwarfWalk", true);
            ConfigureAnimation(RunPath, mainAvatar, "FastRun", true);
            if (File.Exists(JumpPath))
            {
                ConfigureAnimation(JumpPath, mainAvatar, "Jump", false);
            }

            // 3. Extract Animation Clips
            AnimationClip idleClip = LoadAnimationClip(IdlePath);
            AnimationClip walkClip = LoadAnimationClip(WalkPath);
            AnimationClip runClip = LoadAnimationClip(RunPath);
            AnimationClip jumpClip = File.Exists(JumpPath) ? LoadAnimationClip(JumpPath) : null;

            if (idleClip == null || walkClip == null || runClip == null)
            {
                Debug.LogError("[CharacterSetup] Could not load required walk/run/idle animation clips.");
                return;
            }

            // 4. Create Animator Controller with 1D Blend Tree + Jump State
            CreateLocomotionController(idleClip, walkClip, runClip, jumpClip);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CharacterSetup] SUCCESS! Peasant Man character, Humanoid rigs, and Locomotion/Jump Blend Tree successfully configured!");
        }

        [MenuItem("FarmASU/Attach Peasant Man to Player in Scene")]
        public static void AttachCharacterToPlayerInScene()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogError("[CharacterSetup] No GameObject with tag 'Player' found in the open scene!");
                return;
            }

            // Hide old placeholder capsule
            Transform oldVisual = playerObj.transform.Find("Visual");
            if (oldVisual != null)
            {
                oldVisual.gameObject.SetActive(false);
                Debug.Log("[CharacterSetup] Disabled placeholder 'Visual' capsule.");
            }

            // Find or instantiate Peasant Man child
            Transform peasantTransform = playerObj.transform.Find("Peasant Man");
            GameObject peasantInstance;
            if (peasantTransform == null)
            {
                GameObject peasantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                if (peasantPrefab == null)
                {
                    Debug.LogError($"[CharacterSetup] Could not load prefab at {ModelPath}");
                    return;
                }
                peasantInstance = (GameObject)PrefabUtility.InstantiatePrefab(peasantPrefab, playerObj.transform);
                peasantInstance.name = "Peasant Man";
            }
            else
            {
                peasantInstance = peasantTransform.gameObject;
            }

            peasantInstance.transform.localPosition = Vector3.zero;
            peasantInstance.transform.localRotation = Quaternion.identity;
            peasantInstance.transform.localScale = Vector3.one;

            // Configure Animator
            Animator anim = peasantInstance.GetComponent<Animator>();
            if (anim == null)
            {
                anim = peasantInstance.AddComponent<Animator>();
            }

            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            anim.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(ModelPath);
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Configure SkinnedMeshRenderer Material
            Material peasantMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (peasantMat != null)
            {
                SkinnedMeshRenderer smr = peasantInstance.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    smr.sharedMaterial = peasantMat;
                    Debug.Log("[CharacterSetup] Assigned M_PeasantMan material to SkinnedMeshRenderer.");
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(playerObj.scene);
            Debug.Log("[CharacterSetup] Successfully attached Peasant Man to Player with Animator and Material!");
        }

        private static void ConfigureAnimation(string path, Avatar avatar, string clipName, bool loop)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[CharacterSetup] Animation file not found: {path}");
                return;
            }

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;

            // Configure loop time on the clip
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = new[] { new ModelImporterClipAnimation { name = clipName } };
            }

            foreach (var clip in clips)
            {
                clip.name = clipName;
                clip.loopTime = loop;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            Debug.Log($"[CharacterSetup] Configured animation clip at: {path} (Loop: {loop})");
        }

        private static AnimationClip LoadAnimationClip(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var obj in assets)
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static void CreateLocomotionController(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip jump)
        {
            if (File.Exists(ControllerPath))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // 1. Create 1D Blend Tree for Locomotion
            BlendTree blendTree;
            AnimatorState locomotionState = controller.CreateBlendTreeInController("Locomotion", out blendTree, 0);
            rootStateMachine.defaultState = locomotionState;

            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = "Speed";
            blendTree.useAutomaticThresholds = false;

            // Thresholds: 0.0 (Idle), 0.5 (Walk), 1.0 (Run)
            blendTree.AddChild(idle, 0.0f);
            blendTree.AddChild(walk, 0.5f);
            blendTree.AddChild(run, 1.0f);

            // 2. Create Jump State & Transitions
            if (jump != null)
            {
                AnimatorState jumpState = rootStateMachine.AddState("Jump", new Vector3(300, 50, 0));
                jumpState.motion = jump;

                // Locomotion -> Jump transition (on Jump Trigger)
                AnimatorStateTransition toJumpTrigger = locomotionState.AddTransition(jumpState);
                toJumpTrigger.AddCondition(AnimatorConditionMode.If, 0, "Jump");
                toJumpTrigger.duration = 0.08f;
                toJumpTrigger.hasExitTime = false;

                // Locomotion -> Jump transition (falling off ledge / not grounded)
                AnimatorStateTransition toJumpFall = locomotionState.AddTransition(jumpState);
                toJumpFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
                toJumpFall.duration = 0.15f;
                toJumpFall.hasExitTime = false;

                // Jump -> Locomotion transition (when grounded again / landing)
                AnimatorStateTransition toLocomotion = jumpState.AddTransition(locomotionState);
                toLocomotion.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
                toLocomotion.duration = 0.15f;
                toLocomotion.hasExitTime = false;
            }

            Debug.Log($"[CharacterSetup] Created Animator Controller with Locomotion & Jump at: {ControllerPath}");
        }
    }
}
#endif
