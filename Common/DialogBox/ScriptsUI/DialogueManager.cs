using Godot;
using System.Collections.Generic;
using System.Text.Json; // Essencial para parsear o JSON

public partial class DialogueManager : Node
{
	// Sinal emitido quando o diálogo termina. Outros nós podem ouvi-lo.
	[Signal]
	public delegate void DialogueFinishedEventHandler();

	private Queue<DialogueLine> _dialogueQueue = new Queue<DialogueLine>();
	private DialogueUI _dialogueUI;
	private bool _isDialogueActive = false;

	private Button _dialogueButton;
	private DialogueManager _dialogueManager;

	public override void _Ready()
	{
		DialogueInitiation();

		// Carregamos a cena da UI de diálogo e a adicionamos como filha do manager.
		// Assim, o manager controla totalmente a sua UI.
		var dialogueUIScene = ResourceLoader.Load<PackedScene>("res://Common/DialogueUI.tscn").Instantiate();
		AddChild(dialogueUIScene);
		_dialogueUI = (DialogueUI)dialogueUIScene;
		_dialogueUI.Hide(); // Começa escondida

		
	}

	public void DialogueInitiation()
	{
		_dialogueManager = this;
		_dialogueButton = GetNode("/root/Main_Menu/Control/MarginContainer/VBoxContainer/Dialogue") as Button;
		_dialogueButton.Pressed += OnDialogueButtonPressed;
		_dialogueManager.DialogueFinished += OnDialogueFinished;

	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Só processa input se o diálogo estiver ativo
		if (_isDialogueActive && @event.IsActionPressed("ui_accept"))
		{
			// Marca o evento como "tratado" para que outros nós não o processem (ex: o jogador não pula)
			GetViewport().SetInputAsHandled();
			DisplayNextLine();
		}
	}

	public void StartDialogue(string dialoguePath)
	{
		if (!FileAccess.FileExists(dialoguePath))
		{
			GD.PrintErr($"Arquivo de diálogo não encontrado em: {dialoguePath}");
			return;
		}

		// Carrega e parseia o arquivo JSON
		var file = FileAccess.Open(dialoguePath, FileAccess.ModeFlags.Read);
		string content = file.GetAsText();
		file.Close();

		var dialogueData = JsonSerializer.Deserialize<Dialogue>(content);

		// Coloca todas as linhas na fila
		_dialogueQueue.Clear();
		foreach (var line in dialogueData.Lines)
		{
			_dialogueQueue.Enqueue(line);
		}

		// Inicia o processo
		_isDialogueActive = true;
		_dialogueUI.Show();
		DisplayNextLine();
	}

	private void DisplayNextLine()
	{
		if (_dialogueQueue.Count > 0)
		{
			var line = _dialogueQueue.Dequeue();
			_dialogueUI.SetDialogueName(line.Character);
			_dialogueUI.SetDialogueText(line.Text);
		}
		else
		{
			EndDialogue();
		}
	}

	private void EndDialogue()
	{
		_isDialogueActive = false;
		_dialogueUI.Hide();
		EmitSignal(SignalName.DialogueFinished);
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
