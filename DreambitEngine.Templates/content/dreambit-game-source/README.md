# DreambitGame

This project consumes DreambitEngine directly from the `.dreambit/engine` Git submodule.
The source template initializes that submodule automatically when created through `dotnet new`.

```powershell
dotnet build DreambitGame.sln
dotnet run --project src/DreambitGame.VK/DreambitGame.VK.csproj
```

Asset baking maintains incremental blobs under `.cache/dreambit/bake`, and Debug builds copy those
blobs into the launcher's `Content` directory. Before a Release build or publish, choose
**Build > Bake Pak** in Dreambit Editor. Release verifies that the PAK matches the current blobs and
copies it into the output.
