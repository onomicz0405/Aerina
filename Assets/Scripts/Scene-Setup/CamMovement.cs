using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class CamMovement : MonoBehaviour
{    
    [Header("Settings")]
    [Tooltip("Horizontal Mouse Sensitivity")] public float sensitivityX = 1;
    [Tooltip("Vertical Mouse Sensitivity")] public float sensitivityY = 1;
    [Tooltip("Sensitivity of turn binds")] public float Yaw = 200;

    [HideInInspector] public float tilt;
    [HideInInspector] public GameManager GM;
    Vector3 Angles;

    private void Awake()
    {
        GM = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    void LateUpdate()
    {
        if (!GM.pauseInputs)
        {
            //turn binds
            float turnLeft = Convert.ToInt32(Input.GetKey(GM.turnLeft)) * Yaw * Time.fixedDeltaTime;
            float turnRight = Convert.ToInt32(Input.GetKey(GM.turnRight)) * Yaw * Time.fixedDeltaTime;
            //mouse input
            float rotationY = Input.GetAxisRaw("Mouse Y") * sensitivityY;
            float rotationX = Input.GetAxisRaw("Mouse X") * sensitivityX - turnLeft + turnRight;

            if (rotationY > 0)
            {
                Angles = new Vector3(Mathf.MoveTowards(Angles.x, -90, rotationY), Angles.y + rotationX, 0);
            }
            else
            {
                Angles = new Vector3(Mathf.MoveTowards(Angles.x, 90, -rotationY), Angles.y + rotationX, 0);
            }
            //set camera rotation
            transform.localEulerAngles = new Vector3(Angles.x, Angles.y, tilt);
        }       
    }   
}
