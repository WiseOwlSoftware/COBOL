---
order: 5
---
Beyond these four whole facilities, two of the three **optional items of an otherwise-claimed module** are likewise
refused by name (Annex **A.4.10**, "Object orientation" — the OO core itself is mandatory surface and is implemented),
each through its own `DiagnosticCatalog` descriptor carrying `Annex: A4` and the `declined-optional-element` suppress
family (kb/Work PB745): item 1, the repetition of `object-class-name-2` in a `CLASS-ID … INHERITS` clause (§2023:11.3.2 —
multiple inheritance), **COBOL0849**; and item 3, **parametric polymorphism** (§2023:9.3.5.3 — overloading by method
resolution signature: a second method of one name in a factory or instance definition, or a second prototype of one
name in an interface), **COBOL0822**. Item 2, the `INTERFACE-ID … INHERITS` repetition (§2023:11.6.2), is
**implemented** — `INHERITS FROM interface-name-2 …` parses, resolves and enforces §2023:11.6.3 SR2 (COBOL0840) and
SR3/SR6 (COBOL2791, the INHERITS-clause rule it shares with §2023:11.3.3 SR3/SR4/SR5/SR7); the deliberate asymmetry
with the class side is that .NET interface lists are native.
