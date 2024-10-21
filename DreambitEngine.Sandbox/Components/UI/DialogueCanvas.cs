using Dreambit.ECS;

namespace Dreambit.Sandbox.UI;

public class DialogueCanvas : Canvas
{
    private float _width, _height;
    public UINineSlice DialogueBox { get; set; }
    public UIText DialogueText { get; set; }
    public string Text { get; set; } = string.Empty;
    private string _textToDisplay = string.Empty;
    private int _currentCharIndex;
    private float _textTimer;
    private readonly float _textInterval = 0.035f;
    
    public override void OnCreated()
    {
        _width = 56.0f;
        _height = 25.0f;
        
        Transform.Position.X = _width * -0.5f;
        Transform.Position.Y = _height;

        DialogueBox = UINineSlice.Create(this, _width, _height, "Textures/NineSlice");
        DialogueBox.PivotType = PivotType.TopLeft;

        DialogueText = UIText.Create(this, "");
        DialogueText.FontName = "Fonts/monogram-font";
        DialogueText.VTextAlignment = VerticalAlignment.Top;
        DialogueText.HTextAlignment = HorizontalAlignment.Left;
        DialogueText.MaxWidth = 56;
        DialogueText.Transform.Position.X += 1;
        DialogueText.Transform.Position.Y += 1;
        DialogueText.MaxWidth -= 2;
    }

    public override void OnUpdate()
    {
        _textTimer += Time.DeltaTime;
        
        if(!(_textTimer >= _textInterval)) return;
        _textTimer = 0.0f;
        
        _currentCharIndex++;
        
        if (_currentCharIndex >= Text.Length) return;
        
        _textToDisplay += Text[_currentCharIndex];
        DialogueText.Text = _textToDisplay;
    }

    public void StartDialogue(string textToDisplay)
    {
        Text = textToDisplay;
        _textToDisplay = string.Empty;
        _textToDisplay += Text[0];
        DialogueText.Text = _textToDisplay;
        Entity.Enabled = true;
    }

    public void EndDialogue()
    {
        Entity.Enabled = false;
        Text = string.Empty;
        _textToDisplay = string.Empty;
        _currentCharIndex = 0;
    }
}