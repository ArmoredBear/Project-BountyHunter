using Godot;

namespace PlayerScript.PlayerInventory
{
	[GlobalClass]
	public partial class ItemInstance : Resource
	{
		[Export] public ItemData Data;   // Referência ao "template" do item
		[Export] public int Quantity = 1;

		public int GetTotalSlotUsage() => Quantity * (Data?.SlotPerItem ?? 1);

		public override string ToString()
		{
			return $"{Data?.Name ?? "Unknown"} x{Quantity}";
		}
	}
}