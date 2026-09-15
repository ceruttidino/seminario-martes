using System.Collections;
using UnityEngine;

public class DiggingPileBlink : MonoBehaviour
{
    private SpriteRenderer sr;

    private float flashtimer = 0;

    [SerializeField] float flashDuratiron = 1;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();


    }
    private void Update()
    {
        flashtimer += Time.deltaTime;

        float ppp = Mathf.PingPong(flashtimer, flashDuratiron);

        sr.color = new Color(sr.color.r,sr.color.g,sr.color.b,ppp);
    }
}
