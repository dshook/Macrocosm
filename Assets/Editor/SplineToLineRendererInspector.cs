using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineToLineRenderer))]
public class SplineToLineRendererInspector : Editor {

  private SplineToLineRenderer splineMesh;

  public override void OnInspectorGUI  () {
    splineMesh = target as SplineToLineRenderer;

    DrawDefaultInspector();

    if (GUILayout.Button("Generate")){
      splineMesh.GenerateMesh();
    }
  }

}