using PixelariaEngine;
using PixelariaEngine.Graphics;
using PixelariaEngine.Sandbox;

//create the core
using var game = new Core("PixelariaEngine", 1280, 720);

Window.SetAllowUserResizing(true); //allow user resizing

//set the initial scene and & renderer
var scene = new LDtkScene().SetUpWorld("AriaWorld")
    .AddRenderer<DefaultRenderer>() as LDtkScene;

scene!.LevelIdentifier = "Dev_world";

Scene.SetNextScene(scene);

//Run the game
game.Run();