using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayVisual : MonoBehaviour
{
    public float delay;
    void Awake()
    {
        if (TryGetComponent<ParticleSystemRenderer>(out ParticleSystemRenderer PSR))
        {
            GetComponent<ParticleSystemRenderer>().enabled = false;
        }
        if (TryGetComponent<TrailRenderer>(out TrailRenderer TR))
        {
            GetComponent<TrailRenderer>().enabled = false;
        }
        StartCoroutine(Delay());
    }

    IEnumerator Delay()
    {
        yield return new WaitForSeconds(delay);
        if (TryGetComponent<ParticleSystemRenderer>(out ParticleSystemRenderer PSR))
        {
            GetComponent<ParticleSystemRenderer>().enabled = true;
        }
        if (TryGetComponent<TrailRenderer>(out TrailRenderer TR))
        {
            GetComponent<TrailRenderer>().enabled = true;
        }
    }
}
