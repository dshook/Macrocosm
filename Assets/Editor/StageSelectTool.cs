using UnityEngine;
using UnityEditor;
using System;
using PygmyMonkey.ColorPalette;

class StageSelectTool : EditorWindow
{
    [MenuItem("Window/Macrocosm/Stage Select Tool")]
    static void Init()
    {
      var window = GetWindow(typeof(StageSelectTool));
      window.Show();

    }

    void OnGUI()
    {
      GUILayout.Label("Select Stage", EditorStyles.boldLabel);

      for(int i = 1; i <= StageTransitionModel.lastStage; i++){
        if (GUILayout.Button(i.ToString()))
        {
          SelectStage(i);
        }
      }
    }

    void SelectStage(int stage){
      var systems = GameObject.Find("GameSystems");
      if(systems == null){
        return;
      }

      var transition = systems.GetComponent<StageTransitionService>();

      transition.SetGameObjectsActive(stage);

      var mainCam = GameObject.Find("Main Camera").GetComponent<Camera>();
      var palette = ColorPaletteData.Singleton.fromName("Camera Background");
      if(mainCam != null && palette != null){
        //Manually set the camera background color
        mainCam.backgroundColor = palette.getColorAtIndex(stage - 1);
      }
    }

}