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

		/// <summary>
		/// Cria uma cópia independente desta instância (sem compartilhar referência).
		/// </summary>
		public ItemInstance Clone()
		{
			var clone = new ItemInstance
			{
				Data = Data,         // ainda é a mesma referência, o que é desejável — o "template" é o mesmo
				Quantity = Quantity
			};
			return clone;
		}
	}
}