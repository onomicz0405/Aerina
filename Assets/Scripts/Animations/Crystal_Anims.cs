using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crystal_Anims : MonoBehaviour
{
    float spinSpeed = 100;
    float fastSpinSpeed = 450;
    float currentSpinSpeed;
    float spinFastLength = 1f;
    bool slowDownLerp;
    float lerpSpeed = 2;
    Coroutine currentCoroutine;

    public GameObject crystal_aura;
    public Material aura_mat;
    Material crystal_mat;
    float baseAlpha;
    float alphaFadeSpeed = 1.5f;
    float strength = 2;
    float intensity = 10000f; //lol
    float baseStrength;
    Color baseIntensity;

    float bobTarget = 0.2f;
    Vector3 basePos;
    float time = 1;
    public float speed = 1f;
    int direction = 1;

    private void Awake()
    {
        //set values
        crystal_mat = GetComponent<MeshRenderer>().material;
        aura_mat = crystal_aura.GetComponent<MeshRenderer>().material;
        baseAlpha = aura_mat.GetFloat("_alpha");
        aura_mat.SetFloat("_alpha", 0);
        baseStrength = crystal_mat.GetFloat("_Parallax_Strength");
        baseIntensity = crystal_mat.GetColor("_emission");
        currentSpinSpeed = spinSpeed;
        basePos = transform.localPosition;
    }
    public void ResetValues()
    {
        crystal_mat = GetComponent<MeshRenderer>().material;
        aura_mat = crystal_aura.GetComponent<MeshRenderer>().material;
        aura_mat.SetFloat("_alpha", 0);
        currentSpinSpeed = spinSpeed;
    }
    void Update()
    {
        //vfx fade out
        crystal_mat.SetFloat("_Parallax_Strength", Mathf.Lerp(crystal_mat.GetFloat("_Parallax_Strength"), baseStrength, 2 * Time.deltaTime));
        crystal_mat.SetColor("_emission", Vector4.Lerp(crystal_mat.GetColor("_emission"), baseIntensity, 5 * Time.deltaTime));
        aura_mat.SetFloat("_alpha", Mathf.Lerp(aura_mat.GetFloat("_alpha"), 0, alphaFadeSpeed * Time.deltaTime));
        if (aura_mat.GetFloat("_alpha") < 0.1f)
        {
            aura_mat.SetFloat("_alpha", 0);
        }
        //slowing lerp
        if (slowDownLerp)
        {
            currentSpinSpeed = Mathf.Lerp(currentSpinSpeed, spinSpeed, lerpSpeed * Time.deltaTime);
            if (currentSpinSpeed - spinSpeed < 1)
            {
                currentSpinSpeed = spinSpeed;
                slowDownLerp = false;
            }
        }
        //idle spin
        transform.localEulerAngles += new Vector3(0, 0, currentSpinSpeed * Time.deltaTime);
        //idle bob
        if (time >= 1)
        {
            direction = -1;
        }
        else if (time <= -1)
        {
            direction = 1;
        }
        time += speed * Time.deltaTime * direction;
        transform.localPosition = new Vector3(basePos.x, basePos.y + bobTarget*Mathf.Sin(time), basePos.z);
    }

    public void CastSpellAnim()
    {
        //stop overlapping corountines
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        //spin
        currentCoroutine = StartCoroutine(SpinFast());
    }

    IEnumerator SpinFast()
    {
        //set start values
        slowDownLerp = false;
        currentSpinSpeed = fastSpinSpeed;
        //vfx
        crystal_mat.SetFloat("_Parallax_Strength", strength);
        crystal_mat.SetColor("_emission", baseIntensity * intensity);
        aura_mat.SetFloat("_alpha", baseAlpha);
        yield return new WaitForSeconds(spinFastLength);
        //set end values
        slowDownLerp = true;
    }
}
