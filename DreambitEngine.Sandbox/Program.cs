using System;
using System.IO;
using System.Reflection;
using Dreambit;
using Dreambit.Sandbox;


//create the core
using var game = new Core(1280, 720);

Window.SetAllowUserResizing(true); //allow user resizing
Window.SetVsync(false);

LDtkManager.Instance.SetUp("AriaWorld");

var scene = Scene.SetNextLDtkScene(Worlds.AriaWorld.PreloadLevel);

//Run the game
game.Run();