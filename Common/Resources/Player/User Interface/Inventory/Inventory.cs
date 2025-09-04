using Godot;
using System;

public partial class Inventory : Node
{

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   INVENTORY CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

    private Item[] _inventory_items;

    public Item[] Inventory_Items
    {
        get
        {
            return _inventory_items;
        }

        set
        {
            _inventory_items = value;
        }
    }

    public override void _Ready()
    {
        
    }

}
