
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AvatarMappingRepair : EditorWindow
{
    private GameObject sceneRoot;
    private GameObject targetFbx;

    private readonly Dictionary<string, Transform> mapping =
        new Dictionary<string, Transform>();

    private Vector2 scroll;

    [MenuItem("工具/角色骨架/場景骨骼綁定")]
    private static void Open()
    {
        GetWindow<AvatarMappingRepair>("場景骨骼綁定");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Humanoid 骨骼對應工具",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "先指定場景中的角色根物件，再逐項拖入骨骼。" +
            "必要骨骼會標示【必要】，可選骨骼會標示【可選】。",
            MessageType.Info);

        EditorGUILayout.Space(6);

        sceneRoot = (GameObject)EditorGUILayout.ObjectField(
            "場景角色根物件",
            sceneRoot,
            typeof(GameObject),
            true);

        targetFbx = (GameObject)EditorGUILayout.ObjectField(
            "目標 FBX 模型",
            targetFbx,
            typeof(GameObject),
            false);

        if (GUILayout.Button("從場景角色自動比對名稱"))
            AutoMatch();

        EditorGUILayout.Space(8);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawSection("軀幹", "Hips", "Spine", "Chest",
            "UpperChest", "Neck", "Head");

        DrawSection("左手臂", "LeftShoulder", "LeftUpperArm",
            "LeftLowerArm", "LeftHand");

        DrawSection("右手臂", "RightShoulder", "RightUpperArm",
            "RightLowerArm", "RightHand");

        DrawSection("左腿", "LeftUpperLeg", "LeftLowerLeg",
            "LeftFoot", "LeftToes");

        DrawSection("右腿", "RightUpperLeg", "RightLowerLeg",
            "RightFoot", "RightToes");

        if (EditorGUILayout.Foldout(
            EditorPrefs.GetBool("AvatarMappingRepair.Fingers", false),
            "手指骨骼（可選）",
            true))
        {
            EditorPrefs.SetBool("AvatarMappingRepair.Fingers", true);

            DrawFingerGroup("Left");
            DrawFingerGroup("Right");
        }
        else
        {
            EditorPrefs.SetBool("AvatarMappingRepair.Fingers", false);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);

        if (GUILayout.Button("檢查骨骼對應"))
            ValidateMapping(true);

        GUI.backgroundColor = new Color(0.65f, 0.9f, 0.7f);

        if (GUILayout.Button("套用至目標 FBX"))
            ApplyMapping();

        GUI.backgroundColor = Color.white;
    }

    private void DrawSection(string title, params string[] bones)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        foreach (string bone in bones)
            DrawBoneField(bone);

        EditorGUILayout.Space(4);
    }

    private void DrawFingerGroup(string side)
    {
        string[] fingers =
        {
            "Thumb", "Index", "Middle", "Ring", "Little"
        };

        foreach (string finger in fingers)
        {
            DrawBoneField(side + finger + "Proximal");
            DrawBoneField(side + finger + "Intermediate");
            DrawBoneField(side + finger + "Distal");
        }
    }

    private void DrawBoneField(string humanBone)
    {
        int index = System.Array.IndexOf(HumanTrait.BoneName, humanBone);

        if (index < 0)
            return;

        bool required = HumanTrait.RequiredBone(index);

        mapping.TryGetValue(humanBone, out Transform current);

        EditorGUILayout.BeginHorizontal();

        string label = ChineseName(humanBone) +
            (required ? "【必要】" : "【可選】");

        EditorGUILayout.LabelField(label, GUILayout.Width(180));

        Transform selected = (Transform)EditorGUILayout.ObjectField(
            current,
            typeof(Transform),
            true);

        if (selected != null && sceneRoot != null &&
            !IsChildOf(selected, sceneRoot.transform))
        {
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                label + "：所選骨骼不在場景角色根物件之下。",
                MessageType.Warning);

            return;
        }

        if (selected == null)
            mapping.Remove(humanBone);
        else
            mapping[humanBone] = selected;

        EditorGUILayout.EndHorizontal();
    }

    private void AutoMatch()
    {
        if (sceneRoot == null)
        {
            EditorUtility.DisplayDialog(
                "缺少場景角色",
                "請先指定場景中的角色根物件。",
                "確定");
            return;
        }

        Transform[] transforms =
            sceneRoot.GetComponentsInChildren<Transform>(true);

        mapping.Clear();

        foreach (string humanBone in HumanTrait.BoneName)
        {
            Transform found = transforms.FirstOrDefault(
                t => t.name == humanBone);

            if (found != null)
                mapping[humanBone] = found;
        }

        Repaint();
    }

    private bool ValidateMapping(bool showDialog)
    {
        List<string> errors = new List<string>();

        if (sceneRoot == null)
            errors.Add("尚未指定場景角色根物件。");

        if (targetFbx == null)
            errors.Add("尚未指定目標 FBX 模型。");

        if (sceneRoot != null && targetFbx != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(targetFbx);

            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.ToLowerInvariant().EndsWith(".fbx"))
            {
                errors.Add("目標模型必須是專案中的 FBX 資產。");
            }
            else
            {
                GameObject imported =
                    AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (imported == null)
                {
                    errors.Add("無法載入目標 FBX 的模型階層。");
                }
                else
                {
                    Transform[] targetTransforms =
                        imported.GetComponentsInChildren<Transform>(true);

                    foreach (var pair in mapping)
                    {
                        Transform sourceBone = pair.Value;

                        if (sourceBone == null)
                            continue;

                        if (!IsChildOf(sourceBone, sceneRoot.transform))
                        {
                            errors.Add(
                                ChineseName(pair.Key) +
                                " 不屬於指定的場景角色。");
                            continue;
                        }

                        int count = targetTransforms.Count(
                            t => t.name == sourceBone.name);

                        if (count == 0)
                        {
                            errors.Add(
                                ChineseName(pair.Key) +
                                "：目標 FBX 找不到同名骨骼「" +
                                sourceBone.name + "」。");
                        }
                        else if (count > 1)
                        {
                            errors.Add(
                                ChineseName(pair.Key) +
                                "：目標 FBX 有多個同名骨骼「" +
                                sourceBone.name +
                                "」，無法安全判定。");
                        }
                    }
                }
            }
        }

        foreach (string humanBone in HumanTrait.BoneName)
        {
            int index = System.Array.IndexOf(
                HumanTrait.BoneName, humanBone);

            if (index >= 0 &&
                HumanTrait.RequiredBone(index) &&
                (!mapping.TryGetValue(humanBone, out Transform t) ||
                 t == null))
            {
                errors.Add("缺少必要骨骼：" + ChineseName(humanBone));
            }
        }

        // 同一個場景 Transform 不應對應到多個 Humanoid 骨骼。
        var duplicates = mapping
            .Where(p => p.Value != null)
            .GroupBy(p => p.Value)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicates)
        {
            errors.Add(
                "同一骨骼被重複指定：" + group.Key.name);
        }

        if (showDialog)
        {
            if (errors.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "檢查結果",
                    "目前對應通過基本檢查。套用後仍須確認 Avatar 是否有效。",
                    "確定");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "檢查未通過",
                    string.Join("\n", errors),
                    "確定");
            }
        }

        return errors.Count == 0;
    }

    private void ApplyMapping()
    {
        if (!ValidateMapping(true))
            return;

        string path = AssetDatabase.GetAssetPath(targetFbx);

        ModelImporter importer =
            AssetImporter.GetAtPath(path) as ModelImporter;

        if (importer == null)
        {
            EditorUtility.DisplayDialog(
                "套用失敗",
                "無法取得 FBX 的 ModelImporter。",
                "確定");
            return;
        }

        GameObject imported =
            AssetDatabase.LoadAssetAtPath<GameObject>(path);

        Transform[] targetTransforms =
            imported.GetComponentsInChildren<Transform>(true);

        HumanDescription description = importer.humanDescription;

        List<HumanBone> humanBones = new List<HumanBone>();

        foreach (string humanBone in HumanTrait.BoneName)
        {
            if (!mapping.TryGetValue(humanBone, out Transform source) ||
                source == null)
                continue;

            Transform[] matches =
                targetTransforms.Where(t => t.name == source.name).ToArray();

            if (matches.Length != 1)
                continue;

            humanBones.Add(new HumanBone
            {
                humanName = humanBone,
                boneName = matches[0].name,
                limit = new HumanLimit
                {
                    useDefaultValues = true
                }
            });
        }

        description.human = humanBones.ToArray();

        Undo.RecordObject(importer, "套用 Humanoid 骨骼對應");

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.humanDescription = description;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "已執行套用",
            "已將骨骼對應寫入 FBX 匯入設定。" +
            "請重新選取 FBX，確認 Rig 頁籤中的 Avatar 是否顯示 Valid。",
            "確定");
    }

    private static bool IsChildOf(Transform child, Transform root)
    {
        return child == root || child.IsChildOf(root);
    }

    private static string ChineseName(string bone)
    {
        string name = bone
            .Replace("Left", "左")
            .Replace("Right", "右");

        string[] replacements =
        {
            "UpperChest", "上胸",
            "Chest", "胸部",
            "Spine", "脊椎",
            "Hips", "骨盆",
            "Neck", "脖子",
            "Head", "頭部",
            "Shoulder", "肩膀",
            "UpperArm", "上臂",
            "LowerArm", "前臂",
            "Hand", "手掌",
            "UpperLeg", "大腿",
            "LowerLeg", "小腿",
            "Foot", "腳",
            "Toes", "腳趾",
            "Thumb", "拇指",
            "Index", "食指",
            "Middle", "中指",
            "Ring", "無名指",
            "Little", "小指",
            "Proximal", "近端",
            "Intermediate", "中端",
            "Distal", "末端"
        };

        for (int i = 0; i < replacements.Length; i += 2)
            name = name.Replace(replacements[i], replacements[i + 1]);

        return name;
    }
}
