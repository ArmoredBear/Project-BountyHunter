using Godot;
using PlayerScript.PlayerInventory;

public partial class InventoryUI : Control
{
    [Export] private NodePath _playerInventoryPath;
    [Export] private NodePath _vboxPath;

    private PlayerInventory _playerInventory;
    private VBoxContainer _vbox;

    public override void _Ready()
    {
        _playerInventory = GetNode<PlayerInventory>(_playerInventoryPath);
        _vbox = GetNode<VBoxContainer>(_vboxPath);

        _playerInventory.ItemAdded += OnInventoryChanged;
        _playerInventory.ItemRemoved += OnInventoryChanged;
        _playerInventory.InventoryUpdated += OnInventoryChanged;

        RefreshUI();
    }

    private void OnInventoryChanged(ItemInstance item) => RefreshUI();
    private void OnInventoryChanged() => RefreshUI();

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
}
