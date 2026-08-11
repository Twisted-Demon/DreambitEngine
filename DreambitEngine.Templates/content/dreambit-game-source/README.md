# DreambitGame

This project consumes DreambitEngine directly from the `.dreambit/engine` Git submodule.
The source template initializes that submodule automatically when created through `dotnet new`.

```powershell
dotnet build DreambitGame.sln
dotnet run --project src/DreambitGame.VK/DreambitGame.VK.csproj
```

Asset baking writes `.cache/dreambit/content.pak`. Build and publish copy the completed PAK
into the launcher's `Content` directory; source assets are never baked directly into a running
output folder.
