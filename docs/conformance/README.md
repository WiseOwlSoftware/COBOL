# docs/conformance/ — the sources of WiseOwl COBOL's conformance statement

This directory holds the conformance statement that ISO/IEC 1989:2023 §2023:4.2.16 requires of an implementation, one
file per entry. The whole statement, `docs/CONFORMANCE.md`, is rendered from these files and is not tracked: run

    python scripts/rendered_views.py conformance

to write it (stamped on its first line; a hand edit of the rendered file is refused by `rendered_views.py --check`), or
`python scripts/spec/conformance_doc.py --check` to render it in memory and name any source out of shape. Edit the files
here, never the rendered whole. Every NuGet package carries the rendered statement at `docs/CONFORMANCE.md`, rendered
at pack time by the `CobolPackConformanceStatement` target in `Directory.Build.props`.

`head.md` is the title and status before section 1. Each section is a directory named by its number, holding `index.md`
(frontmatter `heading:`, and `columns:` for a table section; its body is the section's opening prose), an optional
`epilogue.md` (prose that speaks for the whole section, after its entries), and one file per entry named by the entry's
key. Frontmatter values are JSON, one `<name>: <value>` a line.

| Section | What it holds | One file per |
|---|---|---|
| `1/` | the conformance summary | (prose only) |
| `2/` | Annex A.3, the processor-dependent elements | A.3 item: `cells:`, and a body for a row's extended prose |
| `3/` | the behavior determinations | determination: `D-<KEY>.md`, the bullet as written |
| `3.1/` | the Annex A.2 undefined cases | determination: `D-<KEY>.md` |
| `3.2/` | the nonstandard extensions (§2023:4.2.10) | extension id: `cells:` |
| `3.3/` to `3.11/`, `3.7.1/` | the COBOL-85 determinations | (prose only) |
| `4/` | the documented non-support facilities and their determinations | block: `order:` (the place it stands), the block as written |
| `5/` | Annex A.4, the optional modules | A.4 module: `cells:` |
| `6/` | maintenance | (prose only) |
| `7/` | Annex A.1, the implementor-defined elements | row `DOC-A.1-<item>`: `cells:`, and a body for a row's extended prose |
| `8/` | the owner-signed derivations | row `DRV-<rule-id>`: `cells:` |

A new determination is a new `3/D-<KEY>.md` (the key unique in the whole statement); a new register row is a new file
in its section. Entries render in natural key order (`DOC-A.1-9` before `DOC-A.1-10`), section 4's blocks by `order:`.
The reader, the layout and the checks are `scripts/spec/conformance_source.py`; the design is
`docs/rearchitecture/DESIGN-orchestrator-loop.md` section 16.8.
