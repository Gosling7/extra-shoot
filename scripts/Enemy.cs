using ExtraShoot.scripts.Interfaces;
using Godot;
using System;

namespace ExtraShoot.scripts;

public partial class Enemy : CharacterBody3D, IDamageable
{
    [Export]
    public int MaxHealth { get; set; } = 30;

    private int _health;
    private ProgressBar _healthbar;

    public override void _Ready()
    {
        _health = MaxHealth;
        _healthbar = GetNode<ProgressBar>("HealthBarRoot/SubViewport/HealthBarUI/ProgressBar");
        _healthbar.MaxValue = MaxHealth;
        _healthbar.Value = _health;
    }


    public void TakeDamage(int amount)
    {
        _health -= amount;
        _healthbar.Value = _health;

        if (_health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        GD.Print("Daed");

        QueueFree();
    }
}
