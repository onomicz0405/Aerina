using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallLogic : MonoBehaviour
{
    public GameObject parent;
    GameObject player;
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player")
        {
            player = collider.gameObject;
            PickUpBall();           
        }
    }

    void PickUpBall()
    {
        //turn off collider and renderer of ball, freeze its physics
        parent.GetComponent<SphereCollider>().enabled = false;
        parent.GetComponent<MeshRenderer>().enabled = false;
        parent.GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<SphereCollider>().enabled = false;
        //set ball weapon
        //player.GetComponent<Movement>().WH.SetBall(parent, gameObject);       
    }
}
