using PixelariaEngine;
using PixelariaEngine.Graphics;
using PixelariaEngine.Sandbox;

using var game = new PixelariaEngine.Core("PixelariaEngine", 1280, 720);
Window.SetAllowUserResizing(true);
Scene.SetNextScene(new TransitionSceneTest().AddRenderer<DefaultRenderer>());
game.Run();