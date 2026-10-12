using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlowOscillate : MonoBehaviour
{
    float time = 1;
    public float speed = 1;
    int direction = 1;
    // Update is called once per frame
    void Update()
    {
        if (time >= 1)
        {
            direction = -1;            
        }
        else if (time <= .1f)
        {
            direction = 1;
        }
        time += speed * Time.deltaTime * direction;
        GetComponent<MeshRenderer>().material.SetFloat("_GlowTime", Mathf.Sin(time));       
    }
}
