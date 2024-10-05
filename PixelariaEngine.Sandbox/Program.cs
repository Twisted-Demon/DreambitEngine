using PixelariaEngine;
using PixelariaEngine.Graphics;
using PixelariaEngine.Sandbox;

//create the core
using var game = new Core("PixelariaEngine", 1280, 720);

Window.SetAllowUserResizing(true); //allow user resizing

//set the initial scene and & renderer
Scene.SetNextScene(new TransitionSceneTest()
    .AddRenderer<DefaultRenderer>());

//Run the game
game.Run();