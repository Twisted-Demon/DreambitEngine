using System;
using Dreambit.ECS;
using LDtk;

namespace Dreambit;

public class LDtkScene : Scene<LDtkScene>
{
    public LDtkLevel Level { get; set; }

    public string LevelIdentifier { get; internal set; }
    public Guid? LevelIid { get; internal init; }

    internal bool LoadByGuid { get; init; }

    protected override void OnInitialize()
    {
        if (LDtkManager.Instance.LDtkWorld == null)
        {
            Logger.Error("LDtk World Not Initialized, please call LDtkScene.SetUpWorld() First");
            return;
        }

        if (LevelIdentifier == null && LevelIid == null)
        {
            Logger.Error("Level Identifier not set, please modify the LDtkScene.LevelIdentifier value first");
            return;
        }

        //set up the level
        Level = LoadByGuid
            ? LDtkManager.Instance.LoadLDtkLevel((Guid)LevelIid!)
            : LDtkManager.Instance.LoadLDtkLevel(LevelIdentifier);

        BackgroundColor = Level._BgColor;

        //create the LDtk Renderer
        var ldtkRenderer = CreateEntity("LDtkRenderer")
            .AttachComponent<LDtkRenderer>();

        ldtkRenderer.Entity.AlwaysUpdate = true;

        ldtkRenderer.Level = Level;

        //set up all the entities
        LDtkManager.SetUpEntities(Level);

        //BackgroundColor = Level._BgColor;
    }

    protected override void OnEnd()
    {
        Level = null;
    }
}