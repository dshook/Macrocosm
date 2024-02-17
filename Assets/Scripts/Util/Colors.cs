using PygmyMonkey.ColorPalette;
using UnityEngine;
using UnityEngine.UI;

public static class Colors{
  public static Color golden = "F5C10A".ToColor();
  public static string goldenHex = "F5C10A";

  public static Color lightGray = "ABABAB".ToColor();
  public static Color mediumLightGray = "c8c8c8".ToColor();
  public static Color mediumGray = "777777".ToColor();
  public static Color darkGray = "444444".ToColor();
  public static Color red = "FF3D48".ToColor();
  public static Color pink = "FF3DB1".ToColor();
  public static Color purple = "D73DFF".ToColor();
  public static Color darkPurple = "520045".ToColor();
  public static Color bluepurple = "5C3DFF".ToColor();
  public static Color blue = "3D74FF".ToColor();
  public static Color teal = "3DDFFF".ToColor();
  public static Color mint = "3DFFAD".ToColor();
  public static Color green = "3DFF56".ToColor();
  public static Color neon = "A5FF3D".ToColor();
  public static Color yellow = "FFE53D".ToColor();
  public static Color orange = "FF9B3D".ToColor();
  public static Color burntorange = "FF623D".ToColor();

  public static Color buttonBlue = "3288BD".ToColor();
  public static Color buttonDisabled = "607D8B".ToColor();

  public static Color greenGrass = "6DAA2D".ToColor();
  public static Color friendlyGreen = "04BE1F".ToColor();
  public static Color water = "39A3D8".ToColor();
  public static Color lake = "2AD8DB".ToColor();
  public static Color woodBrown = "3D2200".ToColor();

  public static Color land = "553A1D".ToColor();
  public static Color sand = "F1B678".ToColor();
  public static Color snow = "CCECEF".ToColor();
  public static Color tundra = "8FBFA3".ToColor();
  public static Color stone = "6A3026".ToColor();
  public static Color shallows = "1CA8F8".ToColor();

  public static Color transparent = new Color(0, 0, 0, 0);
  public static Color transparentWhite = new Color(1, 1, 1, 0);
  public static Color transparentWhite15 = new Color(1, 1, 1, 0.15f);
  public static Color transparentWhite50 = new Color(1, 1, 1, 0.5f);
  public static Color transparentBlack50 = new Color(0, 0, 0, 0.5f);
  public static Color transparentRed = new Color(1, 0, 0, 0.5f);
  public static Color transparentGreen = new Color(0, 1, 0, 0.5f);
  public static Color transparentBlue = new Color(0, 0, 1, 0.5f);

  public static Color redText = "B50000".ToColor();
  public static Color orangeText = "E33E0C".ToColor();
  public static Color purpleText = "66004B".ToColor();
  public static Color greenText = "05BF6B".ToColor();

  public static Color nearBlack = "000914".ToColor();

  public static Color random{
    get{
      return new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1);
    }
  }

  public static ColorPalette stage1 {
    get{
      return ColorPaletteData.Singleton.fromName("Stage1");
    }
  }

  public static ColorPalette UI {
    get{
      return ColorPaletteData.Singleton.fromName("UI");
    }
  }

  public static Color GetColor(ColorPalette palette, string colorName){
    return palette.getColorFromName(colorName).color;
  }

  public static ColorBlock interactableColorBlock = new ColorBlock(){
    normalColor = buttonBlue,
    highlightedColor = buttonBlue,
    pressedColor = buttonBlue,
    selectedColor = buttonBlue,
    disabledColor = buttonDisabled,
    colorMultiplier = 1f,
    fadeDuration = 0.1f
  };

  public static ColorBlock nonInteractableColorBlock = new ColorBlock(){
    normalColor = buttonDisabled,
    highlightedColor = buttonDisabled,
    pressedColor = buttonDisabled,
    selectedColor = buttonDisabled,
    disabledColor = buttonDisabled,
    colorMultiplier = 1f,
    fadeDuration = 0.1f
  };
}
