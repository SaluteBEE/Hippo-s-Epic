using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ReverseAnimationGenerator
{
    private const string AnimRoot = "Assets/Art/ani/ui/map";
    private const string ReversePrefix = "rev_";

    [MenuItem("Tools/Animation/生成反向动画")]
    public static void Generate()
    {
        var animGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimRoot });
        var controllerGuids = AssetDatabase.FindAssets("t:AnimatorController", new[] { AnimRoot });

        var animPaths = animGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !Path.GetFileNameWithoutExtension(p).StartsWith(ReversePrefix))
            .OrderBy(p => p)
            .ToList();

        int created = 0;
        int skipped = 0;

        foreach (var animPath in animPaths)
        {
            string animDir = Path.GetDirectoryName(animPath);
            string animName = Path.GetFileNameWithoutExtension(animPath);
            string reverseName = ReversePrefix + animName;
            string reversePath = Path.Combine(animDir, reverseName + ".anim").Replace('\\', '/');

            var sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
            if (sourceClip == null) continue;

            AnimationClip reverseClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(reversePath);

            if (reverseClip == null)
            {
                reverseClip = CreateReverseClip(sourceClip, reverseName);
                AssetDatabase.CreateAsset(reverseClip, reversePath);
                created++;
                Debug.Log($"[ReverseAnim] 生成反向动画: {reversePath}");
            }
            else
            {
                skipped++;
            }

            foreach (var ctrlGuid in controllerGuids)
            {
                string ctrlPath = AssetDatabase.GUIDToAssetPath(ctrlGuid);
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
                if (controller == null) continue;

                if (ContainsMotion(controller, sourceClip))
                {
                    AddReverseState(controller, reverseClip, reverseName);
                }
            }

            AssignReverseClipToMapRoots(sourceClip, reverseClip);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ReverseAnim] 完成: 生成 {created} 个, 跳过 {skipped} 个(已存在)");
    }

    private static void AssignReverseClipToMapRoots(AnimationClip forwardClip, AnimationClip reverseClip)
    {
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (var guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!prefabPath.Contains("MapRoot")) continue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            var mapRoot = prefab.GetComponent<MapRoot>();
            if (mapRoot == null) continue;

            bool dirty = false;
            var so = new SerializedObject(mapRoot);
            var transitionsProp = so.FindProperty("transitions");

            for (int i = 0; i < transitionsProp.arraySize; i++)
            {
                var transProp = transitionsProp.GetArrayElementAtIndex(i);
                var forwardProp = transProp.FindPropertyRelative("forwardClip");
                var reverseProp = transProp.FindPropertyRelative("reverseClip");

                if (forwardProp.objectReferenceValue == forwardClip && reverseProp.objectReferenceValue == null)
                {
                    reverseProp.objectReferenceValue = reverseClip;
                    dirty = true;
                }
            }

            if (dirty)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(mapRoot);
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"[ReverseAnim] 已绑定反向动画到: {prefabPath}");
            }
        }
    }

    private static AnimationClip CreateReverseClip(AnimationClip source, string name)
    {
        var clip = new AnimationClip
        {
            name = name,
            frameRate = source.frameRate,
            wrapMode = source.wrapMode,
            legacy = source.legacy
        };

        float duration = source.length;

        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            var curve = AnimationUtility.GetEditorCurve(source, binding);
            var reversed = ReverseCurve(curve, duration);
            AnimationUtility.SetEditorCurve(clip, binding, reversed);
        }

        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
        {
            var curve = AnimationUtility.GetObjectReferenceCurve(source, binding);
            var reversed = ReverseObjectReferenceCurve(curve, duration);
            AnimationUtility.SetObjectReferenceCurve(clip, binding, reversed);
        }

        var settings = AnimationUtility.GetAnimationClipSettings(source);
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    private static AnimationCurve ReverseCurve(AnimationCurve source, float duration)
    {
        var keys = new List<Keyframe>();
        foreach (var key in source.keys)
        {
            float newTime = duration - key.time;
            float newInSlope = -key.outTangent;
            float newOutSlope = -key.inTangent;
            var newKey = new Keyframe(newTime, key.value, newInSlope, newOutSlope);
            newKey.tangentMode = key.tangentMode;
            newKey.weightedMode = key.weightedMode;
            newKey.inWeight = key.outWeight;
            newKey.outWeight = key.inWeight;
            keys.Add(newKey);
        }

        keys.Sort((a, b) => a.time.CompareTo(b.time));
        return new AnimationCurve(keys.ToArray());
    }

    private static ObjectReferenceKeyframe[] ReverseObjectReferenceCurve(ObjectReferenceKeyframe[] source, float duration)
    {
        var keys = new List<ObjectReferenceKeyframe>();
        foreach (var key in source)
        {
            keys.Add(new ObjectReferenceKeyframe
            {
                time = duration - key.time,
                value = key.value
            });
        }

        keys.Sort((a, b) => a.time.CompareTo(b.time));
        return keys.ToArray();
    }

    private static bool ContainsMotion(AnimatorController controller, AnimationClip clip)
    {
        foreach (var layer in controller.layers)
        {
            if (ContainsMotionInStateMachine(layer.stateMachine, clip))
                return true;
        }
        return false;
    }

    private static bool ContainsMotionInStateMachine(AnimatorStateMachine sm, AnimationClip clip)
    {
        foreach (var state in sm.states)
        {
            if (state.state.motion == clip)
                return true;
        }

        foreach (var childSm in sm.stateMachines)
        {
            if (ContainsMotionInStateMachine(childSm.stateMachine, clip))
                return true;
        }

        return false;
    }

    private static void AddReverseState(AnimatorController controller, AnimationClip clip, string stateName)
    {
        var layer = controller.layers[0];
        var sm = layer.stateMachine;

        foreach (var state in sm.states)
        {
            if (state.state.name == stateName)
                return;
        }

        var newState = sm.AddState(stateName, new Vector3(260, sm.states.Length * 60 + 30, 0));
        newState.motion = clip;
    }
}
