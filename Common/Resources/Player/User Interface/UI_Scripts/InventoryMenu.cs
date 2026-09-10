using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   INVENTORY MENU
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Overlays the in-game inventory layer.
	**  2 - Starts hidden and toggles with the "Inventory" input action (keyboard I).
	**  3 - Exposes Open/Close methods so the Pause Menu can open/close the inventory.
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class InventoryMenu : CanvasLayer
{
    private Control[] _categoryLists;
    private Button[] _tabButtons;

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();
        Visible = false;
        Setup_Category_Tabs();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Input.IsActionJustPressed("Inventory"))
        {
            Toggle(true);
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Category Tabs
    //!---------------------------------------------------------------------------------------------------------

    private void Setup_Category_Tabs()
    {
        _categoryLists = new Control[]
        {
            GetNode<Control>("Panel/ScrollContainer - Inventory_List_Consumables"),
            GetNode<Control>("Panel/ScrollContainer - Inventory_List_Weapons"),
            GetNode<Control>("Panel/ScrollContainer - Inventory_List_Armors"),
            GetNode<Control>("Panel/ScrollContainer - Inventory_List_Tools"),
            GetNode<Control>("Panel/ScrollContainer - Inventory_List_Etc")
        };

        _tabButtons = new Button[]
        {
            GetNode<Button>("Panel/BoxContainer - Inventory_Tabs/Button"),
            GetNode<Button>("Panel/BoxContainer - Inventory_Tabs/Button2"),
            GetNode<Button>("Panel/BoxContainer - Inventory_Tabs/Button3"),
            GetNode<Button>("Panel/BoxContainer - Inventory_Tabs/Button4"),
            GetNode<Button>("Panel/BoxContainer - Inventory_Tabs/Button5")
        };

        for (int i = 0; i < _tabButtons.Length; i++)
        {
            int tabIndex = i;
            _tabButtons[i].Pressed += () => Show_Tab(tabIndex);
        }

        Show_Tab(0);
    }

    private void Show_Tab(int tabIndex)
    {
        for (int i = 0; i < _categoryLists.Length; i++)
        {
            _categoryLists[i].Visible = i == tabIndex;
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Visibility Methods
    //!---------------------------------------------------------------------------------------------------------

    public void Toggle(bool close_player_menu_on_open)
    {
        Visible = !Visible;
        if (Visible && close_player_menu_on_open)
        {
            Close_Player_Menu();
        }
    }

    public void Open()
    {
        if (Visible == false)
        {
            Visible = true;
        }
    }

    public void Close()
    {
        Visible = false;
    }

    private void Close_Player_Menu()
    {
        var player_menu = GetNodeOrNull<PlayerMenu>("/root/Player/Player_UI/CanvasLayer - Player_Menu");
        if (player_menu != null)
        {
            player_menu.Close_Menu();
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}