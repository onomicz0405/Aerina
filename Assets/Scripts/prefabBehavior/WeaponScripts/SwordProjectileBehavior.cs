using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SwordProjectileBehavior : MonoBehaviour
{
    Rigidbody rb;
    bool hasCollided = false;
    public GameObject impact;
    public float damage;
    public float upwardForce;
    public float knockback;
    Vector3 force;
    void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasCollided)
        {
            if (other != null)
            {
               
                if (other.tag == "hurtbox")
                {
                    other.GetComponent<EnemyData>().TakeDamage(damage);
                    //launch
                    if (other.GetComponent<Rigidbody>() != null)
                    {
                        Vector3 currentVel = other.GetComponent<Rigidbody>().velocity;
                        Vector3 newVel = new Vector3(0, 0, 0);
                        force += new Vector3(rb.velocity.normalized.x * knockback, upwardForce, rb.velocity.normalized.z * knockback);
                        //if the currentVel and the force of the source are going the same direction, add them together, else, just set it equal to the force; check for each axis
                        if (Vector3.Angle(force, currentVel) < 90)
                        {
                            newVel = force + currentVel;
                        }
                        else
                        {
                            newVel = force;
                        }
                        if (newVel.y * currentVel.y > 0)
                        {
                            newVel.y = force.y + currentVel.y;
                        }
                        else if (force.y != 0)
                        {
                            newVel.y = force.y;
                        }
                        //turn off navmeshAgent to let the enemy leave the ground

                        other.GetComponent<NavMeshAgent>().enabled = false;                       
                        other.GetComponent<Rigidbody>().velocity = newVel;
                        other.GetComponent<Rigidbody>().useGravity = true;
                        
                    }
                }
                if (other.tag == "hurtbox" || other.tag == "ground")
                {
                    //spawn explosion visual
                    Instantiate(impact, this.transform.position, Quaternion.identity);
                    hasCollided = true;
                    Destroy(this.gameObject);
                }
            }         
        }
    }

}
