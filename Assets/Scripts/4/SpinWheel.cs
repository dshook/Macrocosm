using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using strange.extensions.mediation.impl;

public class SpinWheel : View
{
  public SpinWheelOption[] options;
  public float speed = 100f;
  public float backOutSpeed = 20f;
  public float spinUpTime = 1f;
  public float spinDownTime = 1f;
  public GameObject sliceHolder;
  public Transform flapper;
  public GameObject textHolder;
  public GameObject sliceTextPrefab;

  [Inject] SpinWheelFinishedSignal spinFinished {get; set;}

  public bool Spinning {
    get{ return spinning;}
  }

  private float curSpeed;
  private float spinTime = 0f;
  private bool spinning;

  const float sliceAngle = 30f;

  protected override void Awake () {
    base.Awake();

    spinning = false;
  }

  public void Setup(List<SpinWheelOption> newOptions, bool shuffle = true){
    if(newOptions.Sum(o => o.slots) != 12){
      Debug.LogWarning("Wheel options do not equal 12 slots");
      return;
    }
    options = newOptions.Where(o => o.slots > 0).ToArray();
    if(shuffle){
      var rand = new System.Random();
      rand.Shuffle(options);
    }

    transform.eulerAngles = Vector3.zero;
    textHolder.transform.DestroyChildren();

    int sliceIndex = 0;
    foreach(var opt in options){
      //create the text descrip for the slice that's centered between all the spanned slices
      //positioning taken care of by the % of parent scripts on the prefab
      var newDescrip = GameObject.Instantiate(
        sliceTextPrefab,
        Vector3.zero,
        Quaternion.identity,
        textHolder.transform
      ) as GameObject;

      newDescrip.transform.eulerAngles = new Vector3(0, 0,
        (sliceIndex * sliceAngle)
        + (opt.slots * sliceAngle)
        - ((sliceAngle * opt.slots)/2f)
      );
      newDescrip.GetComponent<TextMeshProUGUI>().text = opt.descrip;

      //go through all the slices covered by the slots and set them up
      for(int slot = 0; slot < opt.slots; slot++){
        var sliceGO = sliceHolder.transform.GetChild(sliceIndex);
        sliceGO.GetComponent<Unity.VectorGraphics.SVGImage>().color = opt.color;
        // var peg = sliceGO.Find("peg").gameObject;

        //disable peg for subsequent slices
        // if(slot > 0){
        //   peg.SetActive(false);
        // }else{
        //   peg.SetActive(true);
        // }

        sliceIndex++;
      }
    }
  }

  void  Update ()
  {
  }

  public void Spin(){
    if(!spinning){
      StartCoroutine(SpinTheWheel());
    }
  }

  public void Stop(){
    if(spinning){
      StartCoroutine(StopTheWheel());
    }
  }

  IEnumerator SpinTheWheel ()
  {
    spinning = true;
    spinTime = 0f;

    while (spinning) {
      spinTime += Time.smoothDeltaTime;
      curSpeed = Easings.easeInCubic(0f, speed, Mathf.Clamp01(spinTime / spinUpTime));
      transform.eulerAngles = new Vector3 (0, 0, transform.eulerAngles.z + (Time.smoothDeltaTime * curSpeed));
      yield return 0;
    }

  }

  IEnumerator StopTheWheel ()
  {
    spinning = false;
    spinTime = 0f;

    while (curSpeed > 0f) {
      spinTime += Time.smoothDeltaTime;
      curSpeed = Easings.easeInCubic(speed, 0f, Mathf.Clamp01(spinTime / spinDownTime));
      transform.eulerAngles = new Vector3 (0, 0, transform.eulerAngles.z + (Time.smoothDeltaTime * curSpeed));
      yield return 0;
    }

    //back up the wheel if the flapper is deflected (90 degree is neutral)
    spinTime = 0f;
    var flapperDeflection = Mathf.Abs(90f - flapper.transform.localRotation.eulerAngles.z);
    var curBackOutSpeed = 0f;
    while(flapperDeflection > 6f){
      spinTime += Time.smoothDeltaTime;
      curBackOutSpeed = Easings.easeInCubic(0f, backOutSpeed, Mathf.Clamp01(spinTime / (spinDownTime / 2f)));
      transform.eulerAngles = new Vector3 (0, 0, transform.eulerAngles.z - (Time.smoothDeltaTime * curBackOutSpeed));
      flapperDeflection = Mathf.Abs(90f - flapper.transform.localRotation.eulerAngles.z);
      yield return 0;
    }
    //then ease off the back up speed
    curSpeed = curBackOutSpeed;
    spinTime = 0f;
    while (curSpeed > 0f) {
      spinTime += Time.smoothDeltaTime;
      curSpeed = Time.smoothDeltaTime * Easings.easeInCubic(curBackOutSpeed, 0f, Mathf.Clamp01(spinTime / (spinDownTime / 2f)));
      transform.eulerAngles = new Vector3 (0, 0, transform.eulerAngles.z - curSpeed);
      yield return 0;
    }

    //find out who won (using 30 degree angles for 12 slices)
    var resultSlice = transform.eulerAngles.z / sliceAngle;
    if((int)resultSlice < sliceHolder.transform.childCount){
      //reverse it since the slices go up but the spinner goes down
      var sliceIndex = sliceHolder.transform.childCount - 1 - (int)resultSlice;
      var sliceName = sliceHolder.transform.GetChild(sliceIndex).name;

      SpinWheelOption foundOption = options[0];
      int optIdx = 0;
      foreach(var opt in options){
        optIdx += opt.slots;
        if(optIdx > sliceIndex){
          foundOption = opt;
          break;
        }
      }

      // Debug.Log("Winner winner: " + foundOption.descrip);
      spinFinished.Dispatch(foundOption);
    }
  }
}

public struct SpinWheelOption{
  public Color color {get;set;}
  public int slots {get;set;}
  public string descrip {get;set;}
  public SpinEffect effect {get;set;}
  public int amt {get;set;}
}

public enum SpinEffect{
  Win,
  Lose,
  YouLoseFood,
  TheyLoseFood,
  BothLoseFood,
  TheyRun,
  YouRun
}