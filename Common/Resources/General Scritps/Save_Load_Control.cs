using Godot;
using System;

public partial class Save_Load_Control : Node
{
    private string _save_file_path;
    private string _save_file_name;
    private Player_Data _data;

    public string Save_File_Path
    {
        get
        {
            return _save_file_path;
        }
    }

    public string Save_File_Name
    {
        get
        {
            return _save_file_name;
        }

        set
        {
            _save_file_name = value;
        }
    }

    public Player_Data Data
    {
        get
        {
            return _data;
        }

        set
        {
            _data = value;
        }
    }

    
    public override void _Ready()
    {
        _save_file_path = "user://SavedData/";
        Save_File_Name = "PlayerDataSave.tres";
        Data = Player_Data_Autoload.Data;

        Verify_Directory(Save_File_Path);

    }

    public void Verify_Directory(string _path)
    {
        DirAccess.MakeDirAbsolute(_path);
    }

    public void Save_Game()
    {
        Player_Data_Autoload.Instance.Update_Data_To_Save();
        ResourceSaver.Save(Data, Save_File_Path + Save_File_Name);
    }

    public void Load_Game()
    {
        Data = (Player_Data)ResourceLoader.Load(Save_File_Path + Save_File_Name).Duplicate(true);
        Player_Data_Autoload.Data = Data;
        Player_Data_Autoload.Instance.Update_Loaded_Data();
    }


    /**-----------------------------------------------------------------------------------------------------------------------
     *!                                                   TEMPORARY
     *-----------------------------------------------------------------------------------------------------------------------**/

    public override void _Process(double delta)
    {
        if(Input.IsActionJustReleased("TempSave"))
        {
            Save_Game();
            GD.Print("Data Saved!");
        }

        if(Input.IsActionJustReleased("TempLoad"))
        {
            Load_Game();
            GD.Print("Data Loaded!");
        }
    }
}
