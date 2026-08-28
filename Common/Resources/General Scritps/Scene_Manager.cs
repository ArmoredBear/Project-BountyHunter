using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   SCENE_MANAGER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Central scene management singleton.
	**  2 - Stores scene paths and changes the active scene.
	**  3 - Sets the player spawn for scene transitions.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Scene_Manager : Node
{
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------
    #region References and Variables
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------

    public static Scene_Manager Instance;


    private static String MainMenu_Scene_Path = "res://Common/MainScenes/Main_Menu.tscn";
    private static String Tunnel_Scene_Path = "res://Common/Resources/Cenarios/Prefabs/Tunnel.tscn";
    private static String Forest_Scene_Path = "res://Common/Resources/Cenarios/Prefabs/Forest.tscn";
    private static String Clearing_Scene_Path = "res://Common/Resources/Cenarios/Prefabs/Clearing.tscn";
    private static String GameOver_Scene_Path = "res://Common/Resources/Player/PlayerPrefabs/Game_Over_Screen.tscn";



    public Dictionary<e_Game_Scenes, Scene_Data> Scenes_Dictionary = new Dictionary<e_Game_Scenes, Scene_Data>()
    {
        { e_Game_Scenes.MainMenu, new Scene_Data(MainMenu_Scene_Path,"Main Menu", false)},
        { e_Game_Scenes.Forest, new Scene_Data(Forest_Scene_Path,"Forest", false)},
        { e_Game_Scenes.Tunnel, new Scene_Data(Tunnel_Scene_Path,"Tunnel", false)},
        { e_Game_Scenes.Clearing, new Scene_Data(Clearing_Scene_Path,"Clearing", false)},
        { e_Game_Scenes.GameOver, new Scene_Data(GameOver_Scene_Path,"Game Over", false)},
    };


    #endregion
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------


    //! --------------------------------------------------------------------------------------------------------------------------------------------------------
    #region Initialization
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        Singleton_Setup();
        Rerefence_If_Null();

    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    #endregion
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------

    //! --------------------------------------------------------------------------------------------------------------------------------------------------------
    #region Methods
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------

    private void Rerefence_If_Null()
    {
        //This just reference the variables if they are null, the method is called on Ready.

        if (Scenes_Dictionary == null)
        {
            Scenes_Dictionary = new Dictionary<e_Game_Scenes, Scene_Data>()
            {
                { e_Game_Scenes.MainMenu, new Scene_Data(MainMenu_Scene_Path,"Main Menu", false)},
                { e_Game_Scenes.Forest, new Scene_Data(Forest_Scene_Path,"Forest", false)},
                { e_Game_Scenes.Tunnel, new Scene_Data(Tunnel_Scene_Path,"Tunnel", false)},
                { e_Game_Scenes.Clearing, new Scene_Data(Clearing_Scene_Path,"Clearing", false)},
                { e_Game_Scenes.GameOver, new Scene_Data(GameOver_Scene_Path,"Game Over", false)},
            };
        }
    }

    private void Singleton_Setup()
    {
        //Singleton
        if (Instance == null)
        {
            Instance = this;
        }

        else if (Instance != this)
        {
            GD.Print("WARNING ERROR!! MORE THAN 1 SCENE MANAGER!!!");
            GD.Print("DELETING ONE OF THE SCENE MANAGERS TO AVOID ISSUES...");

            this.QueueFree();

        }
    }

    public void SetTransition(string spawnName)
    {
        if (string.IsNullOrEmpty(spawnName))
        {
            GD.PrintErr("SetTransition called with empty spawnName. Defaulting to Spawn_Default.");
            Player_Data_Autoload.Instance.NextSpawnName = "Spawn_Default";
        }
        else
        {
            Player_Data_Autoload.Instance.NextSpawnName = spawnName;
        }

        GD.Print("Scene_Manager: NextSpawnName set to ", Player_Data_Autoload.Instance.NextSpawnName);
    }

    public async void Change_Scene(e_Game_Scenes scene)
    {
        if (!Scenes_Dictionary.ContainsKey(scene))
        {
            GD.PrintErr("Scene_Manager: Scene not found in dictionary: ", scene);
            return;
        }

        string path = Scenes_Dictionary[scene].path;
        GD.Print("Scene_Manager: Changing scene to ", path, " with spawn ", Player_Data_Autoload.Instance.NextSpawnName);

        await TransitionManager.Instance.FadeToBlack();

        GetTree().ChangeSceneToFile(path);

        await TransitionManager.Instance.FadeFromBlack();
    }


    #endregion
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------


    //! --------------------------------------------------------------------------------------------------------------------------------------------------------
    #region TEMPORARY TESTING
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------

    #endregion
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------


}
