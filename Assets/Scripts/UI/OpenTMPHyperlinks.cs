using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TextMeshProUGUI))]
public class OpenTMPHyperlinks : MonoBehaviour, IPointerClickHandler {

  TMP_Text pTextMeshPro;

  public void Start(){
    pTextMeshPro = GetComponent<TMP_Text>();
  }

  public void OnPointerClick(PointerEventData eventData) {
    int linkIndex = TMP_TextUtilities.FindIntersectingLink(pTextMeshPro, Input.mousePosition, Camera.main);
    if( linkIndex != -1 ) { // was a link clicked?
        TMP_LinkInfo linkInfo = pTextMeshPro.textInfo.linkInfo[linkIndex];

        // open the link id as a url, which is the metadata we added in the text field
        Application.OpenURL(linkInfo.GetLinkID());
    }
  }
}