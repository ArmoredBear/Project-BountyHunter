using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
 *!                                              PLAYER DATA AUTOLOAD CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
  	**                                                 	 PURPOSE
  	*
	**  1 - This class is a singleton, it inherits Node but it does not need to be on scene, that is why is called AUTOLOAD,
	**  when the scene starts the object is created automaticaly with this script functions and properties before any other
	** 	object.
  	**  2 - This only holds the player data in the scene and provides easy acess to the data properties on other scripts.
  	**  3 - This also has a constructor to initiate default data.
  	*  
*-----------------------------------------------------------------------------------------------------------------------**/

[Tool]
public partial class Player_Data_Autoload : Node
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Player_Data _player_data;
	
	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------


	[ExportCategory ("Data")]
	[Export] public Player_Data Data
	{
		get
		{
			return _player_data;
		}

		set
		{
			_player_data = value;
		}
	}

	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{	
		Data = new Player_Data(100, 100, 100);

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		

	}


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods
	//!---------------------------------------------------------------------------------------------------------

	#endregion
}
