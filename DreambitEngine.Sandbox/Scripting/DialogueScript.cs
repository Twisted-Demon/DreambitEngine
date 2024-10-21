using Dreambit.ECS;
using Dreambit.Sandbox.UI;
using Dreambit.Scripting;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.Sandbox;

public class DialogueScript : ScriptAction
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
        IsComplete = true;
    }

    public override void OnGroupEnd()
    {
        Entity.Destroy(_dialogueCanvas.Entity);
    }
}