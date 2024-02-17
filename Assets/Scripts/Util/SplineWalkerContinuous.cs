using UnityEngine;

public class SplineWalkerContinuous : MonoBehaviour {

    public BezierSpline spline;

    public float speed = 1f;

    public bool lookForward;

    public float progress;

    private void Update () {
        progress += Time.deltaTime * speed;

        Vector3 position = spline.GetPointFromBeginning(progress);
        transform.position = position;
        if (lookForward) {
            transform.LookAt(position + spline.GetDirection(progress));
        }
    }
}