using UnityEngine;

public interface IAttack
{
    bool CanExecute();
    void Execute();
    float CooldownRemaining { get; }
}
