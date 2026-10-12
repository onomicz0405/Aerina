using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketBehavior : MonoBehaviour
{
    Rigidbody rb;
    public LayerMask layerMask;
    public LayerMask layerMask2;
    RaycastHit info;
    Collider[] explosionInfo;
    public GameObject explosion;
    GameObject currentExplosion;
    public float explosionForce;
    public float explosionRadius;
    public float ballMultiplier;
    public float enemyMultiplier;
    public float pushUpScale;
    public PhysicMaterial friction;
    public PhysicMaterial noFriction;
    public float RL_damage;
    public float gravityAdd;
    bool hasCollided = false;
    Collider[] colliders;
    List<Vector3> positionalDifference = new List<Vector3>();
    void Awake()
    {
       rb = this.gameObject.GetComponent<Rigidbody>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!hasCollided)
        {
            
            if (other.tag == "ground")
            {
                //spawn explosion visual
                currentExplosion = Instantiate(explosion, other.ClosestPointOnBounds(transform.position), Quaternion.identity);
                hasCollided = true;
                explosionInfo = Physics.OverlapSphere(other.ClosestPointOnBounds(transform.position), explosionRadius, layerMask2, QueryTriggerInteraction.Ignore);
                if (explosionInfo.Length > 0)
                {
                    colliders = explosionInfo;
                    for (int x = 0; x < explosionInfo.Length; x++)
                    {
                        positionalDifference.Add((explosionInfo[x].transform.position - other.ClosestPointOnBounds(transform.position)) / 2);
                    }
                }
                Explode();
            }
        }
    }

    private void Update()
    {
        //unique gravity
        rb.velocity += new Vector3(0, -gravityAdd, 0) * Time.deltaTime;
    }
    void Explode()
    {
        //checks if there are any usable colliders within the explosion radius
        if (explosionInfo.Length > 0)
        {
            float regularEF = explosionForce;
            for (int x = 0; x < explosionInfo.Length; x++)
            {
                explosionForce = regularEF;
                Vector3 force;
                if (colliders[x].tag == "Player")
                {
                    if (colliders[x].GetComponent<Movement>().isJump)
                    {
                        explosionForce *= 1.5f;
                    }
                }
                else if (colliders[x].tag == "Ball")
                {
                    colliders[x] = colliders[x].GetComponent<BallLogic>().parent.GetComponent<SphereCollider>();
                    explosionForce *= ballMultiplier;
                }
                else if (colliders[x].tag == "hurtbox")
                {
                    explosionForce *= enemyMultiplier;

                    colliders[x].material = noFriction;
                    colliders[x].GetComponent<EnemyData>().FrictionChange(colliders[x], friction);
                    colliders[x].GetComponent<EnemyData>().TakeDamage(RL_damage);
                }
                //apply force relative to distance from rocket impact point
                if (positionalDifference[x].magnitude < 1.5f)
                {
                    force = (positionalDifference[x].normalized * explosionForce);
                    if (colliders[x].tag == "Ball" || colliders[x].tag == "hurtbox")
                    {
                        force += (Vector3.up * pushUpScale);
                        if (positionalDifference[x].y < 0)
                        {
                            force += (Vector3.up * pushUpScale);
                        }
                    }
                }
                else if (positionalDifference[x].magnitude == 0)
                {
                    force = (Vector3.up * explosionForce);
                    if (colliders[x].tag == "Ball" || colliders[x].tag == "hurtbox")
                    {
                        force += (Vector3.up * pushUpScale);
                    }
                }
                else
                {
                    float scale = (3 - positionalDifference[x].magnitude) / 1.5f;
                    if (scale < 0.5f)
                    {
                        scale = 0.5f;
                    }
                    force = positionalDifference[x].normalized * explosionForce * scale;
                }

                //interia
                Vector3 currentVel = colliders[x].GetComponent<Rigidbody>().velocity;
                Vector3 newVel = new Vector3(0, 0, 0);

                //if the currentVel and the force of the explosion are going the same direction, add them together, else, just set it equal to the force; check for each axis
                if (currentVel.x * force.x > 0)
                {
                    newVel.x = currentVel.x + force.x;
                }
                else
                {
                    newVel.x = force.x;
                }

                if (currentVel.z * force.z > 0)
                {
                    newVel.z = currentVel.z + force.z;
                }
                else
                {
                    newVel.z = force.z;
                }

                if (currentVel.y * force.y > 0)
                {
                    newVel.y = currentVel.y + force.y;
                }
                else
                {
                    newVel.y = force.y;
                }
                //extra player behavior
                if(colliders[x].tag == "Player")
                {
                    if (force.y > 0)
                    {
                        colliders[x].GetComponent<Movement>().isJump = true;                        
                    }
                    if (force.normalized.y < 0.2f && colliders[x].GetComponent<Movement>().grounded)
                    {
                        newVel = new Vector3(newVel.x, rb.velocity.y, newVel.z);

                    }
                }      
                
                //apply new velocity
                colliders[x].GetComponent<Rigidbody>().velocity = newVel;            
            }           
        }

        Destroy(currentExplosion, 0.8f);
        Destroy(this.gameObject);
    }

    IEnumerator WaitForExplode()
    {
        yield return new WaitForSeconds(0.033f);
        Explode();
    }

    
}
