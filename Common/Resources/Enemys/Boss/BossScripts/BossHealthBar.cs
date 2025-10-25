using Godot;
using System;

public partial class BossHealthBar : TextureProgressBar
{
    public override void _Process(double delta)
    {
        base._Process(delta);
        Value = GetNode<EnemyStats>("%BossStats").CurrentHealth;
    }
}
