using UnityEditor;
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

class FindMissingComponents
{

    static Transform[] transforms;
    static List<UnityEngine.Object> offenders = new List<UnityEngine.Object>();
    // Add menu item to the menu.
    [MenuItem("GameObject/Find Missing Components")]
    static void FindMissingComponentsFunction()
    {
        transforms = GameObject.FindObjectsOfType <Transform>();
        offenders.Clear();
        foreach (var item in transforms)
        {
            Debug.Log("checking: " + item.name);
            checkObject(item.gameObject);
        }
        Selection.objects = offenders.ToArray();
        Debug.Log("Found " + offenders.Count.ToString() + " objects with missing components");
    }

    static void checkObject(GameObject go)
    {
        Component[] comps = go.GetComponents<Component>();
        foreach (var item in comps)
        {
            if (item == null)
            {
                offenders.Add(go as UnityEngine.Object);
                break;
            }
        }
    }
	
    // Validate the menu item.
    // The item will be disabled if this function returns false.
    [MenuItem("GameObject/Select Missing Components", true)]
    static bool ValidateFindMissingComponents()
    {
        return true;
    }
		
}