using Godot;
using System;

public partial class Dialog : Control
{
	private Label _speaker;
	private RichTextLabel _dialogue;
	private Button _continue;

	public override void _Ready()
	{
		_speaker = GetNode<Label>("VBoxContainer/Speaker");
		_dialogue = GetNode<RichTextLabel>("VBoxContainer/Dialogue");
		_continue = GetNode<Button>("Box/Continue");
	}

	public void DisplayLine(string line, string speaker)
	{
		_speaker.Visible = (speaker != "");
		_speaker.Text = speaker;
		_dialogue.Text = line;
		Open();
		_continue.GrabFocus();
	}

	public void Open()
	{
		Visible = true;
	}

	public void Close()
	{
		Visible = false;
	}

	private void OnContinuePressed()
	{
		Close();
	}
}
