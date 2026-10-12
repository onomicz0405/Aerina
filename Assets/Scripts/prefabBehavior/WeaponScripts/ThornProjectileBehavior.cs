using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThornProjectileBehavior : MonoBehaviour
{
    Rigidbody rb;
    bool hasCollided = false;
    public GameObject impact;
    public GameObject Thorn_AOE;
    public float damage;
    public float radius;
    int groundLayerNum = (1 << 9);
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
                    //restrain in thorns                    
                }
                if (other.tag == "hurtbox" || other.tag == "ground")
                {
                    //spawn explosion visual
                    Instantiate(impact, this.transform.position, Quaternion.identity).transform.localScale *= 3;
                    hasCollided = true;
                    //if close enough to the ground, spawn on the ground below them, if not, just restrain them in the air
                    RaycastHit groundInfo = new RaycastHit();
                    Physics.Raycast(this.transform.position, Vector3.down, out groundInfo, Mathf.Infinity, groundLayerNum, QueryTriggerInteraction.Ignore);
                    if ((this.transform.position - groundInfo.point).magnitude <= radius)
                    {
                        //spawn aoe effect
                        Instantiate(Thorn_AOE, groundInfo.point + new Vector3(0, 0.01f, 0), Quaternion.identity).transform.localScale *= (radius * 2);
                    }
                    Destroy(this.gameObject);
                }
                

            }            
        }
    }
}
