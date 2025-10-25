using Godot;
using System;

public partial class MainMenu : CanvasLayer
{
    [Export] public Panel Menu_Panel { get; set; }
    [Export] public Panel Logo_Panel { get; set; }
    [Export] public AnimationPlayer Menu_Animation { get; set; }
    [Export] public AnimationPlayer Logo_Animation { get; set; }
    [Export] public bool Menu_is_Shown { get; set; } = false;

    public override void _Ready()
    {
        base._Ready();
        Reference_If_Null();

    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Menu_is_Shown)
        {
            if (Input.IsAnythingPressed())
            {
                Menu_Animation.Play("Slide_Up");
                Logo_Animation.Play("Slide_Down");
                Menu_is_Shown = true;
            }
        }
    }

    public void Reference_If_Null()
    {
        if (Menu_Panel == null)
        {
            GD.Print("Menu Panel is NULL!! Trying to reference it..");
            try
            {
                Menu_Panel = GetNode<Panel>("%MenuPanel");
                GD.Print("Sucess! Reference is: " + Menu_Panel.Name);
            }
            catch (Exception ex)
            {
                GD.Print(ex.Message);

            }
        }
        if (Logo_Panel == null)
        {
            GD.Print("Logo Panel is NULL!! Trying to reference it..");
            try
            {
                Logo_Panel = GetNode<Panel>("%LogoPanel");
                GD.Print("Sucess! Reference is: " + Logo_Panel.Name);
            }
            catch (Exception ex)
            {
                GD.Print(ex.Message);

            }
        }
        if (Menu_Animation == null)
        {
            GD.Print("Menu Animation is NULL!! Trying to reference it..");
            try
            {
                Logo_Panel = GetNode<Panel>("%MenuAnimation");
                GD.Print("Sucess! Reference is: " + Menu_Animation.Name);
            }
            catch (Exception ex)
            {
                GD.Print(ex.Message);

            }
        }
        if (Logo_Animation == null)
        {
            GD.Print("Logo Animation is NULL!! Trying to reference it..");
            try
            {
                Logo_Panel = GetNode<Panel>("%LogoAnimation");
                GD.Print("Sucess! Reference is: " + Logo_Animation.Name);
            }
            catch (Exception ex)
            {
                GD.Print(ex.Message);

            }
        }
    }


}
