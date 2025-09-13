using Godot;

namespace PlayerScript.PlayerInventory
{
    [GlobalClass]
    public partial class ItemData : Resource
    {
        [Export] public string ItemID;
        [Export] public string Name;
        [Export] public Texture2D Icon;
        [Export] public ItemType Type;
        [Export] public int SlotPerItem = 1;
    }
};