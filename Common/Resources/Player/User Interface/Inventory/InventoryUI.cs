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

    [Export] public ItemType Item_Category;

    private NodePath _playerInventoryPath;
    private PlayerInventory _playerInventory;
    private Messenger _messenger;

    private Control _scrollView;
    private VScrollBar _vScrollBar;

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        _playerInventoryPath = "/root/Player/Inventory";
        _playerInventory = GetNode<PlayerInventory>(_playerInventoryPath);
        _messenger = GetNodeOrNull<Messenger>("/root/Messenger");

        _scrollView = GetParent<Control>();
        _vScrollBar = _scrollView.GetNode<VScrollBar>("VScrollBar");

        _vScrollBar.ValueChanged += OnScrollValueChanged;
        _scrollView.Resized += UpdateScrollbar;
        _scrollView.GuiInput += OnScrollViewGuiInput;

        _playerInventory.ItemAdded += OnInventoryChanged;
        _playerInventory.ItemRemoved += OnInventoryChanged;
        _playerInventory.InventoryUpdated += OnInventoryChanged;

        RefreshUI();

        CallDeferred(nameof(UpdateScrollbar));
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!---------------------------------------------------------------------------------------------------------
    #region Signal Handlers
    //!---------------------------------------------------------------------------------------------------------

    private void OnInventoryChanged(ItemInstance item)
    {
        RefreshUI();
        UpdateScrollbar();
    }

    private void OnInventoryChanged()
    {
        RefreshUI();
        UpdateScrollbar();
    }

    private void OnScrollValueChanged(double value)
    {
        Position = new Vector2(0, (float)-value);
    }

    private void OnScrollViewGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                _vScrollBar.Value -= _vScrollBar.Step;
                _scrollView.AcceptEvent();
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                _vScrollBar.Value += _vScrollBar.Step;
                _scrollView.AcceptEvent();
            }
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    private void RefreshUI()
    {
        ClearExistingButtons();

        foreach (ItemInstance item in _playerInventory.GetItemsByType(Item_Category))
        {
            Button button = new Button
            {
                CustomMinimumSize = new Vector2(0, 90),
                Icon = item.Data?.Icon,
                Text = $"{item.Data?.Name ?? "Unknown"} x{item.Quantity}"
            };
            button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            AddChild(button);

            if (Item_Category == ItemType.Consumable)
            {
                ItemInstance usedItem = item;
                button.Pressed += () => Use_Item(usedItem);
            }
        }
    }

    private void Use_Item(ItemInstance item)
    {
        if (_messenger != null)
        {
            _messenger.Use_Item(item);
        }
        _playerInventory.RemoveItem(item, 1);
    }

    private void ClearExistingButtons()
    {
        foreach (Node child in GetChildren())
        {
            child.QueueFree();
        }
    }

    private void UpdateScrollbar()
    {
        float viewHeight = _scrollView.Size.Y;
        float contentHeight = Size.Y;
        float max = Mathf.Max(0, contentHeight - viewHeight);

        _vScrollBar.MaxValue = max;
        _vScrollBar.Page = viewHeight;

        if (_vScrollBar.Value > max)
        {
            _vScrollBar.Value = max;
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}