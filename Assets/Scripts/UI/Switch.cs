using MoreMountains.NiceVibrations;
using strange.extensions.mediation.impl;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// A component to handle UI toggle switches
/// </summary>
public class Switch : View, IPointerClickHandler
{

  [Inject] AudioService audioService {get; set;}

  [Header("Switch")]
  public Transform SwitchKnob;

  public Unity.VectorGraphics.SVGImage background;

  /// the current state of the switch
  bool _isOn = false;
  public bool isOn{
    get{ return _isOn;}
    set{
      _isOn = value;
      SetKnobPosition();
    }
  }

  public Transform OffPosition;
  public Transform OnPosition;
  public AnimationCurve KnobMovementCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
  public float KnobMovementDuration = 0.2f;

  public Color onColor;
  public Color offColor;

  public AudioClip toggleClip;

  public ToggleEvent onValueChanged = new ToggleEvent();

  protected float _knobMovementStartedAt = -50f;
  public bool interactable = true;

  protected override void Awake()
  {
    base.Awake();
    SetKnobPosition();
  }

  /// <summary>
  /// On init, we set our current switch state
  /// </summary>
  protected void SetKnobPosition()
  {
    if (isOn)
    {
      SwitchKnob.position = OnPosition.position;
      background.color = onColor;
    }
    else
    {
      SwitchKnob.position = OffPosition.position;
      background.color = offColor;
    }
  }

  protected void Update()
  {
    var passedTime = Time.unscaledTime - _knobMovementStartedAt;
    if (passedTime < KnobMovementDuration)
    {
      float time = Remap(passedTime, 0f, KnobMovementDuration, 0f, 1f);
      float value = KnobMovementCurve.Evaluate(time);

      if (isOn)
      {
        SwitchKnob.position = Vector3.Lerp(OffPosition.position, OnPosition.position, value);
        background.color = Color.Lerp(offColor, onColor, value);
      }
      else
      {
        SwitchKnob.position = Vector3.Lerp(OnPosition.position, OffPosition.position, value);
        background.color = Color.Lerp(onColor, offColor, value);
      }
    }
  }

  /// <summary>
  /// Use this method to go from one state to the other
  /// </summary>
  public virtual void SwitchState()
  {
    _knobMovementStartedAt = Time.unscaledTime;
    _isOn = !_isOn;
    MMVibrationManager.Haptic(HapticTypes.Success);
    audioService.PlaySfx(toggleClip);

    if (onValueChanged != null)
    {
      onValueChanged.Invoke(isOn);
    }
  }

  public virtual void OnPointerClick(PointerEventData eventData){
    if(!interactable){ return; }

    SwitchState();
  }

  protected float Remap(float x, float A, float B, float C, float D)
  {
      float remappedValue = C + (x - A) / (B - A) * (D - C);
      return remappedValue;
  }

  public class ToggleEvent : UnityEvent<bool>
  {
  }
}