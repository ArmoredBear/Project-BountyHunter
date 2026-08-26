using Godot;
using PlayerScript.PlayerInventory;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   INVENTORYUI
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Manage and display the player inventory in the user interface.
	**  2 - Refresh the item buttons whenever the inventory changes.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class InventoryUI : Control
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    private NodePath _playerInventoryPath;
    private PlayerInventory _playerInventory;

    public Node _vbox;

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        _playerInventoryPath = "/root/Player/Inventory";
        _playerInventory = GetNode<PlayerInventory>(_playerInventoryPath);

        _vbox = this;

        _playerInventory.ItemAdded += OnInventoryChanged;
        _playerInventory.ItemRemoved += OnInventoryChanged;
        _playerInventory.InventoryUpdated += OnInventoryChanged;

        RefreshUI();
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!---------------------------------------------------------------------------------------------------------
    #region Signal Handlers
    //!---------------------------------------------------------------------------------------------------------

    private void OnInventoryChanged(ItemInstance item) => RefreshUI();
    private void OnInventoryChanged() => RefreshUI();

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    private void RefreshUI()
    {
        // limpa botões antigos
        foreach (Node child in _vbox.GetChildren())
            child.QueueFree();

        // recria botões com base nos itens do inventário
        foreach (var type in System.Enum.GetValues(typeof(ItemType)))
        {
            foreach (var item in _playerInventory.GetItemsByType((ItemType)type))
            {
                var btn = new Button
                {
                    Text = $"{item.Data.Name} x{item.Quantity}",
                    CustomMinimumSize = new Vector2(300, 50)
                };

                btn.Pressed += () => GD.Print($"Clicou em {item}");
                _vbox.AddChild(btn);
            }
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
