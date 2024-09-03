using Godot;
using System;

public partial class Inventory : Node
{

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   INVENTORY CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

    private ItemData _item_data;
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

    public ItemData Item_Data
    {
        get
        {
            return _item_data;
        }

        set
        {
            _item_data = value;
        }
    }


    public override void _Ready()
    {
        
    }

}
