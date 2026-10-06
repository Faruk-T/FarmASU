#if UNITY_EDITOR
using System.IO;
using FarmASU.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Unity.Cinemachine;

namespace FarmASU.Editor
{
    /// <summary>
    /// Utility to configure the newly imported Peasant Man FBX model and animations.
    /// Sets Humanoid rigs, enables looping on Walk/Run/Idle clips, non-looping on Jump,
    /// maps the URP Lit material, generates a Locomotion & Jump Animator Controller,
    /// and provides complete one-click player hierarchy & camera repairs.
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
        private const string PickUpPath = CharactersFolder + "/Peasant Man@Picking Up.fbx";

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

            // Optional PickUp Animation
            string foundPickUpPath = null;
            if (File.Exists(PickUpPath))
            {
                foundPickUpPath = PickUpPath;
            }
            else
            {
                string[] files = Directory.GetFiles(CharactersFolder, "*.fbx");
                foreach (var f in files)
                {
                    string fLower = f.ToLower();
                    if (fLower.Contains("pick") || fLower.Contains("gather") || fLower.Contains("lift"))
                    {
                        foundPickUpPath = f.Replace('\\', '/');
                        break;
                    }
                }
            }

            if (foundPickUpPath != null)
            {
                ConfigureAnimation(foundPickUpPath, mainAvatar, "PickUp", false);
            }

            // 3. Extract Animation Clips
            AnimationClip idleClip = LoadAnimationClip(IdlePath);
            AnimationClip walkClip = LoadAnimationClip(WalkPath);
            AnimationClip runClip = LoadAnimationClip(RunPath);
            AnimationClip jumpClip = File.Exists(JumpPath) ? LoadAnimationClip(JumpPath) : null;
            AnimationClip pickUpClip = foundPickUpPath != null ? LoadAnimationClip(foundPickUpPath) : null;

            if (idleClip == null || walkClip == null || runClip == null)
            {
                Debug.LogError("[CharacterSetup] Could not load required walk/run/idle animation clips.");
                return;
            }

            // 4. Create Animator Controller with 1D Blend Tree + Jump & PickUp States
            CreateLocomotionController(idleClip, walkClip, runClip, jumpClip, pickUpClip);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CharacterSetup] SUCCESS! Peasant Man character, Humanoid rigs, and Locomotion/Jump Blend Tree successfully configured!");
        }

        [MenuItem("FarmASU/Attach Peasant Man to Player in Scene")]
        public static void AttachCharacterToPlayerInScene()
        {
            // 1. Locate or create Player GameObject
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                playerObj = GameObject.Find("Player");
            }

            if (playerObj == null)
            {
                Debug.LogWarning("[CharacterSetup] Player GameObject not found by tag or name. Searching for Peasant Man root...");
                GameObject standalonePeasant = GameObject.Find("Peasant Man");
                if (standalonePeasant != null && standalonePeasant.transform.parent == null)
                {
                    playerObj = new GameObject("Player");
                    playerObj.transform.position = standalonePeasant.transform.position;
                    standalonePeasant.transform.SetParent(playerObj.transform);
                }
                else
                {
                    playerObj = new GameObject("Player");
                    playerObj.transform.position = Vector3.zero;
                }
            }

            playerObj.tag = "Player";
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                playerObj.layer = playerLayer;
            }

            // Ensure PlayerController & CharacterController are present
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = playerObj.AddComponent<CharacterController>();
                cc.height = 2.0f;
                cc.radius = 0.4f;
                cc.center = new Vector3(0, 1.0f, 0);
                cc.stepOffset = 0.3f;
            }

            PlayerController pc = playerObj.GetComponent<PlayerController>();
            if (pc == null)
            {
                pc = playerObj.AddComponent<PlayerController>();
            }

            // 2. Hide or remove old placeholder capsule / cylinder
            Transform oldVisual = playerObj.transform.Find("Visual");
            if (oldVisual != null)
            {
                Object.DestroyImmediate(oldVisual.gameObject);
                Debug.Log("[CharacterSetup] Removed old placeholder 'Visual' cylinder/capsule.");
            }

            // 3. Find or instantiate Peasant Man child
            Transform peasantTransform = playerObj.transform.Find("Peasant Man");
            GameObject peasantInstance;
            if (peasantTransform == null)
            {
                // Check if Peasant Man was dropped into scene root
                GameObject rootPeasant = GameObject.Find("Peasant Man");
                if (rootPeasant != null && rootPeasant != playerObj && rootPeasant.transform.parent == null)
                {
                    peasantInstance = rootPeasant;
                    peasantInstance.transform.SetParent(playerObj.transform);
                }
                else
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

            // 4. Ensure CameraTarget exists and is configured
            Transform cameraTargetTransform = playerObj.transform.Find("CameraTarget");
            if (cameraTargetTransform == null)
            {
                GameObject camTargetObj = GameObject.Find("CameraTarget");
                if (camTargetObj != null && camTargetObj.transform.parent == null)
                {
                    cameraTargetTransform = camTargetObj.transform;
                }
                else
                {
                    GameObject newTarget = new GameObject("CameraTarget");
                    newTarget.transform.SetParent(playerObj.transform);
                    newTarget.transform.localPosition = new Vector3(0, 1.4f, 0);
                    cameraTargetTransform = newTarget.transform;
                }
            }

            PlayerCameraTarget cameraTarget = cameraTargetTransform.GetComponent<PlayerCameraTarget>();
            if (cameraTarget == null)
            {
                cameraTarget = cameraTargetTransform.gameObject.AddComponent<PlayerCameraTarget>();
            }

            // 5. Connect Cinemachine Tracking Target
            var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Target.TrackingTarget = cameraTargetTransform;
                vcam.Target.LookAtTarget = null;
                vcam.Target.CustomLookAtTarget = false;
                EditorUtility.SetDirty(vcam);
                Debug.Log("[CharacterSetup] Reconnected CinemachineCamera TrackingTarget to CameraTarget.");
            }

            EditorUtility.SetDirty(playerObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(playerObj.scene);
            Debug.Log("[CharacterSetup] SUCCESS: Full Player Hierarchy & Camera connection repaired successfully!");
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

        private static void CreateLocomotionController(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip jump, AnimationClip pickUp = null)
        {
            if (File.Exists(ControllerPath))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("PickUp", AnimatorControllerParameterType.Trigger);

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

            // 3. Create PickUp State & Transitions (if motion available)
            if (pickUp != null)
            {
                AnimatorState pickUpState = rootStateMachine.AddState("PickUp", new Vector3(300, 180, 0));
                pickUpState.motion = pickUp;

                // Locomotion -> PickUp transition
                AnimatorStateTransition toPickUp = locomotionState.AddTransition(pickUpState);
                toPickUp.AddCondition(AnimatorConditionMode.If, 0, "PickUp");
                toPickUp.duration = 0.10f;
                toPickUp.hasExitTime = false;

                // PickUp -> Locomotion transition (after animation completes)
                AnimatorStateTransition fromPickUp = pickUpState.AddTransition(locomotionState);
                fromPickUp.duration = 0.15f;
                fromPickUp.hasExitTime = true;
                fromPickUp.exitTime = 0.85f;
            }

            Debug.Log($"[CharacterSetup] Created Animator Controller with Locomotion, Jump & PickUp at: {ControllerPath}");
        }
    }
}
#endif
