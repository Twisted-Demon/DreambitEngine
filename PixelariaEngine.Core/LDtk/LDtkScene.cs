using LDtk;
using PixelariaEngine.ECS;

namespace PixelariaEngine;

public class LDtkScene : Scene<LDtkScene>
{
    private string _levelIdentifier;

    private LDtkLevel Level { get; set; }

    public string LevelIdentifier
    {
        get => _levelIdentifier;
        set
        {
            if (_levelIdentifier != null) return;
            _levelIdentifier = value;
        }
    }

    public Scene SetUpWorld(string worldName)
    {
        LDtkManager.Instance.SetUp(worldName);

        return this;
    }

    protected override void OnInitialize()
    {
        if (LDtkManager.Instance.LDtkWorld == null)
        {
            Logger.Error("LDtk World Not Initialized, please call LDtkScene.SetUpWorld() First");
            return;
        }

        if (_levelIdentifier == null)
        {
            Logger.Error("Level Identifier not set, please modify the LDtkScene.LevelIdentifier value first");
            return;
        }

        //set up the level
        Level = LDtkManager.Instance.LoadLDtkLevel(_levelIdentifier);

        //create the LDtk Renderer
        var ldtkRenderer = CreateEntity("LDtkRenderer")
            .AttachComponent<LDtkRenderer>();

        ldtkRenderer.Level = Level;

        //set up all the entities
        LDtkManager.SetUpEntities(Level);
    }
}