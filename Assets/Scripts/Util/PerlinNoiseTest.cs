using UnityEngine;
using System.Collections;
using System.Linq;

// Create a texture and fill it with Perlin noise.
// Try varying the xOrg, yOrg and scale values in the inspector
// while in Play mode to see the effect they have on the noise.

public class PerlinNoiseTest : MonoBehaviour
{
    // Width and height of the texture in pixels.
    public int pixWidth;
    public int pixHeight;
    public Color color;

    public Vector2 origin;
    // The number of cycles of the basic noise pattern that are repeated
    // over the width and height of the texture.
    public float[] scales;

    private Texture2D noiseTex;
    private Color[] pix;
    private SpriteRenderer rend;

    void Start()
    {
        rend = GetComponent<SpriteRenderer>();

        // Set up the texture and a Color array to hold pixels during processing.
        noiseTex = new Texture2D(pixWidth, pixHeight);
        pix = new Color[noiseTex.width * noiseTex.height];
        // rend.material.mainTexture = noiseTex;
        rend.sprite = Sprite.Create(noiseTex, new Rect(0.0f, 0.0f, noiseTex.width, noiseTex.height), new Vector2(0.5f, 0.5f), 100.0f);
    }

    void CalcNoise()
    {
        // For each pixel in the texture...
        float y = 0.0F;

        while (y < noiseTex.height)
        {
            float x = 0.0F;
            while (x < noiseTex.width)
            {
              var sample = RandomExtensions.SamplePerlinOctaves(origin, new Vector2(x / noiseTex.width, y / noiseTex.height), scales);

              pix[(int)y * noiseTex.width + (int)x] = color * sample;
              x++;
            }
            y++;
        }

        // Copy the pixel data to the texture and load it into the GPU.
        noiseTex.SetPixels(pix);
        noiseTex.Apply();
    }

    Vector2 prevOrigin;
    float prevScaleSum = 0;
    Color prevColor;

    bool needsUpdate = true;
    float timer = 0f;

    void Update()
    {
      var scaleSum = scales.Sum();

      if(scaleSum != prevScaleSum || prevOrigin != origin || color != prevColor){
        prevScaleSum = scaleSum;
        prevOrigin = origin;
        prevColor = color;
        needsUpdate = true;
        timer = 0f;
      }

      timer += Time.deltaTime;

      if(needsUpdate && timer > 1.5f){
        needsUpdate = false;
        timer = 0f;
        CalcNoise();
      }
    }
}