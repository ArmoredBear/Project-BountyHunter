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
		// SIGNALS
		// -------------------------
		[Signal] public delegate void ItemAddedEventHandler(ItemInstance item);
		[Signal] public delegate void ItemRemovedEventHandler(ItemInstance item);
		[Signal] public delegate void InventoryUpdatedEventHandler();

		// -------------------------
		// ADD / REMOVE
		// -------------------------

		public bool CanAddItem(ItemInstance item)
		{
			return (_usedSlots + item.GetTotalSlotUsage()) <= MaxInventorySlots;
		}

		public bool AddItem(ItemInstance item)
		{
			var existing = FindItem(i => i.Data.ItemID == item.Data.ItemID);
			if (existing != null)
			{
				int addedUsage = item.GetTotalSlotUsage();
				if (_usedSlots + addedUsage <= MaxInventorySlots)
				{
					// Proteção: evita dobrar referência
					if (ReferenceEquals(existing, item))
					{
						GD.PrintErr($"Tentando empilhar o mesmo objeto '{item.Data.Name}'! Crie uma nova instância.");
						return false;
					}

					existing.Quantity += item.Quantity;
					_usedSlots += addedUsage;
					EmitSignal(SignalName.ItemAdded, existing);
					EmitSignal(SignalName.InventoryUpdated);
					return true;
				}
				return false;
			}

			int usage = _usedSlots + item.GetTotalSlotUsage();
			if (usage <= MaxInventorySlots)
			{
				_inventoryByType[item.Data.Type].Add(item.Clone());
				_usedSlots = usage;
				EmitSignal(SignalName.ItemAdded, item);
				EmitSignal(SignalName.InventoryUpdated);
				return true;
			}

			return false;
		}
		public bool RemoveItem(ItemInstance item)
		{
			if (_inventoryByType[item.Data.Type].Contains(item))
			{
				// Se o item tem quantidade maior que 1, decrementa
				if (item.Quantity > 1)
				{
					item.Quantity--;
					_usedSlots -= (item.Data?.SlotPerItem ?? 1);
				}
				else
				{
					_inventoryByType[item.Data.Type].Remove(item);
					_usedSlots -= item.GetTotalSlotUsage();
				}

				EmitSignal(SignalName.ItemRemoved, item);
				EmitSignal(SignalName.InventoryUpdated);
				return true;
			}
			return false;
		}

		// -------------------------
		// BUSCA GENÉRICA
		// -------------------------

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
