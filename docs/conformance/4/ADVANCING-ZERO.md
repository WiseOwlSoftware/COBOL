---
order: 17
---
> ⚖ **DETERMINATION — ADVANCING 0 is an overprint, represented by a bare carriage return** (2026-09-30; owner
> decision kb/Work R54; kb/Work PB1667). §2023:14.9.51.4 GR25 c): "If integer-1 or the value of the data item referenced
> by identifier-2 is zero, no repositioning of the representation of the printed page is performed." The NEXT line is
> therefore presented at the SAME vertical position as the line the device stands on — an overprint, never a
> continuation of that line's text. The standard leaves the device REPRESENTATION to the implementor, so CLAUDE.md
> rule 1's precedence decides (ISO -> GnuCOBOL -> IBM/MF), and GnuCOBOL 3.2 (`libcob/fileio.c`,
> `cob_seq_write_opt` / `cob_file_write_opt`) writes a **bare carriage return (CR, no line feed)** for AFTER/BEFORE
> ADVANCING 0. WiseOwl COBOL does the same, at the advance's own placement: AFTER writes the return and then the
> record; BEFORE writes the record and then the return (`SequentialConnector.WriteTravel`, reached by the one
> non-LINAGE travel and the one logical-page travel, so a file with a LINAGE clause counts the zero — the counter
> stays — and writes the return too). **Two points are the implementor's and are fixed here.** (1) The return is
> written only while a line IS open — the device stands on a line a presentation wrote and no travel has ended
> (`SequentialConnector.DeviceOnOpenLine`). With none open the device is already at the start of a line, there is
> nothing to overprint and nothing is written (GnuCOBOL writes the return there too; it is a no-op on a line
> start, and the report writer's zero advance on an empty page depends on it being nothing). (2) The return is a
> line CONTROL and not a line end, so it is the same byte on every file shape; the line END of the shape it
> overprints is A.1 item 146 (c′)'s three-way mapping (a record sequential print file without LINAGE: LF, so a
> return followed by the next line's LF reads as a CR LF pair; a LINE SEQUENTIAL file, a LINAGE file and a
> report file: the host newline). **The report writer's line state follows from the same representation.** A report file that stays open
> after a TERMINATE (§2023:14.9.46.4 GR6) stands on that run's last line, and INITIATE (§2023:14.9.21.4 GR1 b) resets the
> report's page model, not the device: the first line of the next run belongs on the line BELOW that one, because
> the only overlap a report may print is a relative line of integer-2 zero inside a report group (§2023:13.18.35.4 GR3),
> and a zero advance is an overprint. `CobolReport.PresentLine` therefore treats its empty page as "the device is at
> page line 1" only while `CobolFile.DeviceOnOpenLine` is false; with the device on an open line it stands at line 0
> and the first line travels to its own number. Pinned by `2023/pb1667_advancing_zero_overprint` and
> `2002/pb1667_reinitiate_open_report_file`.
>
> **A line presented onto an open line is the same overprint (kb/Work PB1840).** §2023:14.9.51.4 GR25 e): a BEFORE
> write's "line is presented before the representation of the printed page is advanced", and GR25 f): an AFTER write's
> line is presented after its advance. So when an earlier write left its line open (an AFTER write's record, a plain
> WRITE's - an AFTER ADVANCING 1 LINE - or a BEFORE ADVANCING 0's), no advance lies between the two lines and the new
> one is at the SAME vertical position as the open one, which is GR25 c)'s zero advance by another route. The
> standard states the position and does not leave it to the implementor, so ISO controls and GnuCOBOL does not:
> GnuCOBOL appends the BEFORE record to the open line's text (`AB`), a continuation the standard's "same vertical
> position" does not describe, and WiseOwl COBOL does not follow it. The device representation is the one above: a
> bare carriage return, written by the ONE presentation (`SequentialConnector.Present`) before the record's
> characters, so every arm that presents gets it by construction - a BEFORE write of any amount (a positive count, 0,
> or PAGE: GR25 g) and h) change the advance and not the place the line is presented), on a file with or without a
> LINAGE clause, record sequential or line sequential. Three points are fixed here. (1) The return is written only
> while a line is open AND the device is not already at its start: after a zero advance's own return
> (`SequentialConnector.DeviceLine.Returned`) the next presentation writes none, so AFTER 1 "A", BEFORE 0 "B",
> BEFORE 1 "C" is `A` CR `B` CR `C` and never two returns in a row. (2) A record with no characters to print (a line
> of spaces, the report writer's page-feed carrier) overprints nothing and writes no return; a blank line presented
> with no line open is itself an open line, with the device at its START (`DeviceLine.Returned`), so the record
> presented onto it next, or an AFTER 0, finds nothing to return over and writes none (a stray return there would
> become data of the next record on a line sequential file, kb/Work PB1540). (3) A WRITE that is
> refused - '44' outside the RECORD IS VARYING bounds, '71' for a character outside the line sequential character
> set, '91', a lock refusal - presents nothing, so it neither writes the return nor closes the open line; a BEFORE
> write whose page transition then fails (§2023:13.18.34.4 GR6 b) 2) HAS presented, so its overprint stands. Pinned by
> `14.9.51-WRITE/pb1840_before_after_open_line` (all four editions) and `OpenLineOverprintTests`, which covers the
> refusals (with a declarative, with a FILE STATUS item, with both, with neither: the run ends at the refused
> write), the blank lines and the end-of-page conditions of a write that overprints.
