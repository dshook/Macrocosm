#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace UnityPackages.UI {

  [CustomEditor (typeof (UIFlexbox))]
  public class UIFlexboxEditor : Editor {

    public override void OnInspectorGUI () {
      var _target = (UIFlexbox) target;
      GUILayout.BeginVertical ("GroupBox");
      this.DrawMessagesGUI (_target);
      this.DrawFlexContainerGUI (_target);
      GUILayout.Label("Default Flex Item", EditorStyles.boldLabel);
      this.DrawFlexItemGUI (_target.defaultFlexItem);
      if (_target.flexItems.Count > 0)
        GUILayout.Label("Flex Items", EditorStyles.boldLabel);
        foreach (var _flexItem in _target.flexItems)
          this.DrawFlexItemGUI (_flexItem);
      if (GUILayout.Button ("Add Flex Item") == true)
        _target.flexItems.Add (new FlexItem ());
      if (GUILayout.Button ("Remove Flex Item") == true &&
        _target.flexItems.Count > 0)
        _target.flexItems.RemoveAt (_target.flexItems.Count - 1);
      if(GUILayout.Button ("Force Update")){
        _target.Draw ();
      }
      GUILayout.EndVertical ();
    }

    private void DrawFlexContainerGUI (UIFlexbox target) {
      GUILayout.Label ("Flex Container", EditorStyles.boldLabel);
      target.flexDirection = (FlexDirection) EditorGUILayout.EnumPopup ("Flex Direction", target.flexDirection);
      target.justifyContent = (JustifyContent) EditorGUILayout.EnumPopup ("Justify Content", target.justifyContent);
      if (target.justifyContent != JustifyContent.Stretch)
        target.itemSize = EditorGUILayout.IntField ("Item Size", target.itemSize);
      target.contentSpacing = (ContentSpacing) EditorGUILayout.EnumPopup ("Content Spacing", target.contentSpacing);
      if (target.contentSpacing != ContentSpacing.None)
        target.spacing = EditorGUILayout.IntField ("Spacing", target.spacing);

      target.includeInactiveChildren = EditorGUILayout.Toggle("Include Inactive Children", target.includeInactiveChildren );
      target.expandContainer = EditorGUILayout.Toggle("Expand Container", target.expandContainer );
    }

    private void DrawFlexItemGUI (FlexItem flexItem) {
      GUILayout.BeginVertical ("GroupBox");
      flexItem.grow = EditorGUILayout.IntSlider ("Grow", flexItem.grow, 1, 100);
      flexItem.alignSelf = (AlignSelf) EditorGUILayout.EnumPopup ("Align Self", flexItem.alignSelf);
      if (flexItem.alignSelf != AlignSelf.Stretch) {
        flexItem.basis = EditorGUILayout.IntField ("Basis", flexItem.basis);
        flexItem.basisType = (BasisType) EditorGUILayout.EnumPopup ("Basis Type", flexItem.basisType);
      }
      GUILayout.EndVertical ();
    }

    private void DrawMessagesGUI (UIFlexbox target) {
      // if (target.flexItems.Count != target.transform.childCount) {
      //   EditorGUILayout.HelpBox ("This Flexbox has an invalid number of children.", MessageType.Warning);
      // }
    }
  }
}
#endif