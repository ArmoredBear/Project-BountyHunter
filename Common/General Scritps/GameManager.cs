using Godot;
using System;

public partial class GameManager : Node
{
	public static GameManager Instance;
	private Button _dialogueButton;
	private DialogueManager _dialogueManager;
	public override void _Ready()
	{
		base._Ready();
			
		if(Instance == null)
		{
			Instance = this;
		}

		else if (Instance != null && Instance != this)
		{
			GD.PrintErr("ERROR!! Instance of Game Manager already exist!!");
		}
		
		_dialogueManager = GetNode<DialogueManager>(.);
		_dialogueButton = GetNode<Button>("Dialogue");
		_dialogueButton.Pressed += OnDialogueButtonPressed;
		
		_dialogueManager.DialogueFinished += OnDialogueFinished;
		



		GD.Print("Game Manager loaded...");
		
	}
		private void OnDialogueButtonPressed()
	{
		// Desabilita o botão para não ser clicado novamente durante o diálogo
		_dialogueButton.Disabled = true;
		// Inicia o diálogo passando o caminho para o arquivo JSON
		_dialogueManager.StartDialogue("res://Dialogue/intro_dialogue.json");
	}

	private void OnDialogueFinished()
	{
		GD.Print("O diálogo terminou! O jogo pode continuar.");
		// Reabilita o botão
		_dialogueButton.Disabled = false;
	}

}
