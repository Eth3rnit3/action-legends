using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Paname;
public static class BuildScene
{
    public static string Main()
    {
        if(EditorApplication.isPlaying) return "Quitter Play avant de construire";
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Action Legends");root.AddComponent<PanameGame>();
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Paris2026.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Paris2026.unity",true)};
        EditorSettings.defaultBehaviorMode=EditorBehaviorMode.Mode2D;
        PlayerSettings.companyName="Action Legends";PlayerSettings.productName="Action Legends";
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.runInBackground=true;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input=settings.FindProperty("activeInputHandler");
        if(input!=null){input.intValue=2;settings.ApplyModifiedPropertiesWithoutUndo();}
        AssetDatabase.SaveAssets();
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        return "Scène Paris2026 sauvegardée, prototype prêt";
    }
}
