using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//https://github.com/UnityPatterns/ObjectPool
public sealed class ObjectPool : MonoBehaviour
{
  public enum StartupPoolMode { Awake, Start, CallManually };

  [System.Serializable]
  public class StartupPool
  {
    public int size;
    public GameObject prefab;
  }

  List<GameObject> tempList = new List<GameObject>();

  Dictionary<GameObject, List<GameObject>> pooledObjects = new Dictionary<GameObject, List<GameObject>>();
  Dictionary<GameObject, GameObject> spawnedObjects = new Dictionary<GameObject, GameObject>();

  public StartupPoolMode startupPoolMode;
  public StartupPool[] startupPools;

  bool startupPoolsCreated;

  void Awake()
  {
    if (startupPoolMode == StartupPoolMode.Awake)
      CreateStartupPools();
  }

  void Start()
  {
    if (startupPoolMode == StartupPoolMode.Start)
      CreateStartupPools();
  }

  public void CreateStartupPools()
  {
    if (!startupPoolsCreated)
    {
      startupPoolsCreated = true;
      var pools = startupPools;
      if (pools != null && pools.Length > 0)
        for (int i = 0; i < pools.Length; ++i)
          CreatePool(pools[i].prefab, pools[i].size);
    }
  }

  public void CreatePool<T>(T prefab, int initialPoolSize) where T : Component
  {
    CreatePool(prefab.gameObject, initialPoolSize);
  }
  public void CreatePool(GameObject prefab, int initialPoolSize)
  {
    if (prefab != null && !pooledObjects.ContainsKey(prefab))
    {
      var list = new List<GameObject>();
      pooledObjects.Add(prefab, list);

      if (initialPoolSize > 0)
      {
        bool active = prefab.activeSelf;
        prefab.SetActive(false);
        Transform parent = transform;
        while (list.Count < initialPoolSize)
        {
          var obj = (GameObject)Object.Instantiate(prefab);
          obj.transform.SetParent(parent, false);
          // obj.transform.parent = parent;
          list.Add(obj);
        }
        prefab.SetActive(active);
      }
    }
  }

  public T Spawn<T>(T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
  {
    return Spawn(prefab.gameObject, parent, position, rotation).GetComponent<T>();
  }
  public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
  {
    return Spawn(prefab.gameObject, null, position, rotation).GetComponent<T>();
  }
  public T Spawn<T>(T prefab, Transform parent, Vector3 position) where T : Component
  {
    return Spawn(prefab.gameObject, parent, position, Quaternion.identity).GetComponent<T>();
  }
  public T Spawn<T>(T prefab, Vector3 position) where T : Component
  {
    return Spawn(prefab.gameObject, null, position, Quaternion.identity).GetComponent<T>();
  }
  public T Spawn<T>(T prefab, Transform parent) where T : Component
  {
    return Spawn(prefab.gameObject, parent, Vector3.zero, Quaternion.identity).GetComponent<T>();
  }
  public T Spawn<T>(T prefab) where T : Component
  {
    return Spawn(prefab.gameObject, null, Vector3.zero, Quaternion.identity).GetComponent<T>();
  }
  public GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
  {
    List<GameObject> list;
    Transform trans;
    GameObject obj;
    if (pooledObjects.TryGetValue(prefab, out list))
    {
      obj = null;
      if (list.Count > 0)
      {
        while (obj == null && list.Count > 0)
        {
          obj = list[0];
          list.RemoveAt(0);
        }
        if (obj != null)
        {
          trans = obj.transform;
          trans.SetParent(parent, false);
          // trans.parent = parent;
          trans.localPosition = position;
          trans.localRotation = rotation;
          obj.SetActive(true);
          spawnedObjects.Add(obj, prefab);
          return obj;
        }
      }
      obj = (GameObject)Object.Instantiate(prefab, position, Quaternion.identity, parent);
      trans = obj.transform;
      // trans.parent = parent;
      trans.SetParent(parent, false);
      trans.localPosition = position;
      trans.localRotation = rotation;
      spawnedObjects.Add(obj, prefab);
      return obj;
    }
    else
    {
      obj = (GameObject)Object.Instantiate(prefab, position, Quaternion.identity, parent);
      trans = obj.GetComponent<Transform>();
      // trans.parent = parent;
      trans.SetParent(parent, false);
      trans.localPosition = position;
      trans.localRotation = rotation;
      return obj;
    }
  }
  public GameObject Spawn(GameObject prefab, Transform parent, Vector3 position)
  {
    return Spawn(prefab, parent, position, Quaternion.identity);
  }
  public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
  {
    return Spawn(prefab, null, position, rotation);
  }
  public GameObject Spawn(GameObject prefab, Transform parent)
  {
    return Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
  }
  public GameObject Spawn(GameObject prefab, Vector3 position)
  {
    return Spawn(prefab, null, position, Quaternion.identity);
  }
  public GameObject Spawn(GameObject prefab)
  {
    return Spawn(prefab, null, Vector3.zero, Quaternion.identity);
  }

  public void Recycle<T>(T obj) where T : Component
  {
    Recycle(obj.gameObject);
  }
  public void Recycle(GameObject obj)
  {
    GameObject prefab;
    if (spawnedObjects.TryGetValue(obj, out prefab))
      Recycle(obj, prefab);
    else
      Object.Destroy(obj);
  }
  public void Recycle(GameObject obj, float delay)
  {
    if(delay <= 0f){
      Recycle(obj);
      return;
    }

    StartCoroutine(DelayedRecycle(obj, delay));
  }
  IEnumerator DelayedRecycle(GameObject obj, float delay){
    yield return new WaitForSeconds(delay);

    Recycle(obj);
  }

  void Recycle(GameObject obj, GameObject prefab)
  {
    if(obj == null){
      Logger.LogWarning("Trying to recycle null GameObject");
      return;
    }
    pooledObjects[prefab].Add(obj);
    spawnedObjects.Remove(obj);
    // obj.transform.parent = transform;
    obj.transform.SetParent(transform, false);
    obj.SetActive(false);
  }

  public void RecycleAll<T>(T prefab) where T : Component
  {
    RecycleAll(prefab.gameObject);
  }
  public void RecycleAll(GameObject prefab)
  {
    foreach (var item in spawnedObjects)
      if (item.Value == prefab)
        tempList.Add(item.Key);
    for (int i = 0; i < tempList.Count; ++i)
      Recycle(tempList[i]);
    tempList.Clear();
  }
  public void RecycleAll()
  {
    tempList.AddRange(spawnedObjects.Keys);
    for (int i = 0; i < tempList.Count; ++i)
      Recycle(tempList[i]);
    tempList.Clear();
  }

  public bool IsSpawned(GameObject obj)
  {
    return spawnedObjects.ContainsKey(obj);
  }

  public int CountPooled<T>(T prefab) where T : Component
  {
    return CountPooled(prefab.gameObject);
  }
  public int CountPooled(GameObject prefab)
  {
    List<GameObject> list;
    if (pooledObjects.TryGetValue(prefab, out list))
      return list.Count;
    return 0;
  }

  public int CountSpawned<T>(T prefab) where T : Component
  {
    return CountSpawned(prefab.gameObject);
  }
  public int CountSpawned(GameObject prefab)
  {
    int count = 0 ;
    foreach (var instancePrefab in spawnedObjects.Values)
      if (prefab == instancePrefab)
        ++count;
    return count;
  }

  public int CountAllPooled()
  {
    int count = 0;
    foreach (var list in pooledObjects.Values)
      count += list.Count;
    return count;
  }

  public List<GameObject> GetPooled(GameObject prefab, List<GameObject> list, bool appendList)
  {
    if (list == null)
      list = new List<GameObject>();
    if (!appendList)
      list.Clear();
    List<GameObject> pooled;
    if (pooledObjects.TryGetValue(prefab, out pooled))
      list.AddRange(pooled);
    return list;
  }
  public List<T> GetPooled<T>(T prefab, List<T> list, bool appendList) where T : Component
  {
    if (list == null)
      list = new List<T>();
    if (!appendList)
      list.Clear();
    List<GameObject> pooled;
    if (pooledObjects.TryGetValue(prefab.gameObject, out pooled))
      for (int i = 0; i < pooled.Count; ++i)
        list.Add(pooled[i].GetComponent<T>());
    return list;
  }

  public List<GameObject> GetSpawned(GameObject prefab, List<GameObject> list, bool appendList)
  {
    if (list == null)
      list = new List<GameObject>();
    if (!appendList)
      list.Clear();
    foreach (var item in spawnedObjects)
      if (item.Value == prefab)
        list.Add(item.Key);
    return list;
  }
  public List<T> GetSpawned<T>(T prefab, List<T> list, bool appendList) where T : Component
  {
    if (list == null)
      list = new List<T>();
    if (!appendList)
      list.Clear();
    var prefabObj = prefab.gameObject;
    foreach (var item in spawnedObjects)
      if (item.Value == prefabObj)
        list.Add(item.Key.GetComponent<T>());
    return list;
  }

  public void DestroyPooled(GameObject prefab)
  {
    List<GameObject> pooled;
    if (pooledObjects.TryGetValue(prefab, out pooled))
    {
      for (int i = 0; i < pooled.Count; ++i)
        GameObject.Destroy(pooled[i]);
      pooled.Clear();
    }
  }
  public void DestroyPooled<T>(T prefab) where T : Component
  {
    DestroyPooled(prefab.gameObject);
  }

  public void DestroyAll(GameObject prefab)
  {
    RecycleAll(prefab);
    DestroyPooled(prefab);
  }
  public void DestroyAll<T>(T prefab) where T : Component
  {
    DestroyAll(prefab.gameObject);
  }
}

/*
public class ObjectPoolExtensions
{
  public void CreatePool<T>(this T prefab) where T : Component
  {
    ObjectPool.CreatePool(prefab, 0);
  }
  public void CreatePool<T>(this T prefab, int initialPoolSize) where T : Component
  {
    ObjectPool.CreatePool(prefab, initialPoolSize);
  }
  public void CreatePool(this GameObject prefab)
  {
    ObjectPool.CreatePool(prefab, 0);
  }
  public void CreatePool(this GameObject prefab, int initialPoolSize)
  {
    ObjectPool.CreatePool(prefab, initialPoolSize);
  }

  public T Spawn<T>(this T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
  {
    return ObjectPool.Spawn(prefab, parent, position, rotation);
  }
  public T Spawn<T>(this T prefab, Vector3 position, Quaternion rotation) where T : Component
  {
    return ObjectPool.Spawn(prefab, null, position, rotation);
  }
  public T Spawn<T>(this T prefab, Transform parent, Vector3 position) where T : Component
  {
    return ObjectPool.Spawn(prefab, parent, position, Quaternion.identity);
  }
  public T Spawn<T>(this T prefab, Vector3 position) where T : Component
  {
    return ObjectPool.Spawn(prefab, null, position, Quaternion.identity);
  }
  public T Spawn<T>(this T prefab, Transform parent) where T : Component
  {
    return ObjectPool.Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
  }
  public T Spawn<T>(this T prefab) where T : Component
  {
    return ObjectPool.Spawn(prefab, null, Vector3.zero, Quaternion.identity);
  }
  public GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
  {
    return ObjectPool.Spawn(prefab, parent, position, rotation);
  }
  public GameObject Spawn(this GameObject prefab, Vector3 position, Quaternion rotation)
  {
    return ObjectPool.Spawn(prefab, null, position, rotation);
  }
  public GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position)
  {
    return ObjectPool.Spawn(prefab, parent, position, Quaternion.identity);
  }
  public GameObject Spawn(this GameObject prefab, Vector3 position)
  {
    return ObjectPool.Spawn(prefab, null, position, Quaternion.identity);
  }
  public GameObject Spawn(this GameObject prefab, Transform parent)
  {
    return ObjectPool.Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
  }
  public GameObject Spawn(this GameObject prefab)
  {
    return ObjectPool.Spawn(prefab, null, Vector3.zero, Quaternion.identity);
  }

  public void Recycle<T>(this T obj) where T : Component
  {
    ObjectPool.Recycle(obj);
  }
  public void Recycle(this GameObject obj)
  {
    ObjectPool.Recycle(obj);
  }

  public void RecycleAll<T>(this T prefab) where T : Component
  {
    ObjectPool.RecycleAll(prefab);
  }
  public void RecycleAll(this GameObject prefab)
  {
    ObjectPool.RecycleAll(prefab);
  }

  public int CountPooled<T>(this T prefab) where T : Component
  {
    return ObjectPool.CountPooled(prefab);
  }
  public int CountPooled(this GameObject prefab)
  {
    return ObjectPool.CountPooled(prefab);
  }

  public int CountSpawned<T>(this T prefab) where T : Component
  {
    return ObjectPool.CountSpawned(prefab);
  }
  public int CountSpawned(this GameObject prefab)
  {
    return ObjectPool.CountSpawned(prefab);
  }

  public List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list, bool appendList)
  {
    return ObjectPool.GetSpawned(prefab, list, appendList);
  }
  public List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list)
  {
    return ObjectPool.GetSpawned(prefab, list, false);
  }
  public List<GameObject> GetSpawned(this GameObject prefab)
  {
    return ObjectPool.GetSpawned(prefab, null, false);
  }
  public List<T> GetSpawned<T>(this T prefab, List<T> list, bool appendList) where T : Component
  {
    return ObjectPool.GetSpawned(prefab, list, appendList);
  }
  public List<T> GetSpawned<T>(this T prefab, List<T> list) where T : Component
  {
    return ObjectPool.GetSpawned(prefab, list, false);
  }
  public List<T> GetSpawned<T>(this T prefab) where T : Component
  {
    return ObjectPool.GetSpawned(prefab, null, false);
  }

  public List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list, bool appendList)
  {
    return ObjectPool.GetPooled(prefab, list, appendList);
  }
  public List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list)
  {
    return ObjectPool.GetPooled(prefab, list, false);
  }
  public List<GameObject> GetPooled(this GameObject prefab)
  {
    return ObjectPool.GetPooled(prefab, null, false);
  }
  public List<T> GetPooled<T>(this T prefab, List<T> list, bool appendList) where T : Component
  {
    return ObjectPool.GetPooled(prefab, list, appendList);
  }
  public List<T> GetPooled<T>(this T prefab, List<T> list) where T : Component
  {
    return ObjectPool.GetPooled(prefab, list, false);
  }
  public List<T> GetPooled<T>(this T prefab) where T : Component
  {
    return ObjectPool.GetPooled(prefab, null, false);
  }

  public void DestroyPooled(this GameObject prefab)
  {
    ObjectPool.DestroyPooled(prefab);
  }
  public void DestroyPooled<T>(this T prefab) where T : Component
  {
    ObjectPool.DestroyPooled(prefab.gameObject);
  }

  public void DestroyAll(this GameObject prefab)
  {
    ObjectPool.DestroyAll(prefab);
  }
  public void DestroyAll<T>(this T prefab) where T : Component
  {
    ObjectPool.DestroyAll(prefab.gameObject);
  }
}

*/