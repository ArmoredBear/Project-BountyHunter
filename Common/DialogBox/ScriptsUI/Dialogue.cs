using System.Collections.Generic;

// Representa uma única linha de diálogo no JSON.
public class DialogueLine
{
	public string Character { get; set; }
	public string Text { get; set; }
}

// Representa o arquivo JSON inteiro, que contém uma lista de linhas.
public class Dialogue
{
	public List<DialogueLine> Lines { get; set; }
}
