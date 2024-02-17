using System.Collections;
using System.Collections.Generic;
using strange.extensions.mediation.impl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AtomRenderer : View {

  public int awakeSize = -1;
  int _size = 1;
  public int size{
    get{ return _size; }
    set{
      var changed = _size != value;
      _size = value;
      if(changed){
        UpdateText();
        UpdateAtomDisplay();
      }
    }
  }

  bool _showTextSize = true;
  public bool showTextSize {
    get{ return _showTextSize; }
    set{
      var changed = _showTextSize != value;
      _showTextSize = value;
      if(changed){ UpdateText(); }
    }
  }

  string _overrideText = null;
  public string overrideText {
    get{ return _overrideText; }
    set{
      var changed = _overrideText != value;
      _overrideText = value;
      if(changed){ UpdateText(); }
    }
  }

  Color _overrideColor = Color.white;
  public Color overrideColor {
    get{ return _overrideColor; }
    set{
      var changed = _overrideColor != value;
      _overrideColor = value;
      if(changed){ UpdateText(); }
    }
  }

  [Inject] ResourceLoaderService loader {get; set;}

  public SpriteRenderer numberRenderer;
  public Transform counterNumberRotator;

  public TMP_Text text;
  public TMP_Text textUnderlay;

  public int atomNameSizeChange = -3;
  SpriteRenderer spriteRenderer;
  Unity.VectorGraphics.SVGImage svgImageRenderer;
  Image imageRenderer;
  bool needsUpdate = false;

  protected override void Awake () {
    base.Awake();

    spriteRenderer = GetComponent<SpriteRenderer>();
    svgImageRenderer = GetComponent<Unity.VectorGraphics.SVGImage>();
    imageRenderer = GetComponent<Image>();

    if(awakeSize > 0){ size = awakeSize; }

    UpdateText();
    UpdateAtomDisplay();
  }

  void Update(){
    if(text != null){
      //keep the text level as the particle spins out of control
      text.transform.localRotation = Quaternion.Inverse(transform.parent.localRotation);
    }
    if(textUnderlay != null){
      textUnderlay.transform.localRotation = text.transform.localRotation;
    }
    if(numberRenderer != null && counterNumberRotator != null){
      //keep the text level as the particle spins out of control
      counterNumberRotator.transform.localRotation = Quaternion.Inverse(transform.parent.localRotation);
    }

    if(needsUpdate){
      needsUpdate = false;
      UpdateText();
      UpdateAtomDisplay();
    }
  }


  void UpdateText(){

    if(numberRenderer != null){
      numberRenderer.color = overrideColor;
      numberRenderer.sprite = loader.Load<Sprite>("Art/stage1/atom numbers/" + size);
    }

    if(text != null){
      text.color = overrideColor;
    }

    if(!string.IsNullOrEmpty(overrideText)){
      if(text != null){
        text.text = overrideText;
      }
    }else if(showTextSize && size >= 5){
      if(text != null){
        var elementName = elementMap.ContainsKey(size) ? elementMap[size] : string.Empty;
        text.text = string.Format("{0}<size={2}>{1}</size>", size, elementName, atomNameSizeChange);
      }
    }else{
      if(text != null){
        text.text = string.Empty;
      }
      if(numberRenderer != null){
        numberRenderer.sprite = null;
      }
    }

    if(textUnderlay != null){
      textUnderlay.text = text.text;
    }
  }

  void UpdateAtomDisplay(){
    if(loader == null){ needsUpdate = true; return; }

    //pick the right svg for the job
    var targetGraphic = loader.particleSvgMap[10];
    if(loader.particleSvgMap.ContainsKey(size)){
      targetGraphic = loader.particleSvgMap[size];
    }
    if(spriteRenderer != null && spriteRenderer.sprite.name != targetGraphic.name){
      spriteRenderer.sprite = targetGraphic;

      //Set the Z position based on the size so that all the same sizes will be rendered together
      //in an instance batch.
      spriteRenderer.transform.position = spriteRenderer.transform.position.SetZ(size * 0.001f);
    }
    if(svgImageRenderer != null && svgImageRenderer.sprite.name != targetGraphic.name){
      svgImageRenderer.sprite = targetGraphic;
    }
    if(imageRenderer != null && imageRenderer.sprite.name != targetGraphic.name){
      imageRenderer.sprite = targetGraphic;
    }

    var color = ColorMap(size);
    var rendererColor = color;
    //Leave out the sizes < 10 because they're already colored
    if(size < 10){
      rendererColor = Color.white;
    }
    if(spriteRenderer != null){
      spriteRenderer.color = rendererColor;
    }
    if(svgImageRenderer != null){
      svgImageRenderer.color = rendererColor;
    }
    if(imageRenderer != null){
      imageRenderer.color = rendererColor;
    }
    if(textUnderlay != null){
      textUnderlay.outlineColor = color * Colors.mediumGray;
      textUnderlay.faceColor = color * Colors.mediumGray;
    }

  }

  public static Dictionary<int, string> elementMap = new Dictionary<int, string>(){
    {1, "H"},
    {2, "He"},
    {3, "Li"},
    {4, "Be"},
    {5, "B"},
    {6, "C"},
    {7, "N"},
    {8, "O"},
    {9, "F"},
    {10, "Ne"},
    {11, "Na"},
    {12, "Mg"},
    {13, "Al"},
    {14, "Si"},
    {15, "P"},
    {16, "S"},
    {17, "Cl"},
    {18, "Ar"},
    {19, "K"},
    {20, "Ca"},
    {21, "Sc"},
    {22, "Ti"},
    {23, "V"},
    {24, "Cr"},
    {25, "Mn"},
    {26, "Fe"},
    {27, "Co"},
    {28, "Ni"},
    {29, "Cu"},
    {30, "Zn"},
    {31, "Ga"},
    {32, "Ge"},
  };

  public static Dictionary<int, string> elementNames = new Dictionary<int, string>(){
    {1, "Hydrogen"},
    {2, "Helium"},
    {3, "Lithium"},
    {4, "Beryllium"},
    {5, "Boron"},
    {6, "Carbon"},
    {7, "Nitrogen"},
    {8, "Oxygen"},
    {9, "Flourine"},
    {10, "Neon"},
    {11, "Sodium"},
    {12, "Magnesium"},
    {13, "Aluminium"},
    {14, "Silicon"},
    {15, "Phosphorus"},
    {16, "Sulfur"},
    {17, "Chlorine"},
    {18, "Argon"},
    {19, "Potassium"},
    {20, "Calcium"},
    {21, "Scandium"},
    {22, "Titanium"},
    {23, "Vanadium"},
    {24, "Chromium"},
    {25, "Manganese"},
    {26, "Iron"},
    {27, "Cobalt"},
    {28, "Nickle"},
    {29, "Copper"},
    {30, "Zinc"},
    {31, "Gallium"},
    {32, "Gerranium"},
  };


  public static Color ColorMap(int size) {
    switch(size){
      case 1:
        return Colors.GetColor(Colors.stage1, "hydrogen");
      case 2:
      case 10:
      case 18:
        return Colors.GetColor(Colors.stage1, "nobleGasses");
      case 3:
      case 11:
      case 19:
        return Colors.GetColor(Colors.stage1, "alkaliMetals");
      case 4:
      case 12:
      case 20:
        return Colors.GetColor(Colors.stage1, "alkalineEarth");
      case 5:
      case 13:
      case 16:
        return Colors.GetColor(Colors.stage1, "transitionMetals");
      case 6:
      case 14:
        return Colors.GetColor(Colors.stage1, "carbon");
      case 7:
        return Colors.GetColor(Colors.stage1, "nitrogen");
      case 8:
        return Colors.GetColor(Colors.stage1, "oxygen");
      case 9:
      case 17:
        return Colors.GetColor(Colors.stage1, "flourine");
      case 15:
        return Colors.GetColor(Colors.stage1, "phosphorus");
      case 21:
      case 22:
      case 23:
      case 24:
      case 25:
      case 26:
        return Colors.GetColor(Colors.stage1, "transition");
      default:
        return Colors.GetColor(Colors.stage1, "hydrogen");
    }
  }

}
