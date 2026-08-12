using System.Net.Mime;
using DreambitEngine.AssetBaker.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DreambitEngine.AssetBaker.Pipeline.Textures;

public class TextureBaker : AssetBakerBase
{
    private const uint FLAG_PREMUL = 1u << 0;
    private const uint FLAG_SRGB = 1u << 1;

    public override string AssetTypeName => "texture";
    public override string[] SupportedInputs => [".png", ".jpg", ".jpeg", ".bmp", ".tga"];
    public override string OutputExtension => ".texb";

    public override void Bake(BakeContext ctx)
    {
        var ext = Path.GetExtension(ctx.InputPath).ToLowerInvariant();
        if (!SupportedInputs.Contains(ext))
        {
            throw new NotSupportedException($"Unsupported asset type: {ext}");
        }
        
        using var img = Image.Load<Rgba32>(ctx.InputPath);
        
        // Resize if needed
        if (ctx.MaxDimension is var limit && (img.Width > limit || img.Height > limit))
        {
            var scale = Math.Min((float)limit / img.Width, (float)limit / img.Height);
            var nw = Math.Max(1, (int)MathF.Round(img.Width * scale));
            var nh = Math.Max(1, (int)MathF.Round(img.Height * scale));
            img.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(nw, nh),
                Sampler = KnownResamplers.Lanczos3,
                Mode = ResizeMode.Stretch,
                Compand = ctx.MarkSRgb
            }));
        }
        
        if (ctx.PremultiplyAlpha)
            PremultiplyAlpha(img, ctx.MarkSRgb);
        
        var mips = new List<(int w, int h, byte[] data)>
        {
            (img.Width, img.Height, DumpRgba(img))
        };

        if (ctx.GenerateMips)
        {
            int w = img.Width, h = img.Height;  
            using var baseRef = img.Clone();

            while (w > 1 || h > 1)
            {
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);
                using var mip = baseRef.Clone(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(w, h),
                    Sampler = KnownResamplers.Box,
                    Mode = ResizeMode.Stretch,
                    Compand = ctx.MarkSRgb
                }));
                mips.Add((w, h, DumpRgba(mip)));
            }
        }

        uint flags = 0;
        if (ctx.PremultiplyAlpha) flags |= FLAG_PREMUL;
        if (ctx.MarkSRgb)         flags |= FLAG_SRGB;

        TexbWriter.Write(ctx.OutputPath, mips, version: 1, flags);
    }

    public override AssetBlob BakeToBytes(BakeContext ctx)
    {
        using var img = Image.Load<Rgba32>(ctx.InputPath);

        if (ctx.MaxDimension is var limit && (img.Width > limit || img.Height > limit))
        {
            var scale = Math.Min((float)limit / img.Width, (float)limit / img.Height);
            var nw = Math.Max(1, (int)MathF.Round(img.Width * scale));
            var nh = Math.Max(1, (int)MathF.Round(img.Height * scale));
            img.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(nw, nh),
                Sampler = KnownResamplers.Lanczos3,
                Mode = ResizeMode.Stretch,
                Compand = ctx.MarkSRgb
            }));
        }

        if (ctx.PremultiplyAlpha)
            PremultiplyAlpha(img, ctx.MarkSRgb);

        var mips = new List<(int w,int h,byte[] data)>
        {
            (img.Width, img.Height, DumpRgba(img))
        };
        if (ctx.GenerateMips)
        {
            int w = img.Width, h = img.Height;
            using var baseRef = img.Clone();
            while (w > 1 || h > 1)
            {
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);
                using var mip = baseRef.Clone(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(w, h),
                    Sampler = KnownResamplers.Box,
                    Mode = ResizeMode.Stretch,
                    Compand = ctx.MarkSRgb
                }));
                mips.Add((w, h, DumpRgba(mip)));
            }
        }

        uint flags = 0;
        if (ctx.PremultiplyAlpha) flags |= FLAG_PREMUL;
        if (ctx.MarkSRgb)         flags |= FLAG_SRGB;

        using var ms = new MemoryStream(capacity: mips.Sum(m => m.data.Length) + 32);
        TexbWriter.Write(ms, mips, version: 1, flags);
        var data = ms.ToArray();

        var logical = GetLogicalPath(ctx, ".texb");
        return new AssetBlob(logical, AssetType.Texture, OutputExtension, data);
    }

    private static void PremultiplyAlpha(Image<Rgba32> image, bool srgb)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    ref var pixel = ref row[x];
                    var alpha = pixel.A / 255f;
                    pixel.R = PremultiplyChannel(pixel.R, alpha, srgb);
                    pixel.G = PremultiplyChannel(pixel.G, alpha, srgb);
                    pixel.B = PremultiplyChannel(pixel.B, alpha, srgb);
                }
            }
        });
    }

    private static byte PremultiplyChannel(byte channel, float alpha, bool srgb)
    {
        var encoded = channel / 255f;
        if (!srgb)
            return ToByte(encoded * alpha);

        var linear = encoded <= 0.04045f
            ? encoded / 12.92f
            : MathF.Pow((encoded + 0.055f) / 1.055f, 2.4f);
        linear *= alpha;
        var premultiplied = linear <= 0.0031308f
            ? linear * 12.92f
            : 1.055f * MathF.Pow(linear, 1f / 2.4f) - 0.055f;
        return ToByte(premultiplied);
    }

    private static byte ToByte(float value) =>
        (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);
    
    private static byte[] DumpRgba(Image<Rgba32> img)
    {
        var bytes = GC.AllocateUninitializedArray<byte>(img.Width * img.Height * 4);
        var o = 0;
        img.ProcessPixelRows(a =>
        {
            for (var y = 0; y < a.Height; y++)
            {
                var row = a.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var p = row[x];
                    bytes[o++] = p.R;
                    bytes[o++] = p.G;
                    bytes[o++] = p.B;
                    bytes[o++] = p.A;
                }
            }
        });
        return bytes;
    }
}
