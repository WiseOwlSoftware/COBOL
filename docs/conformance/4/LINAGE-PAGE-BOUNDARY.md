---
order: 15
---
> ⚖ **DETERMINATION — the §2023:14.9.51.4 GR26 a)/b) boundary at LINAGE-COUNTER = page size** (2026-09-05; kb/Work
> PB686). GR26's two arms cannot both be honoured at that one counter value. Arm a) fires when the counter "is
> equal to or exceeds the page size"; arm b) is clamped to "equal to or exceeds the current value of the footing
> start **and is less than the page size**". Both were verified against the PDF text layer (printed page 792) —
> **this is the standard's own text, not a transcription defect.** Read literally, a WRITE landing the counter
> exactly ON the page size is arm a): page overflow, the record pushed to the first line of the next logical page,
> the counter reset to one. **WiseOwl COBOL rejects that reading.** Both arms are evaluated on the counter the write's
> advancing operation produces, and the boundary is STRICT: `counter > page size` is arm a); `footing start ≤
> counter ≤ page size` is arm b). Three other rules force it. §2023:13.18.34.4 GR2 — the page size is "the number of
> lines that may be written or spaced on the logical page" — and under the literal reading the line NUMBERED page
> size can never receive a record from an AFTER-advancing write, so an N-line body would hold at most N−1 written
> lines on every page, forever; that is also true of a file with NO FOOTING phrase, which is the arm a
> FOOTING-only fixture cannot see. §2023:13.18.34.4 GR3 puts that same line INSIDE the footing area ("between the
> footing start and the page size, **inclusive**"), which arm b)'s clamp then excludes. And GR26's own lead
> sentence conditions the whole rule on lines that "do not fit within the current page body" — a line landing on
> the last body line fits. §2023:13.18.34.4 GR7 c) 1 corroborates the strict boundary from the other side: the
> standard's way of making arm a) fire for ADVANCING PAGE is to say the counter "is implicitly incremented to
> **exceed** the value specified by integer-1" — equality would have sufficed had equality been the trigger.
> **The survey does not split** (the 2026-08-30 standing answer): IBM Enterprise COBOL for z/OS 6.3 states the
> footing END-OF-PAGE as the counter reaching a value that "equal[s] or exceed[s] the value specified in the WITH
> FOOTING phrase" with **no upper clamp**, and page overflow as a write that "would cause the value in the
> LINAGE-COUNTER to **exceed** the number of lines for the page body"; Micro Focus Visual COBOL documents the
> footing area as "the area between footing-line and page-lines, **inclusive**"; and GnuCOBOL passes the NIST
> CCVS-85 suite, whose SQ201M is decisive on its own — `LINAGE IS 50 LINES WITH FOOTING AT 45` with the canonical
> golden printing **six** footing lines reading LINAGE-COUNTER 45, 46, 47, 48, 49 and **50**. Under the literal
> reading the fifth of those writes would overflow and reset the counter, and the sixth would report 1 against a
> required 50. That golden is `tests/nist/valid/SQ201M.txt` and it is green in this tree. Pinned by
> `2023/pb686_linage_gr26_boundary` and its `85/pb686_linage_gr26_boundary_85` edition twin (the discriminating
> lines are `F3 EOP / LC=004` and `N3 NO-EOP / LC=004`), and by
> `LinageConformanceTests.Gr26ab_CounterEqualsBody_IsFootingEopNotOverflow`.
