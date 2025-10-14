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

		// -------------------------
		// AJUSTE DE SLOTS
		// -------------------------
		private int GetSlotDelta(ItemInstance item, int quantityChange)
		{
			return (item.Data?.SlotPerItem ?? 1) * quantityChange;
		}

		// -------------------------
		// ADD ITEM
		// -------------------------

		public bool AddItem(ItemInstance item)
		{
			// Primeiro, procura se já existe item igual
			var existing = FindItem(i => i.Data.ItemID == item.Data.ItemID);

			if (existing != null)
			{
				// Proteção contra empilhar a mesma referência
				if (ReferenceEquals(existing, item))
				{
					GD.PrintErr($"Tentando empilhar o mesmo objeto '{item.Data.Name}'! Crie uma nova instância.");
					return false;
				}

				int additionalSlots = GetSlotDelta(item, item.Quantity);
				if (_usedSlots + additionalSlots <= MaxInventorySlots)
				{
					existing.Quantity += item.Quantity;
					_usedSlots += additionalSlots;
					EmitSignal(SignalName.InventoryUpdated);
					return true;
				}
				return false;
			}

			
			int neededSlots = GetSlotDelta(item, item.Quantity);
			if (_usedSlots + neededSlots <= MaxInventorySlots)
			{
				_inventoryByType[item.Data.Type].Add(item.Clone()); // garante nova instância
				_usedSlots += neededSlots;
				EmitSignal(SignalName.InventoryUpdated);
				return true;
			}

			return false;
		}

		// -------------------------
		// REMOVE ITEM
		// -------------------------

		public bool RemoveItem(ItemInstance item, int quantity = 1)
		{
			// Encontra a instância real no inventário
			var storedItem = FindItem(i => ReferenceEquals(i, item) || i.Data.ItemID == item.Data.ItemID);
			if (storedItem == null)
				return false;

			int removeQuantity = Math.Min(quantity, storedItem.Quantity);
			int slotReduction = GetSlotDelta(storedItem, removeQuantity);

			storedItem.Quantity -= removeQuantity;
			_usedSlots -= slotReduction;

			if (storedItem.Quantity <= 0)
			{
				_inventoryByType[storedItem.Data.Type].Remove(storedItem);
			}


			EmitSignal(SignalName.InventoryUpdated);
			return true;
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
