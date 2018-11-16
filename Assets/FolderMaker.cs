using System.IO;
using System.Linq;
#if UNITY_EDITOR
using ADB = UnityEditor.AssetDatabase;
#endif

public static class FolderMaker  {

    /// <summary>
    /// 编辑器下使用，给定一个类的对象，在这个类的同级目录下创建文件夹并返回路径
    /// </summary>
    /// <typeparam name="T">类</typeparam>
    /// <param name="script">对象</param>
    /// <param name="subPath">指定要创建的文件夹的名称</param>
    /// <returns>文件夹的相对路径，相对于Assets文件夹</returns>
    public static string Creat<T>(T script, string subPath) where T : UnityEngine.Object //class  
    {
        string newPath = "";
#if UNITY_EDITOR
            string path = ADB.FindAssets("t:Script")
                .Where(v => Path.GetFileNameWithoutExtension(ADB.GUIDToAssetPath(v)) == script.GetType().Name)
                .Select(id => ADB.GUIDToAssetPath(id))
                .FirstOrDefault()
                .ToString();
        newPath = Path.Combine(Path.GetDirectoryName(path), subPath);
        if (!ADB.IsValidFolder(newPath))
        {
            newPath = ADB.GUIDToAssetPath(ADB.CreateFolder(path, subPath));
        }
#else
        return newPath;
#endif
        return newPath;
    }
}
