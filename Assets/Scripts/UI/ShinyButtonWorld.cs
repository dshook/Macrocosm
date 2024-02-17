using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PygmyMonkey.ColorPalette;
using TMPro;
using MoreMountains.NiceVibrations;
using strange.extensions.mediation.impl;

// Version of ShinyButton that works in world space
// TODO: could make a generic interface or base class that works for both
public class ShinyButtonWorld : View
{
  public ButtonClickedEvent onClick = new ButtonClickedEvent();
  public ButtonClickedEvent onPointerDown = new ButtonClickedEvent();
  public ButtonClickedEvent onPointerUp = new ButtonClickedEvent();
  [Inject] AudioService audioService {get; set;}
  [Inject] InputService input {get; set;}

  [SerializeField] UIColor _color;

  public UIColor color{
    get{ return _color; }
    set{
      _color = value;
      UpdateColors();
    }
  }

  [SerializeField] bool _isSelected;

  public bool isSelected{
    get{ return _isSelected; }
    set{
      // if(value == _isSelected){ return; }

      _isSelected = value;
      if(_isSelected){
        MoveDown();
      }else{
        MoveUp();
      }
    }
  }

  [SerializeField] bool _interactable = true;
  public bool interactable {
    get { return _interactable; }
    set{
      _interactable = value;
      UpdateColors();
    }
  }

  public string label;
  public AudioClip clickClip;

  [Tooltip("Replaces Label")]
  public Sprite sprite;

  public Vector3 moveAmount = Vector3.zero;

  public class ButtonClickedEvent : UnityEvent
  {
  }

  SpriteRenderer _buttonBackground;
  SpriteRenderer buttonBackground{
    get{
      if(_buttonBackground == null){
        _buttonBackground = transform.Find("Background").GetComponent<SpriteRenderer>();
      }
      return _buttonBackground;
    }
  }

  SpriteRenderer _buttonOutline;
  SpriteRenderer buttonOutline{
    get{
      if(_buttonOutline == null){
        _buttonOutline = transform.Find("Outline").GetComponent<SpriteRenderer>();
      }
      return _buttonOutline;
    }
  }

  SpriteRenderer _buttonUnderlay;
  SpriteRenderer buttonUnderlay{
    get{
      if(_buttonUnderlay == null){
        _buttonUnderlay = transform.Find("Underlay").GetComponent<SpriteRenderer>();
      }
      return _buttonUnderlay;
    }
  }

  SpriteRenderer _labelGraphic;
  SpriteRenderer labelGraphic{
    get{
      if(_labelGraphic == null){
        _labelGraphic = buttonBackground.transform.Find("LabelGraphic").GetComponent<SpriteRenderer>();
      }
      return _labelGraphic;
    }
  }

  Transform _underlayTransform;
  Transform underlayTransform{
    get{
      if(_underlayTransform == null){
        _underlayTransform = transform.Find("Underlay");
      }
      return _underlayTransform;
    }
  }

  TMP_Text _text;
  TMP_Text text{
    get{
      if(_text == null){
        _text = buttonBackground.transform.Find("Text").GetComponent<TMP_Text>();
      }
      return _text;
    }
  }

  ColorPalette _uiPalette;
  ColorPalette uiPalette{
    get{
      if(_uiPalette != null && _uiPalette.name == string.Empty){
        //Unset bad palette when in editor
        _uiPalette = null;
      }
      if(_uiPalette == null){
        _uiPalette = ColorPaletteData.Singleton.fromName("UI");
      }
      return _uiPalette;
    }
  }

  Collider2D _collider2d;
  Collider2D collider2d{
    get{
      if(_collider2d == null){
        _collider2d = transform.GetComponentInChildren<Collider2D>();
      }
      return _collider2d;
    }
  }


  bool isDown = false;


  protected override void Awake()
  {
    base.Awake();
    LateUpdate();
    UpdateColors();
  }

  //Use LateUpdate so any changes to the label during updates can be caught the same frame
  void LateUpdate(){

    var usingGraphic = sprite != null;
    text.gameObject.SetActive(!usingGraphic);
    labelGraphic.gameObject.SetActive(usingGraphic);

    if(usingGraphic){
      labelGraphic.sprite = sprite;
    }else{
      text.text = label;
    }

    var touchdownPoint = input.GetTouchDown(); //no bragging here
    if(touchdownPoint != null){
      if(collider2d.OverlapPoint(touchdownPoint.Item1))
      {
        OnPointerDown();
      }
    }
    if(isDown){
      var touchupPoint = input.GetTouchUp();
      if(touchupPoint != null){
        if(collider2d.OverlapPoint(touchupPoint.Item1))
        {
          OnPointerClick();
        }

        OnPointerUp();
      }
    }

  }

  void OnClick()
  {
    if(onClick != null){
      onClick.Invoke();
    }

    MMVibrationManager.Haptic(HapticTypes.LightImpact);
    audioService.PlaySfx(clickClip);
  }

  public virtual void OnPointerClick(){
    if(!interactable){ return; }

    OnClick();
  }

  public void OnPointerDown()
  {
    if(!interactable){ return; }

    if(onPointerDown != null){
      onPointerDown.Invoke();
    }
    MoveDown();
  }

  public void OnPointerUp()
  {
    if(!interactable || isSelected){ return; }

    if(onPointerUp != null){
      onPointerUp.Invoke();
    }
    MoveUp();
  }

  void MoveDown(){
    if(isDown){ return; }
    isDown = true;

    buttonBackground.transform.localPosition += moveAmount;
    buttonOutline.transform.localPosition += moveAmount;
    // underlayTransform.localPosition = underlayTransform.localPosition + moveAmount;
  }

  void MoveUp(){
    if(!isDown){ return; }
    isDown = false;

    buttonBackground.transform.localPosition -= moveAmount;
    buttonOutline.transform.localPosition -= moveAmount;
    // underlayTransform.localPosition = underlayTransform.localPosition - moveAmount;
  }


  void UpdateColors(){
    var finalColor = isSelected ? UIColor.Green : (interactable ? color : UIColor.Gray);
    if(uiPalette == null){
      Debug.LogWarning("Could not get uiPalette");
      return;
    }
    buttonBackground.color = uiPalette.getColorFromName(finalColor.ToString() + "Primary").color;
    buttonOutline.color = uiPalette.getColorFromName(finalColor.ToString() + "Dark").color;
    buttonUnderlay.color = buttonOutline.color;
  }
}