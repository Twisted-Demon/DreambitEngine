using Dreambit;
using Dreambit.Examples.Pong;
using Microsoft.Xna.Framework;

using var game = new Core(720, 720);

Window.SetAllowUserResizing(true);

Core.Level = LogLevel.Trace;
var scene = new PongScene();
Scene.SetNextScene(scene);

game.Run();