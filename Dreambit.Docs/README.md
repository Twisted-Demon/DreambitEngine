# Dreambit.Docs

The source for the Dreambit engine documentation. The site uses MkDocs and the
built-in ReadTheDocs theme, so it has no theme-specific configuration.

## Preview locally

```powershell
cd Dreambit.Docs
python -m pip install -r requirements.txt
python -m mkdocs serve
```

Open `http://127.0.0.1:8000`. MkDocs watches the Markdown files and reloads the
site as you edit them.

## Validate a production build

```powershell
python -m mkdocs build --strict
```

The generated static site is written to `Dreambit.Docs/site/`.

