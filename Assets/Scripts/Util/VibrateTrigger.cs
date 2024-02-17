using System;
using UnityEngine;

public class VibrateTrigger : MonoBehaviour
{

  private void Start()
  {
    // This is here to hopefully get the android manifest updated with vibrate permission
    // https://nice-vibrations-docs.moremountains.com/adding_nice_vibrations.html#android-users
    Handheld.Vibrate();
  }

}