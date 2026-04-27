using TMPro.Examples;
using UnityEngine;

public class Mole : MonoBehaviour
{
    [SerializeField] LayerMask player;
    [SerializeField] PlayerHealth ph;
    [SerializeField] float yoinkRange = 2f;
    [SerializeField] bool canStealScrap;
    [SerializeField] int ammountScrapStolen;
    [SerializeField] bool canStealKey;


    private void Awake()
    {
        Steal();
    }

    void Update()
    {
        
    }

    void Steal()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, yoinkRange, player);
        if (hit != null)
        {
            Debug.Log("Yoink");
            if (hit.GetComponent<PlayerHealth>().CurrentHealth > 1) // si el jugador tiene mas de 1 de vida (1/2 corazon) lo dañanamos
            {
                hit.GetComponent<PlayerHealth>().PlayerGetHurt();
            }
            if (canStealScrap)
            {
                if (hit.GetComponent<PlayerScrap>().CurrentScrap >= ammountScrapStolen) //si puedo robar la cantidad que tengo marcada lo hago
                {
                    hit.GetComponent<PlayerScrap>().TrySpendScrap(ammountScrapStolen);
                }
                else { hit.GetComponent<PlayerScrap>().TrySpendScrap(hit.GetComponent<PlayerScrap>().CurrentScrap); } // sino robo lo que tiene
            }
            if (canStealKey) 
            {
                if (hit.GetComponent<PlayerKeys>().CurrentKeys > 0) { hit.GetComponent<PlayerKeys>().UseKey(); }
            }

        }
        else 
        {
            Debug.Log("noYoink");
        }
    }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, yoinkRange);
        } 
}
