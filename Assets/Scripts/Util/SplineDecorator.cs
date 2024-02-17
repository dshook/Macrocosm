using UnityEngine;

public enum SplineDecoratorPositioningStrat{
    Distribute, //Use the frequency and spread the items over the length of the spline
    AlignToStart //Use the itemSize and space as many as can fit
}

public class SplineDecorator : MonoBehaviour {

  public bool initOnAwake = true;
  public BezierSpline spline;

  public int frequency;

  public bool lookForward;

  public Transform[] items;

  public bool addWalker;
  public SplineWalkerMode mode;

  [Range(0, 1f)]
  public float scaleMargin = 0.1f;
  public bool scaleInOut = false;

  public SplineDecoratorPositioningStrat positioningStrat = SplineDecoratorPositioningStrat.Distribute;

  public float itemSize = 0.0f;

  public ObjectPool objectPool;

  private void Awake () {
    if(initOnAwake){
      Init();
    }
  }

  public void Init () {
    if (frequency <= 0 || items == null || items.Length == 0) {
      return;
    }
    splineDistances = null;

    var totalItems = GetTotalNumberOfItems();
    for (int p = 0; p < totalItems; p++) {
      Transform item = Spawn(items[p % items.Length]);
      var splinePct = GetSplinePctForItem(p);
      Vector3 position = spline.GetPoint(splinePct);
      item.transform.position = position;
      item.transform.parent = transform;
      if (lookForward) {
        var direction = (Vector2)spline.GetDirection(splinePct);
        item.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
      }

      if (addWalker)
      {
        var walker = item.gameObject.AddComponent<SplineWalker>();
        walker.lookForward = lookForward;
        walker.mode = mode;
        walker.spline = spline;
        walker.progress = splinePct;
        walker.scaleInOut = scaleInOut;
        walker.scaleMargin = scaleMargin;
      }
    }
  }

  private int GetTotalNumberOfItems(){
    if(positioningStrat == SplineDecoratorPositioningStrat.Distribute){
      return frequency * items.Length;
    }else if(positioningStrat == SplineDecoratorPositioningStrat.AlignToStart){
      return Mathf.FloorToInt(splineLength / itemSize) + 1; //add one for the start & end overlap
    }

    return 0;
  }

  private float calcStepSize(float freq, int numItems)
  {
    float stepSize = (freq - 1) * numItems;
    stepSize = 1f / stepSize;
    return stepSize;
  }

  float _splineLength = -1f;
  float splineLength{
    get{
      if(_splineLength < 0f){
        _splineLength = spline.Length(15);
      }
      return _splineLength;
    }
  }

  //Stores the total accumulated distances iterating over the spline so we can place points equidistant
  //Regardless of where the control points are.  Right now only gets cleared in init, but really depends
  //On the spline length
  private float[] splineDistances = null;

  private float GetSplinePctForItem(int itemIndex){
    if(positioningStrat == SplineDecoratorPositioningStrat.Distribute){
      float stepSize = calcStepSize(frequency, items.Length);
      return itemIndex * stepSize;
    }else if(positioningStrat == SplineDecoratorPositioningStrat.AlignToStart){
      if(itemSize <= 0){
        Debug.LogWarning("Set Item Size for Align To Start");
        return 0;
      }
      if(itemIndex == 0){
        return 0;
      }
      // var itemWidthPct = itemSize;
      // return (itemIndex * itemWidthPct) / splineLength;
      return GetSplinePctFromDistance(itemIndex * itemSize);
    }

    return 0;
  }

  int NumSplineSamplePoints(int totalItems){
    return totalItems * 4;
  }

  void CalculateSplineDistances(int totalItems){
    var samplePoints = NumSplineSamplePoints(totalItems);
    splineDistances = new float[samplePoints];
    splineDistances[0] = 0;

    for(var i = 1; i < samplePoints; i++){
      splineDistances[i] = Vector2.Distance(spline.GetPoint((float)i / samplePoints), spline.GetPoint((float)(i - 1) / samplePoints) );
    }
  }

  float GetSplinePctFromDistance(float worldDistAlongSpline){
    //iterate the spline distances till we're between the two that contain the distance we're lookign for, then lerp between them
    var totalItems = GetTotalNumberOfItems();
    if(splineDistances == null){
      CalculateSplineDistances(totalItems);
    }

    float accumulatedDist = 0f;
    if(worldDistAlongSpline >= splineLength){ return 1f; }
    if(worldDistAlongSpline <= 0){ return 0; }

    var samplePoints = NumSplineSamplePoints(totalItems);

    for(var i = 0; i < splineDistances.Length; i++){
      accumulatedDist += splineDistances[i];

      if(accumulatedDist >= worldDistAlongSpline){
        //lerp between this point and the previous one
        var prevDist = accumulatedDist - splineDistances[i];
        var pctLerp = (worldDistAlongSpline - prevDist) / (accumulatedDist - prevDist);
        return Mathf.Lerp((float)(i - 1) / samplePoints, (float)i / samplePoints, pctLerp);
      }
    }

    return 1f;
  }

  public void SetFrequency(int newFrequency)
  {
    if(positioningStrat != SplineDecoratorPositioningStrat.Distribute) return;
    if(newFrequency == frequency) return;

    if (newFrequency > frequency)
    {
      for (int f = 0; f < newFrequency - frequency; f++) {
        for (int i = 0; i < items.Length; i++) {
          Transform item = Spawn(items[i]);
          item.transform.parent = transform;

          if (addWalker)
          {
            var walker = item.gameObject.AddComponent<SplineWalker>();
            walker.lookForward = lookForward;
            walker.mode = mode;
            walker.spline = spline;
            walker.scaleInOut = scaleInOut;
            walker.scaleMargin = scaleMargin;
          }
        }
      }
    }
    else
    {
      int curChildCount = transform.childCount;
      for (int i = curChildCount - 1; i >= newFrequency * items.Length; i--)
      {
        Remove(transform.GetChild(i).gameObject);
      }
    }

    frequency = newFrequency;

    RepositionChildren();
  }

  public void RepositionChildren(){
    //update everyone's position
    int childCount = transform.childCount;
    for (int i = 0; i < childCount; i++)
    {
      var child = transform.GetChild(i).gameObject;
      var splinePct = GetSplinePctForItem(i);

      Vector3 position = spline.GetLocalPoint(splinePct);
      child.transform.localPosition = position;
      child.transform.localScale = Vector3.one;
      if (lookForward) {
        var direction = (Vector2)spline.GetDirection(splinePct);
        child.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
      }
      var walker = child.GetComponent<SplineWalker>();
      if (walker != null)
      {
        walker.progress = splinePct;
      }
    }
  }

  Transform Spawn(Transform original){
    if(objectPool != null){
      return objectPool.Spawn(original);
    }else{
      return Instantiate(original);
    }
  }

  void Remove(GameObject go){
    if(objectPool != null){
      objectPool.Recycle(go);
    }else{
      GameObject.DestroyImmediate(go);
    }
  }
}