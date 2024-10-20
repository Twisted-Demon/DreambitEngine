using PixelariaEngine;
using PixelariaEngine.Sandbox;

//create the core
using var game = new Core("PixelariaEngine", 1280, 720);

Window.SetAllowUserResizing(true); //allow user resizing

LDtkManager.Instance.SetUp("AriaWorld");

var scene = Scene.SetNextLDtkScene(Worlds.AriaWorld.PreloadLevel);
scene.AddRenderer<DefaultRenderer>();

//Run the game
game.Run();