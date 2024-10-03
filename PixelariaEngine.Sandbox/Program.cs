using PixelariaEngine;
using PixelariaEngine.Sandbox;

using var game = new PixelariaEngine.Core("PixelariaEngine", 1280, 720);
game.SetNextScene(new TransitionSceneTest());
game.Run();