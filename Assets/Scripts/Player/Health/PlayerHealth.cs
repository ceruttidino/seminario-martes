using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PlayerHealth : MonoBehaviour
{
    [Header("regular Health")]
    [SerializeField] int playerHealthCap = 20;
    [SerializeField] int playerMaxHealth = 6;
    [SerializeField] int playerHealth = 6;

    [Header("Special Hearts")] //en caso de no haber, remover esa seccion
    //[SerializeField] int ShieldHearts = 0;
    //Agregar mas tipos de vida si los hay

    [Header("Other")]
    [SerializeField] bool canGetHurt = true;
    [SerializeField] float invulTime = 1.5f;

    [Header("UI")]
    [SerializeField] GameObject[] hearts;

    void Start()
    {
        UpdateMaxHearts();
        UpdateHearts(playerHealth);
    }

    void Update()
    {
        if (playerHealth > playerMaxHealth) { playerHealth = playerMaxHealth; }
    }

    public void Heal(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }
        PlayerHeal();
    }
    public void Hurt(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }
        PlayerGetHurt();
    }

    public void AddHeart(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }
        PlayerAddHeart(false);
    }

    public void PlayerGetHurt() 
    { 
        if (canGetHurt) 
        {
            playerHealth -= 1;
            UpdateHearts(playerHealth);
            Invoke("DesInvul", invulTime);
        }

    }

    public void PlayerGetHurt(int hurt)
    {
        if (canGetHurt) 
        {
            playerHealth -= hurt;
            UpdateHearts(playerHealth);
            Invoke("DesInvul", invulTime);
        }
    }

    public void PlayerAddHeart(bool isFull) 
    { 
        playerMaxHealth += 2;
        if (playerMaxHealth > playerHealthCap) { playerMaxHealth = playerHealthCap; }
        if (isFull == true) { PlayerHeal(2); }
        UpdateMaxHearts();
        UpdateHearts(playerHealth);
    }


    public void PlayerAddHeart(int add, bool isFull) //siempre sera multiplicado por 2 para representar corazones enteros
    {
        playerMaxHealth += add * 2;
        if (playerMaxHealth > playerHealthCap) { playerMaxHealth = playerHealthCap; }
        if (isFull == true) { PlayerHeal(2 * add); }
        UpdateMaxHearts();
        UpdateHearts(playerHealth);
    }

    public void PlayerHeal() { if (playerHealth < playerMaxHealth) { playerHealth += 1; UpdateHearts(playerHealth); } }

    public void PlayerHeal(int heal) 
    {
        if (playerHealth < playerMaxHealth) 
        {
            playerHealth += heal; 
            if (playerHealth > playerMaxHealth) { playerHealth = playerMaxHealth; }
            UpdateHearts(playerHealth);
        }
    }

    private void DesInvul()
    {
        canGetHurt = true;
    }

    private void UpdateMaxHearts()
    {
        for (int i = 0; i < playerMaxHealth / 2; i++)
        {
            hearts[i].SetActive(true);
        }
    }
    private void UpdateHearts(int healthAmmount)
    {
        for (int i = 0; i < playerMaxHealth / 2; i++) // a futur actualizar a los sprites de corazon
        {
            if (healthAmmount > 1)
            {
                hearts[i].GetComponent<UnityEngine.UI.Image>().color = Color.green;
                healthAmmount -= 2;
            }
            else if (healthAmmount == 1)
            {
                hearts[i].GetComponent<UnityEngine.UI.Image>().color = Color.red;
                healthAmmount -= 1;
            }
            else { hearts[i].GetComponent<UnityEngine.UI.Image>().color = Color.black; }
        }
    }



    
}
