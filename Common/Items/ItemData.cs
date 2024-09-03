using Godot;
using System;

public class ItemData
{
    /**-----------------------------------------------------------------------------------------------------------------------
*!                                                   ITEM DATA CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
 * *                                                     PURPOSE

 **     1- This is a class that just holds deserialized data loaded by the JsonLoader, nothing more. 
 ** This needs to exist because the data needs to be serialized and deserialized when game is saved and loaded
 ** or if any new items are introduced to the game.  
 *   
 *   
 *
 *-----------------------------------------------------------------------------------------------------------------------**/


    private string _name;
    private string _description;
    private int _amount;
    private int _value;
    private bool _consumable;
    private bool _is_equipment;


    public string Name
    {
        get 
        {
             return _name;
        }

        set
        {
            _name = value;
        }
    }

    public string Description
    {
        get
        {
            return _description;
        }

        set
        {
            _description = value; 
        }
    }

    public int Amount
    {
        get
        {
            return _amount;
        }

        set
        {
            _amount = value;
        }
    }

    public int Value
    {
        get
        {
            return _value;
        }

        set
        {
            _value = value;
        }
    }


    public ItemData(string p_name, string p_description, int p_amount, int p_value)
    {
        _name = p_name;
        _description = p_description;
        _amount = p_amount;
        _value = p_value;
    }
    public ItemData(string p_name, string p_description, int p_amount, int p_value, bool p_consumable)
    {
        _name = p_name;
        _description = p_description;
        _amount = p_amount;
        _value = p_value;
        _consumable = p_consumable;
    }
}
