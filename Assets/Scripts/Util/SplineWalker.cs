using UnityEngine;

public enum SplineWalkerMode {
    Once,
    Loop,
    PingPong,
}

public class SplineWalker : MonoBehaviour {

    public BezierSpline spline;

    public float duration = 5f;

    public bool lookForward;

    public SplineWalkerMode mode;

    [Range(0, 1f)]
    public float scaleMargin = 0.1f;
    public bool scaleInOut = false;
    private bool scaledThisLoop = false;

    [Range(0, 1f)]
    public float progress;
    private bool goingForward = true;

    private void Update () {
        if (goingForward) {
            progress += Time.deltaTime / duration;
            if (scaleInOut && progress > 1f - scaleMargin && !scaledThisLoop)
            {
                LeanTween.scale(gameObject, Vector3.zero, 0.65f);
                LeanTween.scale(gameObject, Vector3.one, 0.5f).setDelay(0.65f);
                scaledThisLoop = true;
            }
            if (progress > 1f) {
                if (mode == SplineWalkerMode.Once) {
                    progress = 1f;
                    scaledThisLoop = false;
                }
                else if (mode == SplineWalkerMode.Loop) {
                    progress -= 1f;
                    scaledThisLoop = false;
                }
                else {
                    progress = 2f - progress;
                    goingForward = false;
                    scaledThisLoop = false;
                }
            }
        }
        else {
            progress -= Time.deltaTime / duration;
            if (progress < 0f) {
                progress = -progress;
                goingForward = true;
            }
        }

        Vector3 position = spline.GetPoint(progress);
        transform.position = position;
        if (lookForward) {
            transform.LookAt(position + spline.GetDirection(progress));
        }
    }
}