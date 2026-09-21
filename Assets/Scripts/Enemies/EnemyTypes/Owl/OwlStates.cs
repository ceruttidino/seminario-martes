using UnityEngine;

public class OwlInvisibleState : IEnemyState
{
    private readonly Transform player;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly VanishingOwl owl;
    private float timer;

    public OwlInvisibleState(Transform player, Transform self, EnemyBehaviour behaviour, VanishingOwl owl)
    {
        this.player = player;
        this.self = self;
        this.behaviour = behaviour;
        this.owl = owl;
    }

    public void Enter()
    {
        owl.EnterInvisible();
        timer = owl.InvisibleDuration;
    }

    public void Tick()
    {
        if (player == null) return;

        timer -= Time.deltaTime;

        owl.MoveTowards(player.position);

        if (Vector2.Distance(self.position, player.position) <= owl.RevealDistance)
        {
            behaviour.SetState(new OwlWindupState(player, self, behaviour, owl));
            return;
        }

        if (timer <= 0f)
            behaviour.SetState(new OwlShadowState(player, self, behaviour, owl));
    }

    public void Exit() { }
}

public class OwlShadowState : IEnemyState
{
    private readonly Transform player;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly VanishingOwl owl;
    private float timer;

    public OwlShadowState(Transform player, Transform self, EnemyBehaviour behaviour, VanishingOwl owl)
    {
        this.player = player;
        this.self = self;
        this.behaviour = behaviour;
        this.owl = owl;
    }

    public void Enter()
    {
        owl.EnterShadow();
        timer = owl.ShadowChaseDuration;
    }

    public void Tick()
    {
        if (player == null) return;

        timer -= Time.deltaTime;

        owl.MoveTowards(player.position);

        if (Vector2.Distance(self.position, player.position) <= owl.RevealDistance)
        {
            behaviour.SetState(new OwlWindupState(player, self, behaviour, owl));
            return;
        }

        if (timer <= 0f)
            behaviour.SetState(new OwlInvisibleState(player, self, behaviour, owl));
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
            behaviour.SetState(new OwlInvisibleState(player, self, behaviour, owl));
    }

    public void Exit() { }
}

public class OwlRevealedState : IEnemyState
{
    private readonly Transform player;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly VanishingOwl owl;
    private float timer;

    public OwlRevealedState(Transform player, Transform self, EnemyBehaviour behaviour, VanishingOwl owl)
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
        timer = owl.RevealedSpawnDuration;
    }

    public void Tick()
    {
        timer -= Time.deltaTime;

        if (player != null) owl.FaceTarget(player.position);

        if (timer <= 0f)
            behaviour.SetState(new OwlShadowState(player, self, behaviour, owl));
    }

    public void Exit() { }
}