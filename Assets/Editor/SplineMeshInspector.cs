using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineMesh))]
public class SplineMeshInspector : Editor {

  private SplineMesh splineMesh;

  public override void OnInspectorGUI  () {
    splineMesh = target as SplineMesh;

    DrawDefaultInspector();

    if (GUILayout.Button("Generate")){
      splineMesh.GenerateMesh();
    }
  }

}