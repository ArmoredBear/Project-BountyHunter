using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   SAVE_LOAD_CONTROL
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Manages saving and loading of player data to and from disk.
	**  2 - Exposes the save file path, file name and current player data.
	**  3 - Provides directory verification, save and load routines.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Save_Load_Control : Node
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private string _save_file_path;
	private string _save_file_name;
	private Player_Data _data;

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

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

	public static Save_Load_Control Instance;

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		_save_file_path = "user://SavedData/";
		Save_File_Name = "PlayerDataSave.tres";
		Data = Player_Data_Autoload.Data;

		Verify_Directory(Save_File_Path);

		//Singleton
		if (Instance == null)
		{
			Instance = this;
		}

		else if (Instance != null && this != Instance)
		{
			Instance = null;
			GetTree().QueueDelete(this);
		}

	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Methods
	//!---------------------------------------------------------------------------------------------------------

	public void Verify_Directory(string _path)
	{
		DirAccess.MakeDirAbsolute(_path);
	}

	public void Save_Game()
	{
		GD.Print("Attempting to save game data to disk...");
		Player_Data_Autoload.Instance.Update_Data_To_Save();
		Error err = ResourceSaver.Save(Data, Save_File_Path + Save_File_Name);
		if (err == Error.Ok)
		{
			GD.Print("Game data saved successfully to: " + Save_File_Path + Save_File_Name);
		}
		else
		{
			GD.PrintErr("Failed to save game data. Error: " + err);
		}
	}

	public void Load_Game()
	{
		GD.Print("Attempting to load game data from disk...");
		var loaded = ResourceLoader.Load(Save_File_Path + Save_File_Name);
		if (loaded != null)
		{
			Data = (Player_Data)loaded.Duplicate(true);
			Player_Data_Autoload.Data = Data;
			Player_Data_Autoload.Instance.Update_Loaded_Data();
			GD.Print("Game data loaded successfully from: " + Save_File_Path + Save_File_Name);
		}
		else
		{
			GD.PrintErr("Failed to load game data from: " + Save_File_Path + Save_File_Name);
		}
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

	#endregion
	//!---------------------------------------------------------------------------------------------------------
}
