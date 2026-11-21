## Entry 3884 — 2026-11-20 22:28 PST — The last 1985 misleading messages: written names, the file verbs' hider, COBOL2326's rule

Three notes of one cluster (kb/Work PB5979, PB5980, PB5978), all diagnostics that told a 1985 programmer something false or unreadable.

**PB5979, qualified references printed run together.** `MOVE X OF T TO Y` over an index-name printed COBOL1637 "references the index-name
'XOFT'"; `KY OF IREC` printed `KYOFIREC`. `ParserRuleContext.GetText()` joins tokens with no separator, and a renderer of the source as
written already existed (`WrittenSource.Of`, which `DataBinder.WrittenText` forwarded to); the defect was every message that did not call it. 129 messages
(an interpolation hole, a `+` with a string literal, or a local feeding either) and the data-reference texts handed to message parameters now
call it; a name that is also a lookup key keeps its key and gains a `<name>Written` twin. The
structure: `MessageOperandTextDriftTests` (RepoDrift) compiles the compiler's sources against the built product and refuses a `GetText()` of a
multi-token context in a message; the one-token contexts it exempts are READ FROM THE GRAMMAR, so no list
is hand-kept. Its first design (syntactic only) saw 394 sites, almost all words; the semantic receiver type and the grammar's one-token
rules cut it to the 129. `DataBinder.WrittenText`, a one-line forwarder of `WrittenSource.Of`, is deleted and its 466 references call the frontend reader
directly (rule 4; the new callers would otherwise have widened DataBinder's reader list). Witnesses: three refusal goldens at 85 (index-name MOVE, indexed START SR4, READ KEY of another file's key).

**PB5980, the file verbs' refusal.** OPEN, CLOSE, READ, START and DELETE of a name that is a container's GLOBAL file but the program's own
data item said "no SELECT or file description entry in this source element gives that name", though one does (§1985:X.1.3.8.2 GR3a). The
report verbs' hider clause became `NameResolution.HidingClause`, one wording shared by both families, with `Declarations.FileNameHiddenBy` the
file twin of `ReportNameHiddenBy`; the I-O-CONTROL file-name resolver says it too. Witnesses: OPEN and READ at 85. A lead: WRITE and REWRITE
of a hidden record-name say "not a logical record of any file description entry", the same fault in `ResolveRecordName`.

**PB5978, COBOL2326 at 85.** `SET K TO 3` over an integer data item cited §1985:VI.6.23.3 SR2, which K satisfies. The rule broken is
§1985:VI.6.23.4 GR3c (GR3b for an index data item receiver), the cells GR5's table marks "No". Each pairing is one `(Later, Y1985)` row through
`EditionInfo.Cite`, so a 2002 or 2014 text is an argument later. Witnesses: GR3c and GR3b refusals. The 2002/2014 halves of the note stay open.

**Gate:** see the landing record; `python scripts/spec/cite.py --check` OK for GR3b, GR3c, GR5, IX.4.5.3 SR2, IX.4.7.3 SR4, VI.5.8.3 SR13.

