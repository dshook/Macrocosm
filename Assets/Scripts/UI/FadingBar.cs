using UnityEngine;

//A bar with contents and faders that appear if the contents have too many items
[ExecuteInEditMode]
public class FadingBar : MonoBehaviour
{

  public float childItemWidth = 1f;

  GameObject _fades;
  GameObject Fades{
    get{
      if(_fades == null){
        _fades = transform.Find("Fades").gameObject;
      }
      return _fades;
    }
  }

  RectTransform _content;
  RectTransform Content{
    get{
      if(_content == null){
        _content = transform.Find("Content").GetComponent<RectTransform>();
      }
      return _content;
    }
  }

  int maxItems;

  void Update(){
    maxItems = Mathf.RoundToInt(Content.rect.width / childItemWidth);
    Fades.SetActive(Content.transform.childCount > maxItems);
  }

}