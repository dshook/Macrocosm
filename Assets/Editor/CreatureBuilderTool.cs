using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

class CreatureBuilderTool : EditorWindow
{

    [MenuItem("Window/Macrocosm/Creature Builder Tool")]
    static void Init()
    {

      var window = GetWindow(typeof(CreatureBuilderTool));
      window.Show();
    }

    Transform creatureHolder;
    GameObject creaturePrefab;
    public List<CreatureModifierId> creatureMods = new List<CreatureModifierId>();

    ResourceLoaderService loader;
    GameObject createdCreatureGO;
    Creature newCreature;

    void OnGUI()
    {
      if(loader == null){
        loader = new ResourceLoaderService();
      }

      GUILayout.Label("Creature Builder", EditorStyles.largeLabel);

      GUILayout.Space(10f);

      GUILayout.Label("Creature Holder");
      creatureHolder = EditorGUILayout.ObjectField(creatureHolder, typeof(Transform), true) as Transform;
      GUILayout.Label("Creature Prefab");
      creaturePrefab = EditorGUILayout.ObjectField(creaturePrefab, typeof(GameObject), true) as GameObject;

      GUILayout.Space(5f);
      GUILayout.Label("Mods", EditorStyles.largeLabel);
      GUILayout.Space(5f);

      // "target" can be any class derrived from ScriptableObject
      // (could be EditorWindow, MonoBehaviour, etc)
      ScriptableObject target = this;
      SerializedObject so = new SerializedObject(target);
      SerializedProperty stringsProperty = so.FindProperty("creatureMods");

      EditorGUILayout.PropertyField(stringsProperty, true); // True means show children

      so.ApplyModifiedProperties(); // Remember to apply modified properties

      // foreach(CreatureModifierType modtype in Enum.GetValues(typeof(CreatureModifierType))){
      //   GUILayout.Label(modtype.ToString());
      // }

      GUILayout.Space(5f);
      if(GUILayout.Button("Create")){
        Create();
      }
    }

    void Create(){
      if(!CheckModsValidity()){ return; }

      if(creatureHolder == null){
        Debug.LogWarning("Set creature holder");
        return;
      }
      if(creaturePrefab == null){
        Debug.LogWarning("Set creature prefab");
        return;
      }

      if(createdCreatureGO != null){
        DestroyImmediate(createdCreatureGO);
        createdCreatureGO = null;
        newCreature = null;
      }

      createdCreatureGO = GameObject.Instantiate(creaturePrefab, Vector3.zero, Quaternion.identity);
      createdCreatureGO.transform.SetParent(creatureHolder, false);

      newCreature = createdCreatureGO.GetComponent<Creature>();
      newCreature.loader = loader;
      newCreature.data = new CreatureData();
      newCreature.data.Reset();

      newCreature.data.modifiers.Clear();
      newCreature.data.modifiers.AddRange(creatureMods);

      newCreature.Reset();
      newCreature.UpdateAnimation();

      Debug.Log("Created!");
    }

    bool CheckModsValidity(){
      foreach(var modId in creatureMods){
        var mod = CreatureModifier.allModifiers[modId];

        if(!mod.repeatable){
          if(creatureMods.Count(m => m == modId) > 1){
            Debug.LogWarning("Duplicate mod: " + modId);
            return false;
          }
        }

        if(mod.excludeMods != null){
          var excludedBy = creatureMods.FirstOrDefault(m => mod.excludeMods.Contains(m));
          if(excludedBy != CreatureModifierId.None){
            Debug.LogWarning(string.Format("{0} excluded by {1}", modId, excludedBy));
            return false;
          }
        }

        if(mod.replaceMods != null){
          var replacedBy = creatureMods.FirstOrDefault(m => mod.replaceMods.Contains(m));
          if(replacedBy != CreatureModifierId.None){
            Debug.LogWarning(string.Format("{0} replaces {1}", modId, replacedBy));
            return false;
          }
        }
        //check prereq mods are fulfilled (for mods that aren't replaced)
        if(mod.prereqMods != null){
          foreach(var prereq in mod.prereqMods){
            if(mod.replaceMods != null && mod.replaceMods.Contains(prereq)){
              continue;
            }
            if(!creatureMods.Contains(prereq)){
              Debug.LogWarning(string.Format("{0} needs prereq {1}", modId, prereq));
              return false;
            }
          }
        }
      }

      if(!creatureMods.Any(m =>
        m == CreatureModifierId.Omnivore ||
        m == CreatureModifierId.Herbivore ||
        m == CreatureModifierId.Carnivore )
      ){
        Debug.LogWarning("Creature must have an Omnivore/Herbivore/Carnivore mod");
        return false;
      }

      return true;
    }

}