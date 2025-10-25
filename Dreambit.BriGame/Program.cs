

using Dreambit;
using Dreambit.BriGame;

using var game = new Core(1280, 720, "Bri Game") as Core;

Core.SetFixedTimeStep(false);

Window.SetAllowUserResizing(true);
Window.SetVsync(false);

LDtkManager.Instance.SetUp("BriWorld");

var scene = Scene.SetNextLDtkScene(Worlds.BriWorld.Level_0);

game.Run();