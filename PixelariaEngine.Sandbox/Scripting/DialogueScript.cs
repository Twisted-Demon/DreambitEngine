using Microsoft.Xna.Framework.Input;
using PixelariaEngine.ECS;
using PixelariaEngine.Sandbox.UI;
using PixelariaEngine.Scripting;

namespace PixelariaEngine.Sandbox;

public class DialogueScript : Script
{
    private DialogueCanvas _dialogueCanvas;
    private readonly string _entityName;
    private readonly string _text;

    public DialogueScript(string entity, string text)
    {
        _entityName = entity;
        _text = text;
    }

    public override void OnStart()
    {
        var (canvas, _) = Canvas.Create<DialogueCanvas>();
        canvas.Entity.Enabled = true;

        canvas.StartDialogue($"{_entityName}: {_text}");
        _dialogueCanvas = canvas;
    }

    public override void OnUpdate()
    {
        if (!Input.IsKeyPressed(Keys.Enter)) return;
        Entity.Destroy(_dialogueCanvas.Entity);
        IsComplete = true;
    }
}