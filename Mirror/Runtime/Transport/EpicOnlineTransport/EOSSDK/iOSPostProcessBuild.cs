#if UNITY_EDITOR&&UNITY_IOS
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;


public static class iOSPostProcessBuild
{
    [PostProcessBuild(1000)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target == BuildTarget.iOS)
        {
            string projectPath = PBXProject.GetPBXProjectPath(path);

            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromFile(projectPath);
            string[] targetGuids = new string[2] { pbxProject.GetUnityMainTargetGuid(), pbxProject.GetUnityFrameworkTargetGuid()};
            foreach(string t in targetGuids)
            {
                pbxProject.SetBuildProperty(t, "ENABLE_BITCODE", "NO");
            }
            
            pbxProject.WriteToFile(projectPath);
        }
    }
}
#endif