using Unity.VisualScripting;
using UnityEngine;

public class BaseTrap : MonoBehaviour
{

    private enum TrapState
    {
        Hidden, Active, InBetween
    }
    private enum TrapType
    {
        AlwaysActive, Timed, StepOnActive
    }
    [SerializeField] TrapType trapType;
    [SerializeField] int damage = 1;
    [SerializeField] int trapStateCycleTime;  // tiempo que la trampa pasa desactivada/activada
    float timer = 0;
    [SerializeField] TrapState trapState;
    [SerializeField] Sprite deactiveSprite;
    [SerializeField] Sprite inBetweenSrpite;
    [SerializeField] Sprite activeSprite;
    SpriteRenderer spriteRenderer;
    bool touching = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        DrawTrap();
        if (trapType == TrapType.AlwaysActive)
        {
            trapState = TrapState.Active;
        }
        if (trapType == TrapType.Timed)
        {
            timer += Time.deltaTime;

            if (timer > trapStateCycleTime) { CycleState(); timer = 0; }
        }
        if (trapType == TrapType.StepOnActive) 
        {
            if (touching)
            {
                spriteRenderer.color = new Color(1, 1, 1, 1);
                trapState = TrapState.Active;
            }
            else
            {
                spriteRenderer.color = new Color(1, 1, 1, 0.4f);
                trapState = TrapState.Hidden;
            }
        }


    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerHealth>() != null) 
        {
            PlayerHealth PH = other.GetComponent<PlayerHealth>();
            touching = true;
            if (trapState == TrapState.Active && PH != null || touching)
            {
                PH.PlayerGetHurt(damage);
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        touching = false;
    }

    private void CycleState()
        {
            if (trapState == TrapState.Hidden) { trapState = TrapState.InBetween; Debug.Log(trapState); return; }
            if (trapState == TrapState.InBetween) { trapState = TrapState.Active; Debug.Log(trapState); return; }
            if (trapState == TrapState.Active) { trapState = TrapState.Hidden; Debug.Log(trapState); return; }

        }
    private void CycleState(TrapState ts)
    {
        trapState = ts;
    }

    private void DrawTrap()
    {
        if (trapState == TrapState.Hidden) { spriteRenderer.sprite = deactiveSprite; }
        if (trapState == TrapState.InBetween) { spriteRenderer.sprite = inBetweenSrpite; }
        if (trapState == TrapState.Active) { spriteRenderer.sprite = activeSprite; }
    }

    private void DeactivateTrap()
    {
        touching = false;
    }
}





