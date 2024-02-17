using Shapes;
using UnityEngine;

[RequireComponent(typeof(Disc))]
public class DiscOffsetAnimator : MonoBehaviour {

	public float speed = 1.0f;

  Disc disc;

	void Start () {
    disc = GetComponent<Disc>();
	}

	void Update () {
    disc.DashOffset += Time.deltaTime * speed;
	}

}
