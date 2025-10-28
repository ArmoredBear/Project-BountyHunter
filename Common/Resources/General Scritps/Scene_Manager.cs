using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


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

    public Dictionary<e_Game_Scenes, Scene_Data> Scenes_Dictionary = new Dictionary<e_Game_Scenes, Scene_Data>()
    {
        { e_Game_Scenes.MainMenu, new Scene_Data(MainMenu_Scene_Path,"Main Menu", false)},
        { e_Game_Scenes.Forest, new Scene_Data(Forest_Scene_Path,"Forest", false)},
        { e_Game_Scenes.Tunnel, new Scene_Data(Tunnel_Scene_Path,"Tunnel", false)},
        { e_Game_Scenes.Clearing, new Scene_Data(Clearing_Scene_Path,"Clearing", false)},
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

    public void Change_Scene(e_Game_Scenes _scene_name)
    {
        // This changes scenes in the game
        string path = Scenes_Dictionary[_scene_name].path;
        GetTree().ChangeSceneToFile(path);
    }

    #endregion
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------




    //! --------------------------------------------------------------------------------------------------------------------------------------------------------
    #region TEMPORARY TESTING
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------

    public void Update_Player_Position(Vector2 _new_position)
    {
        Player.Instance.Position = _new_position;

    }

        #endregion
        //! --------------------------------------------------------------------------------------------------------------------------------------------------------


    }
