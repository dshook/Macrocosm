using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PygmyMonkey.ColorPalette;
using TMPro;

[ExecuteInEditMode]
public class Modal : MonoBehaviour
{
  [SerializeField] UIColor _color;

  public UIColor color{
    get{ return _color; }
    set{
      _color = value;
      UpdateColors();
    }
  }

  Graphic _background;
  Graphic Background{
    get{
      if(_background == null){
        _background = transform.Find("Background").GetComponent<Graphic>();
      }
      return _background;
    }
  }

  Graphic _outline;
  Graphic Outline{
    get{
      if(_outline == null){
        _outline = transform.Find("Outline").GetComponent<Graphic>();
      }
      return _outline;
    }
  }

  Graphic _underlay;
  Graphic Underlay{
    get{
      if(_underlay == null){
        _underlay = transform.Find("Underlay").GetComponent<Graphic>();
      }
      return _underlay;
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

  void Awake()
  {
  }

  void Update(){

  }

  void OnEnable(){
    UpdateColors();
  }

  void UpdateColors(){
    Background.color = uiPalette.getColorFromName(color.ToString() + "Primary").color;
    Outline.color = uiPalette.getColorFromName(color.ToString() + "Dark").color;
    Underlay.color = uiPalette.getColorFromName("ModalUnderlay").color;
  }
}