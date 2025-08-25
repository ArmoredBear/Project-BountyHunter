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

	public override void _Ready()
	{
		// Carregamos a cena da UI de diálogo e a adicionamos como filha do manager.
		// Assim, o manager controla totalmente a sua UI.
		var dialogueUIScene = GD.Load<PackedScene>("res://Common/DialogueUI.tscn");
		_dialogueUI = dialogueUIScene.Instantiate<DialogueUI>();
		AddChild(_dialogueUI);
		_dialogueUI.Hide(); // Começa escondida
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
			_dialogueUI.SetName(line.Character);
			_dialogueUI.SetText(line.Text);
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
}
