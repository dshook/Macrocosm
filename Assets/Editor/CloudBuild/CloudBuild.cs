#if UNITY_CLOUD_BUILD
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class CloudBuildHelper : MonoBehaviour
{
  public static void PreExport(UnityEngine.CloudBuild.BuildManifestObject manifest)
  {
    //Set build numbers based off cloud build number
    int buildNumber = manifest.GetValue<int>("buildNumber");

    PlayerSettings.iOS.buildNumber = buildNumber.ToString();
    PlayerSettings.Android.bundleVersionCode = buildNumber;

#if DEMO

  #if UNITY_IOS
    PlayerSettings.iOS.buildNumber = (buildNumber + 700000).ToString();
  #elif UNITY_ANDROID
    PlayerSettings.bundleVersion = PlayerSettings.bundleVersion + "-demo";
  #endif

#endif
  }

  [PostProcessBuild]
  public static void ChangeXcodePlist(BuildTarget buildTarget, string path) {

    if (buildTarget == BuildTarget.iOS) {

      string plistPath = path + "/Info.plist";
      PlistDocument plist = new PlistDocument();
      plist.ReadFromFile(plistPath);

      PlistElementDict rootDict = plist.root;

      Debug.Log(">> Automation, plist ... <<");

      // example of changing a value:
      // rootDict.SetString("CFBundleVersion", "6.6.6");

      // example of adding a boolean key...
      // < key > ITSAppUsesNonExemptEncryption </ key > < false />
      rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);

      File.WriteAllText(plistPath, plist.WriteToString());
    }
  }
}
#endif
