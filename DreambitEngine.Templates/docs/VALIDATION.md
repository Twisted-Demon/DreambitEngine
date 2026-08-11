# Validation performed

The template smoke tests verify:

- valid template JSON and project XML;
- coordinated runtime/build/template packages with one SDK version;
- isolated template installation and generation;
- portable `.dreambit/project.json` with a generated project ID;
- repository-level central package versions;
- exactly the game, Content, and DesktopVK projects in the generated solution;
- no source submodule, setup scripts, missing content-target import, or machine path;
- no unresolved Dreambit template placeholders;
- successful package restore from the local SDK feed;
- successful generated-solution build.

Run `scripts/test-template.ps1` on Windows or `scripts/test-template.sh` on macOS/Linux.
