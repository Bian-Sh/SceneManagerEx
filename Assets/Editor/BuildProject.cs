using UnityEditor;
class BuildProject
{
	static void PerformNativeBuild()
	{
		string[] scenes ={"Assets/12.unity","Assets/01.unity","Assets/02.unity","Assets/03.unity","Assets/05.unity"};
		BuildPipeline.BuildPlayer(scenes, "Build/Native/SceneListAutoCheck.exe", BuildTarget.StandaloneWindows, BuildOptions.None);
	}
}
