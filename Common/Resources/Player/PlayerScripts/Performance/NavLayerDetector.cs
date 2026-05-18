using Godot;
using System;

public partial class NavLayerDetector : Area2D
{
    public override void _Ready()
    {
        // Programmatically bind the signals to prevent UI connection mistakes
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        // Test if the overlapping physics body is a TileMapLayer chunk
        if (body is TileMapLayer layer)
        {
            if (!layer.NavigationEnabled)
            {
                layer.NavigationEnabled = true;
                GD.Print($"[NavSystem] Player entered range of: {layer.Name}. Navigation ACTIVATED.");
            }
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is TileMapLayer layer)
        {
            if (layer.NavigationEnabled)
            {
                layer.NavigationEnabled = false;
                GD.Print($"[NavSystem] Player left range of: {layer.Name}. Navigation DEACTIVATED.");
            }
        }
    }
}