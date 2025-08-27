using System;
using Godot;

public partial class DialogueUI : CanvasLayer
{
	private Label _nameLabel;
	private RichTextLabel _textLabel;

	public override void _Ready()
	{
		// Usamos % para obter nós únicos na cena, é uma boa prática!
		_nameLabel = GetNode<Label>("%NameLabel");
		_textLabel = GetNode<RichTextLabel>("%TextLabel");
	}

	public void SetDialogueName(string name)
	{
		_nameLabel.Text = name;
	}

	public void SetDialogueText(string text)
	{
		_textLabel.Text = text;
	}

    public static implicit operator DialogueUI(PackedScene v)
    {
        throw new NotImplementedException();
    }
}
