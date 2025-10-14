using Godot;
using PlayerScript.PlayerInventory;
using System;

public partial class AdditemTemp : Node
{
    private ItemInstance _item_instance_test_ref;

    public override void _Ready()
    {
        base._Ready();
        _item_instance_test_ref = ResourceLoader.Load<ItemInstance>("res://_Debug/TestItemInstance.tres");
    }


    private void OnButtonPressed()
    {
        GD.Print("Botao clicado....");
        Player.Instance.GetNode<PlayerInventory>("/root/Player/Inventory").AddItem(_item_instance_test_ref);
    }
}
