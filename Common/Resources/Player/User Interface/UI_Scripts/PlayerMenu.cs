using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   PLAYER MENU
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Overlays the in-game pause/menu layer.
	**  2 - Toggles the menu visibility with the "Menu" input action.
	**  3 - Handles the Save & Quit button to return to the main menu.
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class PlayerMenu : CanvasLayer
{
    private InventoryMenu _inventory_menu;
    private bool _resume_menu_visible = true;
    private bool _resume_inventory_visible = false;

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();
        Visible = false;
        _inventory_menu = GetNodeOrNull<InventoryMenu>("/root/Player/Player_UI/Inventory_UI");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Input.IsActionJustPressed("Menu"))
        {
            Toggle_Menu();
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Menu Visibility
    //!---------------------------------------------------------------------------------------------------------

    public void Close_Menu()
    {
        Visible = false;
    }

    private void Toggle_Menu()
    {
        bool inventory_open = _inventory_menu != null && _inventory_menu.Visible;
        if (Visible == false && inventory_open == false)
        {
            Visible = _resume_menu_visible;
            if (_resume_inventory_visible && _inventory_menu != null && _inventory_menu.Visible == false)
            {
                _inventory_menu.Toggle(false);
            }
            return;
        }
        _resume_menu_visible = Visible;
        _resume_inventory_visible = inventory_open;
        if (inventory_open)
        {
            _inventory_menu.Close();
        }
        Visible = false;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Signal Handlers
    //!---------------------------------------------------------------------------------------------------------

    public void On_Inventory_Pressed()
    {
        if (_inventory_menu != null)
        {
            _inventory_menu.Toggle(false);
        }
    }

    public void On_Save_And_Quit_Pressed()
    {
        Save_Load_Control.Instance.Save_Game();
        Scene_Manager.Instance.Change_Scene(e_Game_Scenes.MainMenu);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
