using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(SongTemplate))]
public class SongTemplateInspector : Editor {

  override public void  OnInspectorGUI () {
    SongTemplate template = (SongTemplate)target;

    float totalBeats = 0;
    foreach(var t in template.templates){
      if(t != null){
        totalBeats += t.beatLength;
      }
    }
    GUILayout.Label("Beats: " + totalBeats, EditorStyles.boldLabel);
    GUILayout.Label("Bars: " + Mathf.RoundToInt(totalBeats / 4f), EditorStyles.boldLabel);

    var typesUsed = template.beatTypesUsed;
    if(typesUsed != null){
      GUILayout.Label("Cell Types: " + string.Join(",", typesUsed));
    }

    DrawDefaultInspector();

  }
}