using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System;
using SceneMgr = UnityEditor.SceneManagement.EditorSceneManager;
using Scene = UnityEngine.SceneManagement.Scene;
using SceneUtility = UnityEngine.SceneManagement.SceneUtility;
using ADB = UnityEditor.AssetDatabase;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace UnityEditor_siki
{
    public class SceneManagerEx : EditorWindow
    {
        #region MenuItem
        [MenuItem("Tools/Scene Manager %M")]
        static void ShowWindow()
        {
            var sceneFinder = GetWindow<SceneManagerEx>();
            sceneFinder.titleContent = new GUIContent("Scene Manager");
            sceneFinder.Show();
        }
        #endregion
        private List<SceneInfo> scenesInProject = new List<SceneInfo>();
        private List<SceneInfo> scenesInSettings = new List<SceneInfo>();
        private int[] sceneInstanceIDs = new int[0];

        private ReorderableList projectList;
        private ReorderableList settingList;

        private Vector2 projectScrollPosition;
        private Vector2 settingScrollPosition;
        int index = -1;

        private void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            //AssetDatabase
            var obj = EditorUtility.InstanceIDToObject(instanceID);       //转换数据类型，obj是否为null 将作为数据分类的依据。
            int sceneCount = SceneManager.sceneCount;
            if (this.sceneInstanceIDs.Length != sceneCount)
            {
                Array.Resize(ref this.sceneInstanceIDs, sceneCount);            //修正数组容器大小，保存 Scene对应的 Instance ID 
            }
            if (null == obj)  //Finger out whether it is a scene or not ,tips : "obj==null " means it is a scene
            {
                if (selectionRect.y == 0)                                                                  //确认首数据的写入点,Debug表明：Rect.y=0,渲染的永远是ScenesBar
                {
                    index = 0;
                }
                else if (index < sceneCount)
                {
                    index++;
                }
                if (this.sceneInstanceIDs[index] != instanceID)                         //一旦 Instance ID 数据发生改变则更新数据
                {
                    this.sceneInstanceIDs[index] = instanceID;
                }

                if (GUI.Button(new Rect(selectionRect.width - 40, selectionRect.y, 20, selectionRect.height), "Ping", GUIStyle.none))
                {
                    var assetObj = ADB.LoadAssetAtPath<SceneAsset>(SceneManager.GetSceneAt(index).path);
                    if (null != assetObj)
                    {
                        EditorGUIUtility.PingObject(assetObj);
                    }
                    else
                    {
                        this.Focus();
                        this.ShowNotification(new GUIContent("Please save the scene first!"));
                    }
                }
            }
            #region For Debug
            if (obj != null)
            {
                //  Debug.LogFormat("{0} - activeInHierarchy:{1}.at {2} ", obj.name, ((GameObject)obj).activeInHierarchy, selectionRect.y);
            }
            else
            {
                Debug.LogFormat("{0} - Secene InHierarchy - InstanceID:{1}.at {2} - {3} ", index, instanceID, selectionRect.y, EditorSceneManager.GetSceneAt(index).path);
            }
            #endregion
        }

        #region default Life cycle function
        private void OnEnable()
        {
            EditorBuildSettings.sceneListChanged += EditorBuildSettings_sceneListChanged;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
            EditorApplication.projectWindowChanged += ProjectWindowChanged;

            this.CreatAssetPath("Config");//这一句测试 跟随本脚本自动生成 Config.json 文件。

            EditorApplication.RepaintHierarchyWindow();//强制要求Hierarchy刷新一次，避免mappedIdInfo数据抓不回来
        }
        private void OnDestroy()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemOnGUI;
            EditorBuildSettings.sceneListChanged -= EditorBuildSettings_sceneListChanged;
            EditorApplication.projectWindowChanged -= ProjectWindowChanged;
        }
        #endregion

        #region Implement Of Several Callback
        private void ProjectWindowChanged()
        {

        }

        /// <summary>
        /// 自动创建 一个 BuildProject.cs 并写入数据
        /// </summary>
        private void EditorBuildSettings_sceneListChanged()
        {
            string[] paths = (from v in EditorBuildSettings.scenes select v.path).ToArray();
            string format = "string[] scenes ={{{0}}};";  // {0} 占位符 如果输出时还需要花括号包裹，则在 {{0}} 的基础上再包裹一层即可，形如： {{{0}}} 。
            string itemInfo = "";

            for (int i = 0; i < paths.Length; i++)
            {
                string item = string.Format("\"{0}\"", paths[i]);
                itemInfo += (i == paths.Length - 1) ? item : (item + ","); //如果不是数组的最后一组数据，后面加个逗号隔开
            }

            itemInfo = string.Format(format, itemInfo);
            string filePath = Application.dataPath + "/Editor/BuildProject.cs";
            string[] folders = Application.dataPath.Split('/');
            string appPath = "Build/Native/" + folders[folders.Length - 2] + ".exe";
            if (File.Exists(filePath))                //If the file exists, for insurance purposes, only modify the collection of scene paths
            {
                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("string[]"))
                    {
                        lines[i] = "\t\t" + itemInfo;
                        break;
                    }
                }
                File.WriteAllLines(filePath, lines);
            }
            else                                       //Otherwise, create a new one, using the projectPath defined above
            {
                using (StreamWriter writer = File.CreateText(filePath))
                {
                    writer.WriteLine("using UnityEditor;\nclass BuildProject\n{{\n\tstatic void PerformNativeBuild()\n\t{{\n\t\t{0}\n\t\tBuildPipeline.BuildPlayer(scenes, \"{1}\", BuildTarget.StandaloneWindows, BuildOptions.None);\n\t}}\n}}",
                        itemInfo,              //BuildList ScenePath collection
                        appPath                //Project packing path and projectName
                        );
                }
            }
            //AssetDatabase.Refresh(); //There is no need to refresh,if you do so,the editor will be stuck for a while as it execute import process. 
        }

        #endregion


        void OnGUI()
        {
            if (projectList == null)
            {
                projectList = new ReorderableList(scenesInProject, typeof(SceneInfo), false, false, false, false);
                projectList.drawHeaderCallback =
                (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Scenes In Project", EditorStyles.boldLabel);
                };
                projectList.drawElementCallback =
                (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var sceneInfo = scenesInProject[index];
                    EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width - 60, rect.height), sceneInfo.name);
                    if (GUI.Button(new Rect(rect.width - 60, rect.y, 20, rect.height), "P", GUIStyle.none))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneInfo.path));
                    }
                    if (GUI.Button(new Rect(rect.width - 40, rect.y, 20, rect.height), "O", GUIStyle.none))
                    {
                        List<SceneInfo> scenes = this.GetScenesInHierarchy();                                            //得到 Hierarchy 中的 已加载的 场景
                        int sceneIndex = scenes.FindIndex(v => v.path == sceneInfo.path);                    //确认当前请求添加的场景 的状态
                        if (sceneIndex == -1)
                        {
                            bool savemodified = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                            if (savemodified) //After the user responds positively (with or without saving), open the scene he specified.
                            {
                                int switchIndex = EditorUtility.DisplayDialogComplex("OpenSceneMode", "Addition:Add scene and keep the loaded scene\nSingle：Will Remove all the loaded scene of hierarchy", "Single", "Cancel", "Addition");
                                switch (switchIndex)
                                {
                                    case 0:
                                        EditorSceneManager.OpenScene(sceneInfo.path);
                                        break;
                                    case 2:
                                        EditorSceneManager.OpenScene(sceneInfo.path, OpenSceneMode.Additive);
                                        break;
                                    case 1:
                                    default:
                                        this.ShowNotification(new GUIContent("Aborting！"));
                                        break;
                                }
                            }
                            else
                            {
                                this.ShowNotification(new GUIContent("Aborting！"));
                            }
                        }
                        else                          //If it is stay in the hierarchy,no matter whether it is load or unload, ping this scene
                        {
                            EditorGUIUtility.PingObject(scenes[sceneIndex].instanceID);
                            this.ShowNotification(new GUIContent("This scene already in the hierarchy！"));
                        }

                    }
                    if (GUI.Button(new Rect(rect.width - 20, rect.y, 20, rect.height), "A", GUIStyle.none))
                    {
                        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(sceneInfo.path);
                        if (sceneIndex == -1)
                        {
                            var tmpSceneIn = EditorBuildSettings.scenes.ToList();
                            tmpSceneIn.Add(new EditorBuildSettingsScene(sceneInfo.path, true));
                            EditorBuildSettings.scenes = tmpSceneIn.ToArray();
                            ADB.Refresh();
                        }
                        else
                        {
                            this.ShowNotification(new GUIContent("This scene already in the buildlist！"));
                        }
                    }
                };
            }
            if (settingList == null)
            {
                settingList = new ReorderableList(scenesInSettings, typeof(SceneInfo), true, false, false, true);
                settingList.onReorderCallback =
                (ReorderableList list) =>
                {
                    EditorBuildSettings.scenes =
                        list.list.Cast<SceneInfo>()
                        .Select(scene =>
                        {
                            var editorScene = new EditorBuildSettingsScene();
                            editorScene.enabled = scene.enabledInSettings;
                            editorScene.path = scene.path;
                            return editorScene;
                        })
                        .ToArray();
                };
                settingList.drawHeaderCallback =
                (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Scenes In Setting", EditorStyles.boldLabel);
                };
                settingList.drawElementCallback =
                (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var sceneInfo = scenesInSettings[index];
                    sceneInfo.enabledInSettings = GUI.Toggle(new Rect(rect.x, rect.y, 14, rect.height), sceneInfo.enabledInSettings, string.Empty);
                    var tmpScenesInSetting = EditorBuildSettings.scenes.ToList();
                    tmpScenesInSetting.Where(scene => scene.path == sceneInfo.path).First().enabled = sceneInfo.enabledInSettings;
                    EditorGUI.LabelField(new Rect(rect.x + 14, rect.y, rect.width - 34, rect.height), sceneInfo.name);
                    EditorGUI.LabelField(new Rect(rect.width - 8, rect.y, 20, rect.height), index.ToString());

                    if (GUI.Button(new Rect(rect.width - 65, rect.y, 20, rect.height), "PH", GUIStyle.none))
                    {
                        int id = this.GetInstanceId(sceneInfo);
                        if (id != -1)
                        {
                            RemoveNotification();
                            EditorGUIUtility.PingObject(id);
                        }
                        else
                        {
                            ShowNotification(new GUIContent("The scene you pinged is not in hierarchy!"));
                        }
                    }
                    if (GUI.Button(new Rect(rect.width - 30, rect.y, 20, rect.height), "PP", GUIStyle.none))
                    {
                        var assetObj = ADB.LoadAssetAtPath<SceneAsset>(sceneInfo.path);
                        if (null != assetObj)
                        {
                            RemoveNotification();
                            EditorGUIUtility.PingObject(assetObj);
                        }
                    }
                };
                settingList.onRemoveCallback = (ReorderableList list) =>
                {
                    if (EditorUtility.DisplayDialog("Attention：", "Really want to remove this scene from the BuildList？", "Yes", "No"))
                    {
                        var tempList = EditorBuildSettings.scenes.ToList();
                        tempList.RemoveAt(list.index);
                        tempList.TrimExcess();
                        EditorBuildSettings.scenes = tempList.ToArray();
                        ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    }
                };
            }
            scenesInProject = GetScenesInProject();
            scenesInSettings = GetScenesInSettings();

            projectList.list = scenesInProject;
            settingList.list = scenesInSettings;

            // settingScrollPosition = EditorGUILayout.BeginScrollView(settingScrollPosition);
            settingList.DoLayoutList();
            //EditorGUILayout.EndScrollView();

            projectScrollPosition = EditorGUILayout.BeginScrollView(projectScrollPosition);
            projectList.DoLayoutList();
            EditorGUILayout.EndScrollView();
        }


        #region SceneInfo

        private class SceneInfo
        {
            public bool enabledInSettings;
            public string name;
            public string path;
            public int instanceID;
        }

        #endregion
        #region Util
        private int GetInstanceId(SceneInfo sceneInfo)
        {
            List<SceneInfo> scenes = GetScenesInHierarchy();

            int index = scenes.FindIndex(v => v.path == sceneInfo.path);
            if (index != -1)
            {
                EditorApplication.RepaintHierarchyWindow();
                return scenes[index].instanceID;
            }
            return -1;
        }

        private List<SceneInfo> GetScenesInHierarchy()
        {
            int count = SceneManager.sceneCount;                        //必须使用SeceneManager 获取该数值，否则未保存的的Scene将不被计数
            Scene[] scenes = new Scene[count];
            for (int i = 0; i < count; i++)
            {
                scenes[i] = EditorSceneManager.GetSceneAt(i);
            }
            List<SceneInfo> result = scenes.Select(v =>
            {
                var sceneInfo = new SceneInfo()
                {
                    instanceID = this.sceneInstanceIDs[Array.IndexOf(scenes, v)],
                    name = v.name,
                    path = v.path,
                    enabledInSettings =
                        EditorBuildSettings.scenes
                        .Where(scene => scene.enabled)
                        .Select(scene => scene.path)
                        .Contains(v.path),
                };
                return sceneInfo;
            }).ToList();
            return result;
        }

        private List<SceneInfo> GetScenesInSettings()
        {
            List<SceneInfo> scenes =
                EditorBuildSettings.scenes
                .Select(scene =>
                {
                    var sceneInfo = new SceneInfo();
                    sceneInfo.enabledInSettings = scene.enabled;
                    sceneInfo.path = scene.path;
                    //sceneInfo.name = Path.GetFileNameWithoutExtension(sceneInfo.path);
                    sceneInfo.name = scene.path.Substring(7).Replace(".unity", ""); //Like BuildList, the display path will be much better.
                    return sceneInfo;
                })
                .ToList();
            return scenes;
        }
        private List<SceneInfo> GetScenesInProject()
        {
            List<SceneInfo> scenes = AssetDatabase.FindAssets("t:Scene")
                .Select(id =>
                {
                    var info = new SceneInfo();
                    info.path = AssetDatabase.GUIDToAssetPath(id);
                    info.name = Path.GetFileNameWithoutExtension(info.path);
                    info.enabledInSettings =
                        EditorBuildSettings.scenes
                        .Where(scene => scene.enabled)
                        .Select(scene => scene.path)
                        .Contains(info.path);
                    return info;
                })
                .ToList();
            //The name of the Scene should show the path to avoid confusion
            scenes.GroupBy(scene => scene.name)
                .Where(group => group.Count() > 1)
                .SelectMany(group => group.ToList())
                .ToList()
                .ForEach(scene => scene.name = scene.path.Substring(7).Replace(".unity", ""));
            return scenes;
        }

        #endregion

    }
    public static class EditorAssetEx
    {
        public static string CreatAssetPath<T>(this T script, string subPath) where T : UnityEngine.Object //class  
        {
            if (null == script) return string.Empty;

            string path = ADB.FindAssets("t:Script")
                .Where(v => Path.GetFileNameWithoutExtension(ADB.GUIDToAssetPath(v)) == script.GetType().Name)
                .Select(id => ADB.GUIDToAssetPath(id))
                .FirstOrDefault()
                .ToString();
            string newPath = Path.Combine(Path.GetDirectoryName(path), subPath);

            if (!ADB.IsValidFolder(newPath))
            {
                newPath = ADB.GUIDToAssetPath(ADB.CreateFolder(path, subPath));
            }
            return newPath;
        }
    }
}
