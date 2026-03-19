using UnityEngine;

public interface IAttack
{
    bool CanExecute(); //attack can or cant be executed
    void Execute(); //what happens when is executed
    float CooldownRemaining {  get; } //cooldown
}
