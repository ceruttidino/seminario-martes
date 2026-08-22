using UnityEngine;

public class RatChaseState : IEnemyState
{
    private Transform player;
    private EnemyMovement movement;
    private Transform enemy;
    private EnemyBehaviour behaviour;
    private RegeneratingRat rat;

    public RatChaseState(Transform player, EnemyMovement movement, Transform enemy, EnemyBehaviour behaviour, RegeneratingRat rat)
    {
        this.player = player;
        this.movement = movement;
        this.enemy = enemy;
        this.behaviour = behaviour;
        this.rat = rat;
    }

    public void Enter(){ }
    
    public void Tick()
    {
        if (player == null)
        {
            Debug.LogError("PLAYER NULL");
            return;
        }

        Vector2 toPlayer = player.position - enemy.position;
        float distance = toPlayer.magnitude;
        float stopDistance = rat.AttackTriggerRange;

        if (distance <= stopDistance)
        {
            movement.Move(Vector2.zero);
            behaviour.SetState(new RatAttackState(player, movement, enemy, behaviour, rat));
            return;
        }

        // Siempre a velocidad completa (sin frenado gradual): un multiplicador que
        // se acerca a 0 a medida que distance -> stopDistance hacia un jugador
        // quieto genera una desaceleracion exponencial que nunca llega a cruzar
        // el umbral en la practica (la rata se "cuelga" justo afuera del rango de
        // ataque para siempre). Full speed hasta cruzar el umbral evita ese caso.
        movement.Move(toPlayer.normalized);
    }

    public void Exit() { }
}
