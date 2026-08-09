using Dreambit;
using DreambitGame;

using var engine = new Core(
    title: "Dreambit Game",
    width: 1280,
    height: 720);

Core.Level = LogLevel.Info;
Core.SetTargetFps(__DREAMBIT_TARGET_FPS__);

Scene.SetNextScene<MainScene>();

engine.Run();
