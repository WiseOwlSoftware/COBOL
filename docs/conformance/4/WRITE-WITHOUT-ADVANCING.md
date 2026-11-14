---
order: 18
---
> ⚖ **DETERMINATION — a WRITE without the ADVANCING phrase is an explicit `AFTER ADVANCING 1 LINE` in every state
> of the device, and a file supports vertical positioning when its description carries a LINAGE clause or the program
> names an ADVANCING phrase on any WRITE of it** (2026-10-07, revised 2026-11-11 by kb/Work PB5201; kb/Work PB1027, adjudicated in wave 1033, implemented in
> wave 1037). §2023:14.9.51.4 GR25: "If the ADVANCING phrase is not used, automatic advancing shall be provided by the
> implementor to act as if the user has specified AFTER ADVANCING 1 LINE" (`cite.py --check 14.9.51.4` OK, 25). The
> standard names no implementor choice of PLACEMENT, so the two spellings are ONE path (`SequentialConnector.WriteRecord`
> reroutes the omitted phrase to `WriteAdvancingRecord(lines: 1, before: false)`), whatever the device holds: after a
> BEFORE write, whose own advance already ended the line, a plain WRITE advances AGAIN (one empty line, as AFTER
> does); on a LINAGE page the first plain WRITE after OPEN presents on body line 2 (§2023:13.18.34.4 GR7 d) puts the
> counter at 1, and the AFTER advance makes it 2, GR7 c) 3), and an overflowing plain WRITE presents ON line 1 of the
> next page (GR26 a) with the AFTER phrase implied); a line sequential file no longer travels twice around an AFTER
> write. Before PB1027 the omitted phrase took BEFORE's placement whenever no line was open, so each of those was one
> line off the file the spelled-out phrase writes. Pinned by `PlainWriteAdvancingConformanceTests` (every
> three-write mix of plain, AFTER and BEFORE over record sequential and line sequential files, with and without
> LINAGE, written both ways and compared byte for byte) and by `85/pb1027_plain_write_is_after_advancing_85` and
> `2023/pb1027_plain_write_is_after_advancing`.
> **The latitude, and where this determination departs from the wave-1033 adjudication.** GR25's own first
> sentences — "If the physical file does not support vertical positioning, the ADVANCING and END-OF-PAGE phrases are
> ignored" — leave to the implementor which physical files support it (Annex A.3 item 37 is claimed in §2, but the
> claim is that the PHRASES work; a text file on disk that no program ever asked to position is not a device that
> has a page). WiseOwl COBOL's determination: a file supports vertical positioning when (a) its description has a
> LINAGE clause (positioned from OPEN: GR7 d), or (b) any WRITE of it in the program names an ADVANCING phrase
> (`FileModel.WrittenWithAdvancing`, told to the connector at registration as `AdvancingDeclared`; a report file reaches
> the medium only through such a WRITE), or (c) an ADVANCING phrase has been written to it since the OPEN
> (`SequentialConnector.SupportsVerticalPositioning`; the EXTERNAL file another program positions).
> A file for which none holds — an ordinary text file or fixed-block data file written with plain WRITEs only —
> has nothing to position, so its plain WRITE is the record and its own line end (a line sequential delimiter, or
> nothing for a record sequential block): NO leading empty line, the shape GnuCOBOL 3.2 writes by default. The
> wave-1033 decision read every sequential file as positioned and would have given such a file a leading empty line,
> citing GnuCOBOL's `-fwrite-after`; that flag is NOT the default (default `cobc` writes `AAAAA` LF `BBBBB` LF for two
> plain WRITEs and treats a plain WRITE as a BEFORE write on every file), so the precedence ISO, then GnuCOBOL does
> not select it, and the operator's caveat of 2026-10-07 asked for the default probe before any output moved. No
> output of a plain-only file changes. **The capability is a property of the program, decided at compile time**
> (kb/Work PB5201, 2026-11-11, which closed the owner question PB1027 left open: the capability read from the run's
> history put a plain WRITE that preceded a file's first ADVANCING write on a line of its own, where §1985:VII.4.7.4
> GR16 and §2023:14.9.51.4 GR25 make it AFTER ADVANCING 1 LINE whichever WRITE of the file runs first): a plain `A`
> followed by `AFTER ADVANCING 1 LINE` `B` is LF `A` LF `B` LF, the file the all-spelled-out program writes
> (`1985-GR-VII_4_7_4-16-plain-first`). An EXTERNAL file shared by programs is a print file when ANY describing program writes an ADVANCING phrase to
> it (`FileRegistry.Register` ORs the declaration into the one connector), and one whose describers register after its
> first plain WRITE has run still inherits the capability at run time from the first ADVANCING write (`_printControl`).)
