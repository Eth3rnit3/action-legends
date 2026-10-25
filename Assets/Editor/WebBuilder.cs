#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ActionLegends.Editor
{
    public static class WebBuilder
    {
        public static void Configure()
        {
            Paname.LevelCatalog.LoadAll();
            PlayerSettings.productName="Action Legends";PlayerSettings.companyName="Action Legends";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.WebGL.template="PROJECT:ActionLegends";
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.threadsSupport=false;
            PlayerSettings.WebGL.initialMemorySize=256;PlayerSettings.WebGL.maximumMemorySize=1024;
            PlayerSettings.WebGL.memoryGrowthMode=WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.nameFilesAsHashes=true;
            PlayerSettings.WebGL.showDiagnostics=false;
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Paris2026.unity");
            var game=UnityEngine.Object.FindAnyObjectByType<Paname.PanameGame>();game.gameObject.name="Action Legends";
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Paris2026.unity",true)};
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Action Legends/Construire la version navigateur")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new BuildFailedException("Arrêter le mode Play avant de compiler.");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.WebGL)throw new BuildFailedException("Choisir Web dans Build Profiles et activer cette plateforme avant la compilation.");
            Configure();string path=Path.GetFullPath("WebBuild");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Paris2026.unity"},target=BuildTarget.WebGL,locationPathName=path,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Compilation web échouée : "+report.summary.result);
            // GameCI valide une méthode de build personnalisée en cherchant ce texte exact dans la sortie Unity.
            Console.WriteLine("Build succeeded!");
        }
        [PostProcessBuild(50)]
        public static void Finish(BuildTarget target,string path)
        {
            if(target!=BuildTarget.WebGL)return;
            File.WriteAllText(Path.Combine(path,".nojekyll"),"");
            File.WriteAllText(Path.Combine(path,"build-info.json"),"{\"game\":\"Action Legends\",\"version\":\""+Application.version+"\",\"unity\":\""+Application.unityVersion+"\"}");
        }
    }
}
#endif
