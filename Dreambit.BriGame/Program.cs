using Dreambit;
using Dreambit.BriGame;
using Dreambit.BriGame.Scenes;
using Dreambit.BriGame.Scenes.Galaga;


using var game = new Core(1280,720, "Galaga Clone");

Core.SetFixedTimeStep(false);
Window.SetAllowUserResizing(true);
Window.SetVsync(false);
Window.CenterOnPrimaryDisplay();

Core.Level = LogLevel.Trace;

LDtkManager.Instance.SetUp("BriWorld");
LDtkManager.Instance.LoadWorld(Worlds.BriWorld.Iid);
Scene.SetNextLDtkScene<BriWorldScene>(Worlds.BriWorld.Level_0);


game.Run();