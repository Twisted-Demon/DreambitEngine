using Dreambit;
using Dreambit.Examples.Particles;
using Dreambit.Examples.Pong;
using Dreambit.Examples.SpaceGame;
using Microsoft.Xna.Framework;

using var game = new Core(1280, 720);

Window.SetAllowUserResizing(true);
Window.SetFixedTimeStep(false);

Core.Level = LogLevel.Trace;
var scene = new SpaceGameScene();
Scene.SetNextScene(scene);

game.Run();