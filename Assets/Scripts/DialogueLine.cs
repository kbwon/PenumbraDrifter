using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string speakerName;

    [TextArea(2, 5)]
    public string text;

    public Sprite portrait;

    public DialogueLine() { }

    public DialogueLine(string speakerName, string text)
    {
        this.speakerName = speakerName;
        this.text = text;
    }
}
