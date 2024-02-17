using UnityEngine;
using strange.extensions.mediation.impl;
using System;

public class CameraMovementView : View
{
  [Inject] TraumaModel trauma { get; set; }

  public GameObject shakerHolder;

  public float traumaDamping = 0.4f;
  public float noiseFrequency = 10f;
  public float maxShakeAmt = 2f;


  void Update()
  {
      UpdateShake();
  }

  void UpdateShake()
  {
    if (Input.GetKeyDown(KeyCode.Asterisk)) {
      trauma.trauma += 0.4f;
    }

    trauma.trauma -= traumaDamping * Time.deltaTime;
    var shake = Mathf.Pow(trauma.trauma, 3);

    var destX = maxShakeAmt * shake * (Mathf.PerlinNoise(0f, Time.time * noiseFrequency) - 0.5f);
    var destY = maxShakeAmt * shake * (Mathf.PerlinNoise(1f, Time.time * noiseFrequency) - 0.5f);

    shakerHolder.transform.position = new Vector3(destX, destY, 0f);
  }


}