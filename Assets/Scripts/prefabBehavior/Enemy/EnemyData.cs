using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ENEMYTYPE { PURSUER, MAGE };

public class EnemyData : MonoBehaviour
{
    public Transform HealthBar;
    public Transform Bar_Parent;
    public Transform cam;

    public ENEMYTYPE enemyType;
    public float health = 200;
    public float max_health = 200;
    RaycastHit slopeInfo;
    int groundLayerNum = (1 << 9);
    float healthBarScale;

    private void Awake()
    {
        healthBarScale = HealthBar.localScale.x;
        Bar_Parent.gameObject.SetActive(false);
    }

    void Update()
    {
        //HealthBar UI
        if (health < max_health)
        {
            Bar_Parent.gameObject.SetActive(true);
        }
        HealthBar.localScale = new Vector3((health / max_health) * healthBarScale, HealthBar.localScale.y, HealthBar.localScale.z);
        Bar_Parent.rotation = Quaternion.LookRotation(Bar_Parent.position-cam.position, Vector3.up);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //grounded
        if (collision.transform.tag == "ground")
        {

            Physics.Raycast(transform.position, Vector3.down, out slopeInfo, Mathf.Infinity, groundLayerNum, QueryTriggerInteraction.Ignore);
            if (slopeInfo.collider != null)
            {
                if (collision.collider == slopeInfo.collider && slopeInfo.normal.normalized.y >= 0.7f)
                {
                    GetComponent<Rigidbody>().useGravity = false;
                    GetComponent<Rigidbody>().velocity *= 0;
                }
            }
            
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.transform.tag == "ground")
        {
            GetComponent<Rigidbody>().useGravity = true;
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            EnemyDeath();
        }
    }

    public void EnemyDeath()
    {
        //play death anim
        //wait for anim to finish
        Destroy(this.gameObject);
    }

    public void FrictionChange(Collider x, PhysicMaterial y)
    {
        StartCoroutine(WaitForFriction(x, y));
    }

    IEnumerator WaitForFriction(Collider x, PhysicMaterial y)
    {
        yield return new WaitForSecondsRealtime(0.5f);

        x.material = y;
    }
}
