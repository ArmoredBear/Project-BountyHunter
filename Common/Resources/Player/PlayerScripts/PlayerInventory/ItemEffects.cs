using Godot;
using System;
using System.Collections.Generic;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   ITEMEFFECTS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Single place with every item effect in the game.
	**  2 - Maps an item EffectID to the action it performs.
	**  3 - The Use button only needs to call Use(item) or go through Messenger.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

namespace PlayerScript.PlayerInventory
{
	public static class ItemEffects
	{
		//!---------------------------------------------------------------------------------------------------------
		#region Effect Registry
		//!---------------------------------------------------------------------------------------------------------

		private static readonly Dictionary<string, Action<int>> Effects = new()
		{
			{ "heal_small", HealSmall },
			{ "heal_large", HealLarge },
			{ "pill", StartPillRegen }
		};

		#endregion
		//!---------------------------------------------------------------------------------------------------------

		//!---------------------------------------------------------------------------------------------------------
		#region Public Methods
		//!---------------------------------------------------------------------------------------------------------

		public static void Use(ItemInstance item)
		{
			if (item == null || item.Data == null)
			{
				return;
			}

			string effectId = item.Data.EffectID;

			if (string.IsNullOrEmpty(effectId))
			{
				GD.Print($"[ItemEffects] Item '{item.Data.Name}' has no EffectID set.");
				return;
			}

			if (Effects.TryGetValue(effectId, out Action<int> effect))
			{
				effect(item.Quantity);
			}
			else
			{
				GD.Print($"[ItemEffects] No effect registered for '{effectId}'.");
			}
		}

		#endregion
		//!---------------------------------------------------------------------------------------------------------

		//!---------------------------------------------------------------------------------------------------------
		#region Effects
		//!---------------------------------------------------------------------------------------------------------

		private static void HealSmall(int quantity)
		{
			HealPlayer(50, quantity);
		}

		private static void HealLarge(int quantity)
		{
			HealPlayer(150, quantity);
		}

		private static void HealPlayer(int healPerItem, int quantity)
		{
			int totalHeal = healPerItem * quantity;
			GD.Print($"[ItemEffects] Healing player for {totalHeal} HP.");

			Player_Data_Autoload.Instance.Apply_Heal(totalHeal);
		}

		//!---------------------------------------------------------------------------------------------------------
		#region Pill
		//!---------------------------------------------------------------------------------------------------------

		private const int Pill_Tick_Heal = 5;
		private const int Pill_Tick_Count = 20;

		private static void StartPillRegen(int quantity)
		{
			int totalHeal = Pill_Tick_Heal * Pill_Tick_Count * quantity;
			GD.Print($"[ItemEffects] Starting pill regen for {totalHeal} HP.");

			Player_Data_Autoload.Instance.Start_Pill_Regen(totalHeal);
		}

		#endregion

		#endregion
		//!---------------------------------------------------------------------------------------------------------
	}
}