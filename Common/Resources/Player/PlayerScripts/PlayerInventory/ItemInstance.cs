using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   ITEMINSTANCE
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Represents an item instance held in the inventory (template data plus quantity).
	**  2 - Provides helpers for slot usage, string representation, and cloning.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

namespace PlayerScript.PlayerInventory
{
	[GlobalClass]
	public partial class ItemInstance : Resource
	{
		//!---------------------------------------------------------------------------------------------------------
		#region Variables
		//!---------------------------------------------------------------------------------------------------------

		[Export] public ItemData Data;   // Referência ao "template" do item
		[Export] public int Quantity = 1;

		#endregion
		//!---------------------------------------------------------------------------------------------------------

		//!---------------------------------------------------------------------------------------------------------
		#region Methods
		//!---------------------------------------------------------------------------------------------------------

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

		#endregion
		//!---------------------------------------------------------------------------------------------------------
	}
}