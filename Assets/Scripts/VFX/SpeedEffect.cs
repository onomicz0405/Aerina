using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeedEffect : MonoBehaviour
{
    int time;
    public List<Sprite> Sprites = new List<Sprite>();
    private void Awake()
    {
        StartCoroutine(TickRate());
    }

    IEnumerator TickRate()
    {
        while (this.gameObject.activeInHierarchy == true)
        {
            GetComponent<Image>().sprite = Sprites[time];
            yield return new WaitForSeconds(1f / 7.5f);
            //time++;
            if (time > 3)//frame amount - 1
            {
                time = 0;
            }
        }
    }
}
