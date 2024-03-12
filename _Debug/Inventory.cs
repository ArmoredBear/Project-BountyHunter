using Godot;
using System.Collections.Generic;

public partial class Inventory : Node
{
    // Define the structure for an inventory item
    public struct InventoryItem
    {
        public string name;
        public string category;
    }

    // Define the maximum number of items per category
    private const int MaxItemsPerCategory = 5;

    // Create a dictionary to store items based on their category
    private Dictionary<string, List<InventoryItem>> inventory;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Initialize the inventory dictionary
        inventory = new Dictionary<string, List<InventoryItem>>();

        // Initialize categories
        string[] categories = { "Weapons", "Armor", "Consumables", "QuestItems", "Miscellaneous" };

        // Populate the inventory dictionary with empty lists for each category
        foreach (string category in categories)
        {
            inventory.Add(category, new List<InventoryItem>());
        }

        // Add some example items to the inventory
        AddItem("Sword", "Weapons");
        AddItem("Shield", "Armor");
        AddItem("Health Potion", "Consumables");
        AddItem("Key", "QuestItems");
        AddItem("Gem", "Miscellaneous");

        // Print the current inventory to the console
        PrintInventory();
    }

    // Function to add an item to the inventory
    private void AddItem(string itemName, string category)
    {
        // Check if the category exists in the inventory
        if (inventory.ContainsKey(category))
        {
            // Check if the category has reached the maximum number of items
            if (inventory[category].Count < MaxItemsPerCategory)
            {
                // Add the item to the category
                InventoryItem newItem;
                newItem.name = itemName;
                newItem.category = category;
                inventory[category].Add(newItem);
                GD.Print("Added", itemName, "to", category);
            }
            else
            {
                GD.Print("Cannot add", itemName, "to", category, "- Category is full!");
            }
        }
        else
        {
            GD.Print("Category", category, "does not exist in the inventory!");
        }
    }

    // Function to print the current inventory to the console
    private void PrintInventory()
    {
        GD.Print("Current Inventory:");

        foreach (var category in inventory.Keys)
        {
            GD.Print("Category:", category);

            foreach (var item in inventory[category])
            {
                GD.Print("  -", item.name);
            }
        }
    }
}
