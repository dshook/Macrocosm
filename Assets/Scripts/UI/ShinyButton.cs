using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PygmyMonkey.ColorPalette;
using TMPro;
using MoreMountains.NiceVibrations;
using strange.extensions.mediation.impl;

[ExecuteInEditMode]
public class ShinyButton : View, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
  public ButtonClickedEvent onClick = new ButtonClickedEvent();
  public ButtonClickedEvent onPointerDown = new ButtonClickedEvent();
  public ButtonClickedEvent onPointerUp = new ButtonClickedEvent();
  [Inject] AudioService audioService {get; set;}

  [SerializeField] UIColor _color;

  public UIColor color{
    get{ return _color; }
    set{
      if(_color == value){ return; }
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
      if(_interactable == value){ return; }
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

  Graphic _buttonBackground;
  Graphic buttonBackground{
    get{
      if(_buttonBackground == null){
        _buttonBackground = transform.Find("Background").GetComponent<Graphic>();
      }
      return _buttonBackground;
    }
  }

  Graphic _buttonOutline;
  Graphic buttonOutline{
    get{
      if(_buttonOutline == null){
        _buttonOutline = transform.Find("Outline").GetComponent<Graphic>();
      }
      return _buttonOutline;
    }
  }

  Graphic _buttonUnderlay;
  Graphic buttonUnderlay{
    get{
      if(_buttonUnderlay == null){
        _buttonUnderlay = transform.Find("Underlay").GetComponent<Graphic>();
      }
      return _buttonUnderlay;
    }
  }

  Unity.VectorGraphics.SVGImage _labelGraphic;
  Unity.VectorGraphics.SVGImage labelGraphic{
    get{
      if(_labelGraphic == null){
        _labelGraphic = buttonBackground.transform.Find("LabelGraphic").GetComponent<Unity.VectorGraphics.SVGImage>();
      }
      return _labelGraphic;
    }
  }

  RectTransform _underlayRectTransform;
  RectTransform underlayRectTransform{
    get{
      if(_underlayRectTransform == null){
        _underlayRectTransform = transform.Find("Underlay").GetComponent<RectTransform>();
      }
      return _underlayRectTransform;
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


  bool isDown = false;
  public bool IsDown { get{ return isDown; }}


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

  }

  public virtual void OnPointerClick(PointerEventData eventData){
    if(!interactable){ return; }

    if(onClick != null){
      onClick.Invoke();
    }

    MMVibrationManager.Haptic(HapticTypes.LightImpact);
    audioService.PlaySfx(clickClip);
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if(!interactable){ return; }

    if(onPointerDown != null){
      onPointerDown.Invoke();
    }
    MoveDown();
  }

  public void OnPointerUp(PointerEventData eventData)
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
    underlayRectTransform.offsetMax = underlayRectTransform.offsetMax + (Vector2)moveAmount;
  }

  void MoveUp(){
    if(!isDown){ return; }
    isDown = false;

    buttonBackground.transform.localPosition -= moveAmount;
    buttonOutline.transform.localPosition -= moveAmount;
    underlayRectTransform.offsetMax = underlayRectTransform.offsetMax - (Vector2)moveAmount;
  }


  void UpdateColors(){
    var finalColor = isSelected ? UIColor.Green : (interactable ? color : UIColor.Gray);
    buttonBackground.color = uiPalette.getColorFromName(finalColor.ToString() + "Primary").color;
    buttonOutline.color = uiPalette.getColorFromName(finalColor.ToString() + "Dark").color;
    buttonUnderlay.color = buttonOutline.color;
  }
}