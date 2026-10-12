using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EFFECT_TYPE { KNOCK_UP, KICK }

public class HitboxBehavior : MonoBehaviour
{
    public EFFECT_TYPE effectType;
    public float hitbox_damage;
    public Vector3 knockUpForce;
    public Vector3 kickForce;
    bool hasKicked = false;

    Collider Player;
    Transform cam;

    void Awake()
    {
        Player = GameObject.Find("Player").GetComponent<Collider>();
        cam = GameObject.Find("Main Camera").transform;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "hurtbox")
        {
            //apply damage
            other.GetComponent<EnemyData>().TakeDamage(hitbox_damage);     
            //apply effect
            switch (effectType)
            {
                case (EFFECT_TYPE.KNOCK_UP):
                    KnockUp(other, knockUpForce);
                    break;
                case (EFFECT_TYPE.KICK):
                    if (!hasKicked)
                    {
                        Kick(other);
                    }
                    break;
            }
        }
    }

    void KnockUp(Collider other, Vector3 force)
    {
        if (other.GetComponent<Rigidbody>() != null)
        {
            Vector3 currentVel = other.GetComponent<Rigidbody>().velocity;
            Vector3 newVel = new Vector3(0, 0, 0);
            //if the currentVel and the force of the source are going the same direction, add them together, else, just set it equal to the force; check for each axis
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

            if (other.tag == "Player")
            {
                other.GetComponent<Rigidbody>().velocity = newVel;
            }
            else
            {
                //turn off navmeshAgent to let the enemy leave the ground
                other.GetComponent<NavMeshAgent>().enabled = false;
                other.GetComponent<Rigidbody>().velocity = newVel;
                other.GetComponent<Rigidbody>().useGravity = true;
            }

            
        }
    }

    void Kick(Collider other)
    {
        hasKicked = true;
        //knock enemy back

        //knock player back
        Vector3 currentKickForce = new Vector3(cam.forward.x * kickForce.z, kickForce.y, cam.forward.z * kickForce.z);
        KnockUp(Player, currentKickForce);
        //destroy hitbox
        Destroy(gameObject);
    }
}
