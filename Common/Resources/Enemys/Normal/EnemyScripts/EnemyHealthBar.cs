using Godot;
using System;

public partial class EnemyHealthBar : TextureProgressBar
{
    public override void _Process(double delta)
    {
        base._Process(delta);
        Value = GetNode<EnemyStats>("%EnemyStats").CurrentHealth;
    }
}
