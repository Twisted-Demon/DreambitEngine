using PixelariaEngine;
using PixelariaEngine.Graphics;
using PixelariaEngine.Sandbox;

using var game = new PixelariaEngine.Core("PixelariaEngine", 1280, 720);
game.SetNextScene(new TransitionSceneTest().AddRenderer<Basic2DRenderer>());
game.Run();