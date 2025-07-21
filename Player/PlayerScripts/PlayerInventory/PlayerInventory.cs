using Godot;
using System.Collections.Generic;

namespace PlayerScript.PlayerInventory
{
	public partial class PlayerInventory : Resource
	{
		[Export] public int MaxInventorySlots { get; set; } = 20;

		private readonly Dictionary<ItemType, Godot.Collections.Array<ItemInstance>> _inventoryByType = new()
		{
			{ ItemType.Consumable, new Godot.Collections.Array<ItemInstance>() },
			{ ItemType.Weapon, new Godot.Collections.Array<ItemInstance>() },
			{ ItemType.Armor, new Godot.Collections.Array<ItemInstance>() },
			{ ItemType.Tool, new Godot.Collections.Array<ItemInstance>() }
		};

		public int GetUsedInventorySlots()
		{
			int used = 0;
			foreach (var category in _inventoryByType.Values)
			{
				foreach (var item in category)
					used += item.GetTotalSlotUsage();
			}
			return used;
		}

		public bool CanAddItem(ItemInstance item)
		{
			return (GetUsedInventorySlots() + item.GetTotalSlotUsage()) <= MaxInventorySlots;
		}

		public bool AddItem(ItemInstance item)
		{
			if (CanAddItem(item))
			{
				_inventoryByType[item.Type].Add(item);
				return true;
			}
			return false;
		}

		public void RemoveItem(ItemInstance item)
		{
			_inventoryByType[item.Type].Remove(item);
		}

		public List<ItemInstance> GetItemsByType(ItemType type)
		{
			List<ItemInstance> items = new();
			foreach (var item in _inventoryByType[type])
				items.Add(item);
			return items;
		}
	}
}
