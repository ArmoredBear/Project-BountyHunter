using Godot;

namespace PlayerScript.PlayerInventory
{
	[GlobalClass]
	public partial class ItemInstance : Resource
	{
		[Export] public string ItemID;
		[Export] public int Quantity = 1;
		[Export] public int SlotPerItem = 1;
		[Export] public ItemType Type;

		public int GetTotalSlotUsage() => Quantity * SlotPerItem;
	}
}
