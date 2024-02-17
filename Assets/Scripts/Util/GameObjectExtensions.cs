using UnityEngine;

public static class GameObjectExtensions
{
  public static void DestroyChildren(this Transform root, bool immediate = false)
  {
    int childCount = root.childCount;
    for (int i = childCount - 1; i >= 0; i--)
    {
      if (immediate)
      {
        GameObject.DestroyImmediate(root.GetChild(i).gameObject);
      }
      else
      {
        GameObject.Destroy(root.GetChild(i).gameObject);
      }
    }
  }

  public static void ActivateChildren(this Transform root, bool newActiveStatus)
  {
    for(int c = 0; c < root.childCount; c++){
      root.GetChild(c).gameObject.SetActive(newActiveStatus);
    }
  }

  public static Transform FindInChildren(this Transform self, string name) {
    int count = self.childCount;
    for(int i = 0; i < count; i++) {
      Transform child = self.GetChild(i);
      if(child.name == name) return child;
      Transform subChild = child.FindInChildren(name);
      if(subChild != null) return subChild;
    }
    return null;
  }

  public static GameObject FindInChildren(this GameObject self, string name) {
    Transform transform = self.transform;
    Transform child = transform.FindInChildren(name);
    return child != null ? child.gameObject : null;
  }
}

