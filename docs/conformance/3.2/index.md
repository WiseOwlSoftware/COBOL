---
heading: "### 3.2 Nonstandard extensions (§2023:4.2.10; kb/Work PB1525)"
columns: ["Id", "Extension (class 1: a documented language element ISO does not define)", "Origin", "`--flag-extensions`", "Standard form"]
---
§2023:4.2.10: "Documentation associated with an implementation shall identify nonstandard extensions for which support is
claimed and shall specify any reserved words added for nonstandard extensions", and "An implementation shall provide a
warning mechanism that optionally may be invoked by the user at compile time to indicate use of a nonstandard extension
in a compilation group. This warning mechanism shall flag only extensions that are syntactically distinguishable"
(`cite.py --check 4.2.10` on both sentences → OK §2023:4.2.10 3)). This section is the first obligation and the **register**
behind it is the second: one table, `NonstandardExtensionRegister` (`src/WiseOwl.Cobol.Editions/NonstandardExtension.cs`), read
by the one binder seam `EditionContext.Extension`; `NonstandardExtensionRegisterDriftTests` keeps this table, that
register, the reserved-word table (D-RW1) and the grammar's `usageKeyword` equal, and refuses an accepted extension that
no binder site names.

**The warning mechanism is `cobol --flag-extensions`.** Off by default — a compile that does not ask is byte-identical to
one that never had the option. On, every use of an extension below that the compiler ACCEPTS draws **COBOL2894**, a
warning that names the construct, its origin and the standard form to write instead; it is orthogonal to `--std` and to
`--permissive`, and it never changes whether the program compiles. A row marked *not flagged* is documented and never
flagged, because it is not a property of the compilation group's text (§2023:4.2.10: "flag only extensions that are
syntactically distinguishable"). Adding an extension is adding a row: a usage spelling joins its row's spellings and the one
usage funnel (`PictureAnalyzer.ParseUsage`) flags it; a statement or phrase gets one `EditionContext.Extension` call at the
one place its binder accepts it.
