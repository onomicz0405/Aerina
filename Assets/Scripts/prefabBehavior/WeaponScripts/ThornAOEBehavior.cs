using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ThornAOEBehavior : MonoBehaviour
{
    public float duration;
    public float bounciness;
    bool canBounce = true;
    List<GameObject> restrainedAgents = new List<GameObject>();
    private void Awake()
    {
        StartCoroutine(WaitForDestroy(this.gameObject));
    }

    //safety net if going too fast
    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Player" && canBounce)
        {
            canBounce = false;
            Rigidbody rb = other.GetComponent<Rigidbody>();
            //jump pad                                    
            ApplyMovement(new Vector3(0, bounciness, 0), rb);
        }
        if (other.tag == "hurtbox")
        {
            //restrain
            if (!restrainedAgents.Contains(other.gameObject))
            {
                restrainedAgents.Add(other.gameObject);
            }
            if (other.GetComponent<NavMeshAgent>().enabled == true)
            {
                other.GetComponent<NavMeshAgent>().isStopped = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        canBounce = true;
        if (other.tag == "hurtbox")
        {
            if (other.GetComponent<NavMeshAgent>().enabled == true)
            {
                other.GetComponent<NavMeshAgent>().isStopped = false;
            }
            
        }
    }

    void ApplyMovement(Vector3 force, Rigidbody rb)
    {
        Vector3 currentVel = rb.velocity;
        Vector3 newVel = new Vector3(currentVel.x, 0, currentVel.z);

        if (currentVel.y * force.y >= 0)
        {
            rb.GetComponent<Movement>().isJump = true;
            rb.GetComponent<Movement>().Jump();
            newVel.y = force.y;
        }
        else if (Mathf.Abs(rb.velocity.y) > force.y && currentVel.y < 0)
        {
            newVel.y = -currentVel.y;
        }
        else
        {
            rb.GetComponent<Movement>().isJump = true;
            rb.GetComponent<Movement>().Jump();
            newVel.y = force.y;
        }

        rb.velocity = newVel;
    }

    IEnumerator WaitForDestroy(GameObject obj)
    {
        yield return new WaitForSecondsRealtime(duration);
        foreach (GameObject x in restrainedAgents)
        {
            if (x != null)
            {
                if (x.GetComponent<NavMeshAgent>().enabled == true)
                {
                    x.GetComponent<NavMeshAgent>().isStopped = false;
                }
            }
        }
        Destroy(this.gameObject);
    }
}
