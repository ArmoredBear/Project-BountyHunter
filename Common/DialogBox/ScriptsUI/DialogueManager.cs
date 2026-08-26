using Godot;
using System.Collections.Generic;
using System.Text.Json; // Essencial para parsear o JSON

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   DIALOGUE MANAGER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Loads and parses dialogue JSON files, queueing every line for playback.
	**  2 - Drives the DialogueUI, handling input to advance lines, and emits a finished signal.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class DialogueManager : Node
{
	//!---------------------------------------------------------------------------------------------------------
	#region Signals
	//!---------------------------------------------------------------------------------------------------------

	// Sinal emitido quando o diálogo termina. Outros nós podem ouvi-lo.
	[Signal]
	public delegate void DialogueFinishedEventHandler();

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	public Queue<DialogueLine> _dialogueQueue = new Queue<DialogueLine>();
	[Export] public  DialogueUI _dialogueUI;
	[Export] public bool _isDialogueActive = false;

	[Export] private DialogueManager _dialogueManager;

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		DialogueInitiation();

		// Carregamos a cena da UI de diálogo e a adicionamos como filha do manager.
		// Assim, o manager controla totalmente a sua UI.

		if(_dialogueUI == null)
		{
			GD.Print("Dialogue UI Null! Trying to reference it...");
			_dialogueUI = GetNode<DialogueUI>("%DialogueUI");
        }
		
		_dialogueUI.Hide(); // Começa escondida

		
	}

	public void DialogueInitiation()
	{
		_dialogueManager = this;
		_dialogueManager.DialogueFinished += OnDialogueFinished;

	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Só processa input se o diálogo estiver ativo
		if (_isDialogueActive && @event.IsActionPressed("Dialogue_Interact"))
		{
			// Marca o evento como "tratado" para que outros nós não o processem (ex: o jogador não pula)
			GetViewport().SetInputAsHandled();
			DisplayNextLine();
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

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
		// Inicia o diálogo passando o caminho para o arquivo JSON
		_dialogueManager.StartDialogue("res://Dialogue/intro_dialogue.json");
	}

	private void OnDialogueFinished()
	{
		GD.Print("O diálogo terminou! O jogo pode continuar.");
		// Reabilita o botão
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------
}
