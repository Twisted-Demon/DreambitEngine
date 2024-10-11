using PixelariaEngine;
using PixelariaEngine.Graphics;
using PixelariaEngine.Sandbox;

//create the core
using var game = new Core("PixelariaEngine", 1280, 720);

Window.SetAllowUserResizing(true); //allow user resizing
LDtkManager.Instance.SetUp("AriaWorld");

//set the initial scene and & renderer
Scene.SetNextLDtkScene(Worlds.World.Dev_world);

//Run the game
game.Run();