using Godot;
using System;
using System.Collections.Generic;

#nullable enable

namespace PlayerScript.PlayerInventory
{
	public partial class PlayerInventory : Node
	{
		[Export] public int MaxInventorySlots { get; set; } = 20;

		private readonly Dictionary<ItemType, Godot.Collections.Array<ItemInstance>> _inventoryByType = new()
		{
			{ ItemType.Consumable, new Godot.Collections.Array<ItemInstance>() },
			{ ItemType.Weapon, new Godot.Collections.Array<ItemInstance>() },
			{ ItemType.Armor, new Godot.Collections.Array<ItemInstance>() },
			{ ItemType.Tool, new Godot.Collections.Array<ItemInstance>() }
		};

		private int _usedSlots = 0;

		// -------------------------
		// GETTERS
		// -------------------------


		public int GetUsedInventorySlots() => _usedSlots;

		public int GetFreeSlots() => MaxInventorySlots - _usedSlots;

		public List<ItemInstance> GetItemsByType(ItemType type)
		{
			return new List<ItemInstance>(_inventoryByType[type]);
		}

		// -------------------------
		// ADD / REMOVE
		// -------------------------=
		[Signal] public delegate void ItemAddedEventHandler(ItemInstance item);
		[Signal] public delegate void ItemRemovedEventHandler(ItemInstance item);

		public bool CanAddItem(ItemInstance item)
		{
			return (_usedSlots + item.GetTotalSlotUsage()) <= MaxInventorySlots;
		}

		public bool AddItem(ItemInstance item)
		{
			int newUsage = _usedSlots + item.GetTotalSlotUsage();
			if (newUsage <= MaxInventorySlots)
			{
				_inventoryByType[item.Data.Type].Add(item);
				_usedSlots = newUsage;
				EmitSignal(SignalName.ItemAdded, item);
				return true;
			}
			return false;
		}
		public bool RemoveItem(ItemInstance item)
		{
			if (_inventoryByType[item.Data.Type].Remove(item))
			{
				_usedSlots -= item.GetTotalSlotUsage();
				EmitSignal(SignalName.ItemRemoved, item);
				return true;
			}
			return false;
		}

		// -------------------------
		// BUSCA GENÉRICA
		// -------------------------

		/// Procura o primeiro item que satisfaça a condição.
		/// Exemplo: FindItem(i => i.Id == "potion_01");
		public ItemInstance? FindItem(Predicate<ItemInstance> match)
		{
			foreach (var category in _inventoryByType.Values)
			{
				foreach (var item in category)
				{
					if (match(item))
						return item;
				}
			}
			return null;
		}

		/// Procura todos os itens que satisfaçam a condição.
		/// Exemplo: FindItems(i => i.Rarity == Rarity.Legendary);
		public List<ItemInstance> FindItems(Predicate<ItemInstance> match)
		{
			List<ItemInstance> result = new();
			foreach (var category in _inventoryByType.Values)
			{
				foreach (var item in category)
				{
					if (match(item))
						result.Add(item);
				}
			}
			return result;
		}

		// -------------------------
		// DEBUG
		// -------------------------

		public void PrintInventory()
		{
			GD.Print($"[Inventory] Used: {_usedSlots}/{MaxInventorySlots}");
			foreach (var kvp in _inventoryByType)
			{
				GD.Print($"  {kvp.Key}: {kvp.Value.Count} items");
				foreach (var item in kvp.Value)
				{
					GD.Print($"    - {item}");
				}
			}
		}

	}
}
