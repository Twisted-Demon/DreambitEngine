# LDtk integration

Dreambit owns a raw LDtk 1.5.3 JSON model and loader. It does not create game
entities or render levels automatically. Games can interpret layer and entity
instances in whichever way fits their architecture.

The loader supports embedded and external levels, legacy single-world projects,
multi-world projects, tileset and background paths, file-path fields, and LDtk
entity references. Paths are resolved relative to the project file without
mutating the imported JSON values.

LDtk projects and external `.ldtkl` files are baked as normal Dreambit JSON
assets. Referenced PNG files continue through the texture baker.
