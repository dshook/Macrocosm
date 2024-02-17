using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class MultiText : MonoBehaviour
{
  public string text;

  private string _lastFrameText;
  private List<TMP_Text> _tmpTexts;

  private void OnEnable()
  {
    _tmpTexts = new List<TMP_Text>(GetComponentsInChildren<TMP_Text>());
  }

  void Update()
  {
    if (HasTextChanged())
    {
      UpdateSpriteTexts();
    }
  }

  bool HasTextChanged()
  {
    bool textChanged = false;

    if (text != _lastFrameText)
    {
      textChanged = true;
    }

    _lastFrameText = text;

    return textChanged;
  }

  void UpdateSpriteTexts()
  {
    foreach (var tmpText in _tmpTexts)
    {
      tmpText.text = text;
    }
  }
}