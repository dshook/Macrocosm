using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Must be a trigger collider
[RequireComponent(typeof(BoxCollider2D))]
public class RbPusher : MonoBehaviour {

	public float moveForce = 1.0f;
  public Vector2 pushDirection;

  HashSet<Rigidbody2D> rbList = new HashSet<Rigidbody2D>();

	void Start () {
	}

	void Update () {
    rbList.RemoveWhere(s => s == null);
    foreach(var rb in rbList){
      rb.AddForce(pushDirection * moveForce);
    }
	}

	void OnTriggerEnter2D(Collider2D col)
	{
    var enteringRb = GetRigidbody(col);
    if(enteringRb != null){
      rbList.Add(enteringRb);
    }
	}

	void OnTriggerExit2D(Collider2D col)
  {
    var enteringRb = GetRigidbody(col);
    if(enteringRb != null){
      rbList.Remove(enteringRb);
    }
  }

  Rigidbody2D GetRigidbody(Collider2D col){
    var colRb = col.transform.GetComponent<Rigidbody2D>();
    var parentRb = col.transform.parent.GetComponent<Rigidbody2D>();
    return colRb == null ? parentRb : colRb; //null coalesce doesn't work? wtf
  }
}
