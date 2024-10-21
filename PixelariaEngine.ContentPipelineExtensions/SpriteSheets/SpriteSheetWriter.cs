using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Dreambit;

[ContentTypeWriter]
public class SpriteSheetWriter : ContentTypeWriter<SpriteSheet>
{
    public override string GetRuntimeReader(TargetPlatform targetPlatform)
    {
        var type = typeof(SpriteSheetReader);
        
        var typeName = type.FullName;
        var assemblyName = type.Assembly.GetName().Name;
        
        return $"{typeName}, {assemblyName}";
    }

    protected override void Write(ContentWriter output, SpriteSheet spriteSheet)
    {
        // write the texture path
        output.Write(spriteSheet.TexturePath);
        
        // write the number of columns
        output.Write(spriteSheet.Columns);
        
        // write the number of rows
        output.Write(spriteSheet.Rows);
    }
}