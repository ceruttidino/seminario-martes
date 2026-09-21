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
    [SerializeField] int trapStateCycleTime;
    float timer = 0;
    [SerializeField] TrapState trapState;
    [SerializeField] Sprite deactiveSprite;
    [SerializeField] Sprite inBetweenSrpite;
    [SerializeField] Sprite activeSprite;
    SpriteRenderer spriteRenderer;
    bool touching = false;

    [Header("Damage Tick")]
    [SerializeField] float damageTickInterval = 1f; // segundos entre golpes mientras estás adentro
    float lastDamageTime = -999f;
    PlayerHealth playerInside;

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

        // Daño continuo: tickea mientras el player esté adentro y la trampa esté activa.
        if (playerInside != null && trapState == TrapState.Active
            && Time.time >= lastDamageTime + damageTickInterval)
        {
            playerInside.PlayerGetHurt(damage);
            lastDamageTime = Time.time;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerFeetMarker>() == null)
            return;

        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            playerInside = ph;
            touching = true;
            lastDamageTime = -999f;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerFeetMarker>() == null)
            return;

        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == playerInside)
        {
            playerInside = null;
            touching = false;
        }
    }

    private void CycleState()
    {
        if (trapState == TrapState.Hidden) { trapState = TrapState.InBetween; return; }
        if (trapState == TrapState.InBetween) { trapState = TrapState.Active; return; }
        if (trapState == TrapState.Active) { trapState = TrapState.Hidden; return; }
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
        playerInside = null;
    }
}