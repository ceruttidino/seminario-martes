using UnityEngine;

public class OwlStalkState : IEnemyState
{
    private readonly Transform player;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly VanishingOwl owl;

    public OwlStalkState(Transform player, Transform self, EnemyBehaviour behaviour, VanishingOwl owl)
    {
        this.player = player;
        this.self = self;
        this.behaviour = behaviour;
        this.owl = owl;
    }

    public void Enter() => owl.EnterStealth();

    public void Tick()
    {
        if (player == null) return;

        owl.MoveTowards(player.position);

        if (Vector2.Distance(self.position, player.position) <= owl.RevealDistance)
            behaviour.SetState(new OwlWindupState(player, self, behaviour, owl));
    }

    public void Exit() { }
}

public class OwlWindupState : IEnemyState
{
    private readonly Transform player;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly VanishingOwl owl;

    private float timer;

    public OwlWindupState(Transform player, Transform self, EnemyBehaviour behaviour, VanishingOwl owl)
    {
        this.player = player;
        this.self = self;
        this.behaviour = behaviour;
        this.owl = owl;
    }

    public void Enter()
    {
        owl.Reveal();
        owl.Stop();
        owl.FaceTarget(player.position);
        owl.TriggerAttack();
        timer = owl.WindupTime;
    }

    public void Tick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            behaviour.SetState(new OwlAttackState(player, self, behaviour, owl));
    }

    public void Exit() { }
}

public class OwlAttackState : IEnemyState
{
    private readonly Transform player;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly VanishingOwl owl;

    private float timer;

    public OwlAttackState(Transform player, Transform self, EnemyBehaviour behaviour, VanishingOwl owl)
    {
        this.player = player;
        this.self = self;
        this.behaviour = behaviour;
        this.owl = owl;
    }

    public void Enter()
    {
        timer = owl.RecoverTime;

        if (Vector2.Distance(self.position, player.position) <= owl.AttackRadius
            && player.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(owl.HeartDamage);
        }
    }

    public void Tick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            behaviour.SetState(new OwlStalkState(player, self, behaviour, owl));
    }

    public void Exit() { }
}