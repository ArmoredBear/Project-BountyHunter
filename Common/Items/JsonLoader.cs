using Godot;
using System;
using Newtonsoft.Json;
using System.IO;

public partial class JsonLoader : Node
{
    private string _item_saved_data_path;
    private string _inventory_saved_data_path;
    private string _json_data;
    private ItemData _item_data;
    private InventoryData _inventory_data;

    public string Item_Saved_Data_Path
    {
        get
        {
            return _item_saved_data_path;
        }

        set
        {
            _item_saved_data_path = value;
        }
    }

    public string Inventory_Saved_Data_Path
    {
        get
        {
            return _inventory_saved_data_path;
        }

        set
        {
            _inventory_saved_data_path = value;
        }
    }
    
    public string Json_Data
    {
        get
        {
            return _json_data;
        }
        set
        {
            _json_data = value;
        }
    }

    public ItemData Item_Data
    {
        get
        {
            return _item_data;
        }

        set
        {
            _item_data = value;
        }
    }

    public InventoryData Inventory_Data
    {
        get
        {
            return _inventory_data;
        }
        
        set
        {
            _inventory_data = value;
        }
    }


    public override void _Ready()
    {
        Item_Saved_Data_Path = ProjectSettings.GlobalizePath("user://SavedData");
        Item_Data = new ItemData("Pill","A pill that gradually restores health", 3, 100);

        Serialize_Data();

        string data = LoadTextFromFile(Item_Saved_Data_Path,"ItemData.json");

        GD.Print("-- LOADED DATA --  : " + data);
    }

    public void Serialize_Data()
    {
        Json_Data = JsonConvert.SerializeObject(Item_Data);
        //GD.Print(Json_Data);

        SaveTextToFile(Item_Saved_Data_Path, "ItemData.json", Json_Data);
    }

    public void Deserialize_Data()
    {
        Item_Data = (ItemData)JsonConvert.DeserializeObject(Json_Data);
        //GD.Print(Item_Data.Name);
    }

    private void SaveTextToFile(string path, string file_name, string data)
    {
        if(!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        path = Path.Join(path, file_name);

        GD.Print("-- SAVED DATA AT -- :" + path);

        try
        {
            File.WriteAllText(path, data);
        }
        catch (System.Exception e)
        {
            
            GD.Print(e);
        }
    }

    private string LoadTextFromFile(string path, string file_name)
    {
        string data = null;
        
        path = Path.Join(path, file_name);

        if(!File.Exists(path)) return null;

        try
        {
            data = File.ReadAllText(path);
        }
        catch (System.Exception e)
        {
            
            GD.Print(e);
        }

        return data;
    }
}
