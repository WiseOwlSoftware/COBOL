---
order: 9
---
> ⚖ **DETERMINATION — ALLOCATE's GR8/GR9 fill keys on the OPTIONS INITIALIZE clause's PRESENCE, not its
> section list** (2026-08-29; kb/Work PB151). §2023:14.9.3.4 GR8/GR9 say the non-INITIALIZED allocated content
> "depends on the INITIALIZE clause of the OPTIONS paragraph. If it is specified, the content is that of the
> specified-fill-character" — the rules reference the CLAUSE, and allocated storage belongs to no section, so
> the clause's LOCAL-STORAGE/SCREEN/WORKING-STORAGE target list does not scope it: any written INITIALIZE
> clause supplies the fill. No clause → space (GR8's "undefined", the conformant choice). INITIALIZED always
> wins with binary zeros (GR6). Pinned by `2023/pb151_options_fill` and, against the other two consuming arms,
> by `2023/pb152_options_initialize_arm_agreement`.
> ⛔ **CORRECTED 2026-09-02 (kb/Work PB152) — this paragraph used to state the fill map itself, and it was
> WRONG in two ways.** It said a literal fill takes its "first character", which admitted spellings
> §2023:11.9.10.3 SR1 forbids (see the §2023:11.9.10 determination below); and it said HIGH-VALUES is U+FFFF, while every
> other HIGH-VALUE in this compiler was then the program collating sequence's highest character (§2023:8.3.3.6.4 GR6),
> U+00FF under the native sequence (U+FFFF since owner decision kb/Work R52). That second answer existed only because the ALLOCATE arm carried a PRIVATE
> copy of the §2023:11.9.10.4 GR5 map. There is now ONE resolution of the specified-fill-character
> (`InitialStateBackground`, over `FigurativeConstants.FillChar`) shared by all three arms, and this document
> no longer restates the map.
