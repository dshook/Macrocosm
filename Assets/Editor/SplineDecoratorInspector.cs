using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineDecorator))]
public class SplineDecoratorInspector : Editor {

  private SplineDecorator decorator;
  string decorateCount;

  public override void OnInspectorGUI () {
    decorator = target as SplineDecorator;

    DrawDefaultInspector();

    GUILayout.Label("Decorate Count");
    decorateCount = GUILayout.TextField(decorateCount);

    if(GUILayout.Button("Decorate")) {
      int count = 0;
      int.TryParse(decorateCount, out count);
      decorator.SetFrequency(count);
    }

    if(GUILayout.Button("Reposition Children")) {
      decorator.RepositionChildren();
    }
  }

}