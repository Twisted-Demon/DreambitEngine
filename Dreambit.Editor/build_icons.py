#!/usr/bin/env python3
"""
Dreambit Editor Icon Baker
==========================

Supports:
    - SVG source icons via resvg_py
    - PNG source icons via Pillow
    - Pure-white RGB with preserved alpha for ImGui tinting
    - Individual 24x24 / 32x32 / 64x64 PNG textures
    - atlas_32.png
    - atlas_32_manifest.json
    - Recursive category folders
    - Duplicate icon-name validation
    - Deterministic atlas ordering
    - Automatic bootstrap from an existing png_64 folder

Install:
    python -m pip install pillow resvg_py

Usage:
    python build_icons_resvg.py

Optional:
    python build_icons_resvg.py --icons-dir ./Icons
    python build_icons_resvg.py --sizes 24 32 64
    python build_icons_resvg.py --atlas-icon-size 32

Expected layout:

    Dreambit.Editor/
        build_icons_resvg.py
        Icons/
            Source/
                01_Viewport/
                    mouse.svg
                    open_with.svg
                02_Runtime/
                    play_arrow.svg
                    stop.svg
                06_Components/
                    camera_alt.svg

Generated layout:

    Icons/
        Source/
            ...your originals...

        png_24/
            ...generated PNGs...

        png_32/
            ...generated PNGs...

        png_64/
            ...generated PNGs...

        atlas_32.png
        atlas_32_manifest.json

Your EditorIconService only needs:
    Icons/atlas_32.png
    Icons/atlas_32_manifest.json

The loose PNG folders are generated as convenience assets.
"""

from __future__ import annotations

import argparse
import json
import math
import shutil
import sys
from dataclasses import dataclass
from io import BytesIO
from pathlib import Path
from typing import Iterable

try:
    from PIL import Image
except ImportError:
    print(
        "\nERROR: Pillow is not installed.\n\n"
        "Install dependencies with:\n"
        "    python -m pip install pillow resvg_py\n",
        file=sys.stderr,
    )
    raise SystemExit(1)


SUPPORTED_EXTENSIONS = {".png", ".svg"}
DEFAULT_SIZES = (24, 32, 64)
DEFAULT_ATLAS_ICON_SIZE = 32


@dataclass(frozen=True)
class SourceIcon:
    name: str
    category: str
    path: Path
    relative_path: Path


@dataclass(frozen=True)
class GeneratedIcon:
    name: str
    category: str
    source: Path
    png_paths: dict[int, Path]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build Dreambit Editor ImGui icon textures and atlas."
    )
    parser.add_argument(
        "--icons-dir",
        type=Path,
        default=Path("Icons"),
        help="Dreambit Editor Icons directory. Default: ./Icons",
    )
    parser.add_argument(
        "--sizes",
        type=int,
        nargs="+",
        default=list(DEFAULT_SIZES),
        help="Individual PNG sizes to generate. Default: 24 32 64",
    )
    parser.add_argument(
        "--atlas-icon-size",
        type=int,
        default=DEFAULT_ATLAS_ICON_SIZE,
        help="Raster size used for atlas icons. Default: 32",
    )
    parser.add_argument(
        "--columns",
        type=int,
        default=0,
        help="Atlas column count. 0 = choose automatically.",
    )
    parser.add_argument(
        "--no-bootstrap",
        action="store_true",
        help=(
            "Do not automatically create Icons/Source from an existing "
            "Icons/png_64 directory."
        ),
    )
    return parser.parse_args()


def bootstrap_source_if_needed(
    icons_dir: Path,
    source_dir: Path,
    allow_bootstrap: bool,
) -> None:
    if source_dir.exists() or not allow_bootstrap:
        return

    existing = icons_dir / "png_64"
    if not existing.exists():
        return

    print(f"[bootstrap] Creating source directory: {source_dir}")
    print(f"[bootstrap] Copying existing icons from: {existing}")

    source_dir.mkdir(parents=True, exist_ok=True)

    copied = 0
    for path in sorted(existing.rglob("*.png")):
        relative = path.relative_to(existing)
        target = source_dir / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)
        copied += 1

    print(f"[bootstrap] Copied {copied} icons.")
    print()


def discover_icons(source_dir: Path) -> list[SourceIcon]:
    if not source_dir.exists():
        raise RuntimeError(
            f"Source directory does not exist:\n"
            f"    {source_dir}\n\n"
            "Create it and place SVG/PNG icons inside it, for example:\n\n"
            "    Icons/Source/01_Viewport/mouse.svg\n"
            "    Icons/Source/02_Runtime/play_arrow.svg\n"
        )

    paths = sorted(
        (
            path
            for path in source_dir.rglob("*")
            if path.is_file() and path.suffix.lower() in SUPPORTED_EXTENSIONS
        ),
        key=lambda path: path.relative_to(source_dir).as_posix().casefold(),
    )

    if not paths:
        raise RuntimeError(
            f"No SVG or PNG icons were found under:\n"
            f"    {source_dir}"
        )

    icons: list[SourceIcon] = []
    names_seen: dict[str, Path] = {}

    for path in paths:
        relative = path.relative_to(source_dir)
        name = path.stem
        normalized_name = name.casefold()

        if normalized_name in names_seen:
            previous = names_seen[normalized_name]
            raise RuntimeError(
                "Duplicate icon name detected.\n\n"
                f"Icon key:\n"
                f"    {name}\n\n"
                f"First file:\n"
                f"    {previous}\n\n"
                f"Second file:\n"
                f"    {path}\n\n"
                "EditorIconService keys icons only by filename, so icon names "
                "must be globally unique even when stored in different folders."
            )

        names_seen[normalized_name] = path

        parent = relative.parent
        category = parent.as_posix() if parent != Path(".") else "Uncategorized"

        icons.append(
            SourceIcon(
                name=name,
                category=category,
                path=path,
                relative_path=relative,
            )
        )

    return icons


def load_svg(path: Path, size: int) -> Image.Image:
    try:
        import resvg_py
    except ImportError:
        raise RuntimeError(
            "SVG source icon found, but resvg_py is not installed.\n\n"
            f"SVG:\n"
            f"    {path}\n\n"
            "Install dependencies with:\n\n"
            "    python -m pip install pillow resvg_py\n"
        ) from None

    try:
        png_bytes = resvg_py.svg_to_bytes(
            svg_path=str(path),
            width=size,
            height=size,
        )
    except Exception as exc:
        raise RuntimeError(
            f"Failed to rasterize SVG:\n"
            f"    {path}\n\n"
            f"resvg_py error:\n"
            f"    {exc}"
        ) from exc

    try:
        with Image.open(BytesIO(png_bytes)) as image:
            return image.convert("RGBA")
    except Exception as exc:
        raise RuntimeError(
            f"resvg_py produced an unreadable PNG for:\n"
            f"    {path}\n\n"
            f"Pillow error:\n"
            f"    {exc}"
        ) from exc


def load_png(path: Path, size: int) -> Image.Image:
    try:
        with Image.open(path) as source:
            source = source.convert("RGBA")

            if source.width <= 0 or source.height <= 0:
                raise RuntimeError(f"Invalid PNG dimensions:\n    {path}")

            scale = min(size / source.width, size / source.height)
            width = max(1, round(source.width * scale))
            height = max(1, round(source.height * scale))

            resized = source.resize(
                (width, height),
                Image.Resampling.LANCZOS,
            )

            canvas = Image.new(
                "RGBA",
                (size, size),
                (255, 255, 255, 0),
            )

            x = (size - width) // 2
            y = (size - height) // 2

            canvas.alpha_composite(resized, (x, y))
            return canvas

    except RuntimeError:
        raise
    except Exception as exc:
        raise RuntimeError(
            f"Failed to load PNG:\n"
            f"    {path}\n\n"
            f"Pillow error:\n"
            f"    {exc}"
        ) from exc


def make_white_tintable(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    alpha = image.getchannel("A")

    output = Image.new(
        "RGBA",
        image.size,
        (255, 255, 255, 0),
    )
    output.putalpha(alpha)
    return output


def render_source(icon: SourceIcon, size: int) -> Image.Image:
    extension = icon.path.suffix.lower()

    if extension == ".svg":
        image = load_svg(icon.path, size)
    elif extension == ".png":
        image = load_png(icon.path, size)
    else:
        raise RuntimeError(f"Unsupported icon format:\n    {icon.path}")

    return make_white_tintable(image)


def clean_generated_size_directories(
    icons_dir: Path,
    sizes: Iterable[int],
) -> None:
    for size in sizes:
        directory = icons_dir / f"png_{size}"
        if directory.exists():
            shutil.rmtree(directory)


def generate_individual_pngs(
    icons_dir: Path,
    source_icons: list[SourceIcon],
    sizes: list[int],
) -> list[GeneratedIcon]:
    clean_generated_size_directories(icons_dir, sizes)

    generated: list[GeneratedIcon] = []
    total = len(source_icons)

    for index, icon in enumerate(source_icons, start=1):
        png_paths: dict[int, Path] = {}

        for size in sizes:
            output_directory = icons_dir / f"png_{size}" / icon.category
            output_directory.mkdir(parents=True, exist_ok=True)

            output_path = output_directory / f"{icon.name}.png"

            rendered = render_source(icon, size)
            rendered.save(output_path, "PNG")

            png_paths[size] = output_path

        generated.append(
            GeneratedIcon(
                name=icon.name,
                category=icon.category,
                source=icon.path,
                png_paths=png_paths,
            )
        )

        print(f"[{index:>3}/{total}] {icon.category}/{icon.name}")

    return generated


def verify_white_pngs(
    generated: list[GeneratedIcon],
    sizes: list[int],
) -> None:
    for icon in generated:
        for size in sizes:
            path = icon.png_paths[size]

            with Image.open(path) as image:
                rgba = image.convert("RGBA")

                for channel_name in ("R", "G", "B"):
                    minimum, maximum = rgba.getchannel(channel_name).getextrema()

                    if minimum != 255 or maximum != 255:
                        raise RuntimeError(
                            "Tint validation failed.\n\n"
                            f"File:\n"
                            f"    {path}\n\n"
                            f"Channel {channel_name} is not pure white."
                        )


def choose_columns(icon_count: int) -> int:
    desired = max(1, math.ceil(math.sqrt(icon_count)))

    columns = 1
    while columns < desired:
        columns *= 2

    return columns


def build_atlas(
    icons_dir: Path,
    generated: list[GeneratedIcon],
    atlas_icon_size: int,
    requested_columns: int,
) -> None:
    if not generated:
        raise RuntimeError("Cannot build an atlas with zero icons.")

    if atlas_icon_size not in generated[0].png_paths:
        raise RuntimeError(
            f"Atlas size {atlas_icon_size} was not generated.\n"
            f"Include it in --sizes."
        )

    icon_count = len(generated)

    columns = (
        requested_columns
        if requested_columns > 0
        else choose_columns(icon_count)
    )

    if columns <= 0:
        raise RuntimeError("Atlas column count must be greater than zero.")

    rows = math.ceil(icon_count / columns)

    atlas_width = columns * atlas_icon_size
    atlas_height = rows * atlas_icon_size

    atlas = Image.new(
        "RGBA",
        (atlas_width, atlas_height),
        (255, 255, 255, 0),
    )

    entries: list[dict] = []

    for index, icon in enumerate(generated):
        column = index % columns
        row = index // columns

        x = column * atlas_icon_size
        y = row * atlas_icon_size

        source_path = icon.png_paths[atlas_icon_size]

        with Image.open(source_path) as source:
            atlas.alpha_composite(
                source.convert("RGBA"),
                (x, y),
            )

        entries.append(
            {
                "index": index,
                "icon": icon.name,
                "category": icon.category,
                "x": x,
                "y": y,
                "width": atlas_icon_size,
                "height": atlas_icon_size,
                "u0": x / atlas_width,
                "v0": y / atlas_height,
                "u1": (x + atlas_icon_size) / atlas_width,
                "v1": (y + atlas_icon_size) / atlas_height,
            }
        )

    atlas_filename = f"atlas_{atlas_icon_size}.png"
    manifest_filename = f"atlas_{atlas_icon_size}_manifest.json"

    atlas_path = icons_dir / atlas_filename
    manifest_path = icons_dir / manifest_filename

    atlas.save(atlas_path, "PNG")

    manifest = {
        "atlas": atlas_filename,
        "cell_size": atlas_icon_size,
        "atlas_width": atlas_width,
        "atlas_height": atlas_height,
        "columns": columns,
        "rows": rows,
        "icons": entries,
    }

    manifest_path.write_text(
        json.dumps(manifest, indent=2) + "\n",
        encoding="utf-8",
    )

    print()
    print(f"[atlas]    {atlas_path}")
    print(f"           {atlas_width}x{atlas_height}")
    print(f"           {columns} columns x {rows} rows")
    print(f"[manifest] {manifest_path}")


def main() -> int:
    args = parse_args()

    icons_dir = args.icons_dir.resolve()
    source_dir = icons_dir / "Source"

    sizes = sorted(set(args.sizes))

    if any(size <= 0 for size in sizes):
        raise RuntimeError("All icon sizes must be greater than zero.")

    atlas_icon_size = args.atlas_icon_size

    if atlas_icon_size <= 0:
        raise RuntimeError("--atlas-icon-size must be greater than zero.")

    if atlas_icon_size not in sizes:
        sizes.append(atlas_icon_size)
        sizes.sort()

    icons_dir.mkdir(parents=True, exist_ok=True)

    bootstrap_source_if_needed(
        icons_dir=icons_dir,
        source_dir=source_dir,
        allow_bootstrap=not args.no_bootstrap,
    )

    source_icons = discover_icons(source_dir)

    print()
    print("Dreambit Editor Icon Baker")
    print("==========================")
    print()
    print(f"Source: {source_dir}")
    print(f"Output: {icons_dir}")
    print(f"Icons:  {len(source_icons)}")
    print(f"Sizes:  {', '.join(map(str, sizes))}")
    print()

    generated = generate_individual_pngs(
        icons_dir=icons_dir,
        source_icons=source_icons,
        sizes=sizes,
    )

    print()
    print("[validate] Checking tint-ready RGB...")

    verify_white_pngs(
        generated=generated,
        sizes=sizes,
    )

    print("[validate] All generated icons are pure white + alpha.")

    build_atlas(
        icons_dir=icons_dir,
        generated=generated,
        atlas_icon_size=atlas_icon_size,
        requested_columns=args.columns,
    )

    print()
    print("Done.")
    print()
    print("EditorIconService runtime files:")
    print(f"    Icons/atlas_{atlas_icon_size}.png")
    print(f"    Icons/atlas_{atlas_icon_size}_manifest.json")
    print()

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except RuntimeError as error:
        print()
        print("ERROR:", error, file=sys.stderr)
        print()
        raise SystemExit(1)
