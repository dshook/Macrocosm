using UnityEngine;
using System.Collections;
using TMPro;
using System.Linq;

[ExecuteInEditMode]
public class SplineText : MonoBehaviour
{
    public bool manualUpdate;
    public float characterWidthMult = 0.01f;
    //public float length; // Currently Only Informational
    public BezierSpline vertexCurve;
    public TMP_Text m_TextComponent;

    void Awake()
    {
        // Make sure I have the thigs I need to get the data to deform text
        if (m_TextComponent == null)
            m_TextComponent = gameObject.GetComponent<TMP_Text>();
        if (vertexCurve == null)
            vertexCurve = gameObject.GetComponent<BezierSpline>();
        UpdateTextPosition();
    }

    void Start()
    {
    }

    private float lastCharWidthMult;
    private string lastText;
    private TextAlignmentOptions textAlign;

    void Update()
    {
        if (manualUpdate) return;

        if(m_TextComponent.havePropertiesChanged)
        {
            UpdateTextPosition();
        }
    }

    void OnRenderObject()
    {
        if(lastCharWidthMult != characterWidthMult || lastText != m_TextComponent.text || textAlign != m_TextComponent.alignment)
        {
            UpdateTextPosition();
        }

        lastCharWidthMult = characterWidthMult;
        lastText = m_TextComponent.text;
        textAlign = m_TextComponent.alignment;
    }

    public void UpdateTextPosition()
    {
        // Make sure I have the thigs I need to get the data to deform text
        if (m_TextComponent == null)
            m_TextComponent = gameObject.GetComponent<TMP_Text>();
        if (vertexCurve == null)
            vertexCurve = gameObject.GetComponent<BezierSpline>();

        if (m_TextComponent)
        {

            Vector3[] vertexPositions;

            m_TextComponent.renderMode = TextRenderFlags.Render;
            m_TextComponent.ForceMeshUpdate();
            m_TextComponent.renderMode = TextRenderFlags.DontRender;

            TMP_TextInfo textInfo = m_TextComponent.textInfo;
            int characterCount = textInfo.characterCount;

            if (characterCount >= 0)
            {
                vertexPositions = textInfo.meshInfo[0].vertices;

                float boundsMaxX = m_TextComponent.rectTransform.rect.width * 0.5f;
                float boundsMinX = -boundsMaxX;

                //centering vars
                float minVertexX = 0f, maxVertexX = 0f, midVertexX = 0f;

                if (m_TextComponent.alignment == TextAlignmentOptions.Center)
                {
                    maxVertexX = vertexPositions.Max(v => v.x);
                    minVertexX = vertexPositions.Min(v => v.x);
                    midVertexX = ((maxVertexX - minVertexX) / 2) + minVertexX;
                }

                for (int i = 0; i < characterCount; i++)
                {
                    var charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible)
                        continue;

                    int vertexIndex = charInfo.vertexIndex;

                    // Compute the baseline mid point for each character
                    Vector3 offsetToMidBaseline = new Vector3(
                        (charInfo.bottomLeft.x  + charInfo.bottomRight.x) / 2,
                        textInfo.characterInfo[i].baseLine
                    );

                    //change vectors from each of the corners to the 'center' point of the character
                    Vector3 cBL = vertexPositions[vertexIndex + 0] - offsetToMidBaseline;
                    Vector3 cTL = vertexPositions[vertexIndex + 1] - offsetToMidBaseline;
                    Vector3 cTR = vertexPositions[vertexIndex + 2] - offsetToMidBaseline;
                    Vector3 cBR = vertexPositions[vertexIndex + 3] - offsetToMidBaseline;

                    float t = 0f;
                    if (m_TextComponent.alignment == TextAlignmentOptions.Center)
                    {
                        //this is essentially spreading out the characters from the middle of the spline
                        t = 0.5f + (offsetToMidBaseline.x - midVertexX) * characterWidthMult;
                    }
                    else if (m_TextComponent.alignment == TextAlignmentOptions.Justified)
                    {
                        t = (0.5f + i) / (float)characterCount;
                    }
                    else
                    {
                        //find the % of the way through the bounding box this character is
                        t = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX);
                    }
                    Vector3 point = transform.InverseTransformPoint(vertexCurve.GetPoint(t));
                    Vector3 xAxis = transform.InverseTransformDirection(vertexCurve.GetVelocity(t)).normalized;
                    Vector3 yAxis = (Vector3.up - xAxis * xAxis.y).normalized;

                    vertexPositions[vertexIndex + 0] = point + cBL.x * xAxis + cBL.y * yAxis;
                    vertexPositions[vertexIndex + 1] = point + cTL.x * xAxis + cTL.y * yAxis;
                    vertexPositions[vertexIndex + 2] = point + cTR.x * xAxis + cTR.y * yAxis;
                    vertexPositions[vertexIndex + 3] = point + cBR.x * xAxis + cBR.y * yAxis;
                }

                // Upload the mesh with the revised information
                m_TextComponent.mesh.vertices = vertexPositions;
                m_TextComponent.mesh.uv = textInfo.meshInfo[0].uvs0;
                m_TextComponent.mesh.uv2 = textInfo.meshInfo[0].uvs2;
                m_TextComponent.mesh.colors32 = textInfo.meshInfo[0].colors32;

                m_TextComponent.mesh.RecalculateBounds(); // We need to update the bounds of the text object.
            }
        }
    }

}