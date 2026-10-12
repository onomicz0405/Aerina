using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailgunLaser : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(LaserAnimation(0.5f));
        
    }

    IEnumerator LaserAnimation(float t)
    {
        LineRenderer gradient = GetComponent<LineRenderer>();

        float y = t * 60;
        float a = 1.0f;
        for (float x = 1; a > 0; x++)
        {

            gradient.endColor = new Color(0, 0, 0, a);
            a = (-Mathf.Pow(1/y,2) * Mathf.Pow(x,2) + 1);
            yield return new WaitForFixedUpdate();
        }
        
    }
}
