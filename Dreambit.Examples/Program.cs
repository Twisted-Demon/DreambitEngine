using Dreambit;
using Dreambit.Examples.Particles;
using Dreambit.Examples.Pong;
using Dreambit.Examples.SpaceGame;
using Dreambit.Examples.UiExample;
using Microsoft.Xna.Framework;

using var game = new Core(1280, 720);

Window.SetAllowUserResizing(true);
Window.SetFixedTimeStep(false);
Window.SetVsync(false);

Core.Level = LogLevel.Trace;
var scene = new UiExampleScene();
Scene.SetNextScene(scene);

game.Run();
