using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class EnemyPathfinding : MonoBehaviour
{
    EnemyData enemyData;
    NavMeshAgent navMeshAgent;
    public float Desired_Range;
    public Transform player;
    RaycastHit slopeInfo;
    int groundLayerNum = (1 << 9);
    public float EnemyLength;
    float oscillation;
    bool oscillation_direction = true; //true is positive
    public float oscillation_rate;
    public float strafe_range;
    public bool canReActivate = true;
    public float trueAccel;


    private void Awake()
    {

        enemyData = GetComponent<EnemyData>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        switch (enemyData.enemyType)
        {
            case ENEMYTYPE.PURSUER:
                navMeshAgent.speed = 15;
                navMeshAgent.acceleration = 40;
                navMeshAgent.autoBraking = false;
                Desired_Range = .5f;
                break;
            case ENEMYTYPE.MAGE:
                navMeshAgent.speed = 10;
                navMeshAgent.acceleration = 10;
                navMeshAgent.autoBraking = false;
                oscillation_rate = 1f;
                strafe_range = 5f;
                Desired_Range = 5f;
                break;
        }
        trueAccel = navMeshAgent.acceleration;
        oscillation = Random.Range(-1,1);
    }

    void FixedUpdate()
    {
        if (oscillation_direction)
        {
            oscillation = Mathf.Clamp(oscillation + oscillation_rate * Time.deltaTime, -1, 1);
            if (oscillation == 1)
            {
                oscillation_direction = false;
            }
        }
        else
        {
            oscillation = Mathf.Clamp(oscillation - oscillation_rate * Time.deltaTime, -1, 1);
            if (oscillation == -1)
            {
                oscillation_direction = true;
            }
        }

        if (navMeshAgent.enabled)
        {
            Vector3 newDes;
            RaycastHit groundInfo;           
            if (Physics.Raycast(player.transform.position, Vector3.down, out groundInfo, Mathf.Infinity, groundLayerNum, QueryTriggerInteraction.Ignore))
            {
                switch (enemyData.enemyType)
                {
                    case ENEMYTYPE.PURSUER:
                        newDes = (transform.position + new Vector3(0,-navMeshAgent.height / 2,0) - groundInfo.point).normalized * Desired_Range + groundInfo.point;
                        navMeshAgent.SetDestination(newDes);
                        break;
                    case ENEMYTYPE.MAGE:
                        newDes = (transform.position + new Vector3(0, -navMeshAgent.height / 2, 0) - groundInfo.point).normalized * Desired_Range + groundInfo.point;
                        float offset = Mathf.Sin(oscillation * Mathf.PI / 2);
                        newDes += Vector3.Cross((groundInfo.point + new Vector3(0, -navMeshAgent.height / 2, 0) - transform.position).normalized, Vector3.up).normalized * strafe_range * offset;
                        navMeshAgent.SetDestination(newDes);
                        break;
                }
            }           
        }
        
    }

    void Update()
    {       
        //check if grounded
        if (Physics.Raycast(transform.position, Vector3.down, out slopeInfo, EnemyLength, groundLayerNum, QueryTriggerInteraction.Ignore) 
            && GetComponent<Rigidbody>().velocity.y < 0 && !navMeshAgent.enabled)
        {
            navMeshAgent.enabled = true;
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Rigidbody>().velocity *= 0;
        }
    }

}
