---
order: 1
---
1. **Message Control System (MCS) asynchronous messaging** (E.3.2 item 1 / A.3 item 4) — **PROVIDED since kb/Work PB4744, and so no longer a documented non-support facility.** The slot keeps its number because
   the facility's documentation is cited as "§4 item N" throughout (the renumbering of items 2 to 5 would touch every citation of them and lands as a tree-wide rename of its own).
   The facility is `SEND` and `RECEIVE` (the ISO/IEC 1989:2023 MCS statements, Annex A.3 item 4, claimed in §2 row 4; the communication facility of COBOL-85 and 2002 left the standard at 2014, and §3.3
   records its implementation at those editions). **D-MCS-SERVER, D-MCS-LENGTH, D-MCS-WAIT, D-MCS-RAISE and D-MCS-CHECK (section 3) are its implementor determinations; D-MCS-NR and D-MCS-TRANSPORT name its
   exception condition and its transport.** What follows is how the statements bind: every operand that is an identifier is resolved (the FROM, GIVING and
   RETURNING operands, the `CONTINUE AFTER` expression, a `RAISING EXCEPTION` exception-name through the one
   exception-name funnel) and the statements of the ON EXCEPTION / NOT ON EXCEPTION phrases are bound as the
   statements they are, so an undefined name there is the ordinary error (kb/Work PB3568). SEND's `TO` operand is read
   by the statement's FORMAT (kb/Work PB4090): Format 1 names a message-server-name, a processor-defined name that is no
   data item (§2023:14.9.38.3 SR2), so it is NOT resolved and an undefined one is no error; Format 2 names a message-tag
   data-name-2 (SR4), a data reference like any other. The grammar's one rule cannot tell the formats apart, so
   `MessagingBinder.ClassifySend` does, from the phrases written (a literal-1 operand and RETURNING are Format 1's,
   RAISING is Format 2's) and the declaration the name silently resolves to (a message-tag item is Format 2's); a
   statement with evidence for both is **COBOL5488**, and one with evidence for neither is Format 1, so a bare
   `SEND TO name FROM x` is **COBOL1850** (RETURNING is required there, §2023:14.9.38.2 prints it without brackets). The
   operands the formats print as NAMES (SEND's TO operand, which is message-server-name-1 or data-name-2, and its
   RETURNING data-name-1; RECEIVE's data-name-1 and data-name-2) are asked their WRITTEN shape first
   (`MessagingBinder.ScreenName`, kb/Work PB4118): a subscript, a reference modifier or a special register is
   refused with **COBOL2024**, the one data-name-n shape screen the SORT keys and the file clauses use, and is read no
   further (no second diagnostic for the format or for an undefined name). A qualifier is a name's own form, so a
   TO operand written `A OF B` is a data-name-2: Format 2's evidence (a message-server-name-1 is a bare name), resolved
   like any data reference. The syntax rules of both statements are screened (kb/Work PB4743, slice S3 of
   `docs/rearchitecture/DESIGN-message-control.md`, `MessagingBinder`): SEND's literal-1 is alphanumeric or national
   (§2023:14.9.38.3 SR1), SEND's data-name-1 and data-name-2 and RECEIVE's data-name-1 are message-tag data items (§2023:14.9.38.3 SR4,
   §2023:14.9.31.3 SR1) and RECEIVE's data-name-2 an integer data item of class numeric (§2023:14.9.31.3 SR3), each refused with
   **COBOL6265**; identifier-1 of either statement and every item subordinate to it carries no ANY LENGTH, BASED, DYNAMIC-LENGTH,
   OBJECT-REFERENCE, POINTER, FUNCTION-POINTER or PROGRAM-POINTER clause and no OCCURS DEPENDING ON whose object lies outside
   identifier-1 (§2023:14.9.38.3 SR3, §2023:14.9.31.3 SR2), each refused with **COBOL6266** (a RECEIVE's CONSTANT RECORD receiver is the receiving funnel's COBOL1548 first, §2023:13.18.15.3 SR2);
   and the word after `CONTINUE AFTER MESSAGE` is RECEIVED (§2023:14.9.31.2). The binder builds one node per statement format
   (`BoundSendToServer`, `BoundSendResponse`, `BoundReceiveMessage`), which `MessagingEmitter` renders as one `AsyncMessaging` call each (kb/Work PB4744). A FIXED group item that holds message tags is a message area: each tag travels beside its image as a slot of the tag and its token
   (`AsyncMessaging.SlotsOf`), and a receiving tag is what the message holds at its own positions, the token of a carried slot resolving to that slot's tag
   (`AsyncMessaging.Resolved`, kb/Work PB4957; `conformance:cases/14.9.38-SEND/send-group-holding-tag`, `MessagingProcessTests`), and so does a variable-length group that holds one (its contiguous decomposition decodes the tag's positions: `conformance:cases/14.9.38-SEND/send-var-group-holding-tag`). Pinned by
   `conformance:wiseowl/cases/14.9.31-RECEIVE/pb3568-receive-undefined-operand`, its siblings and the
   `conformance:wiseowl/cases/14.9.38-SEND/pb4090-*` cases.
   They are a COBOL-2023 introduction, and below `--std 2023` the edition pass answers **COBOL0900**
   (`async-messaging-2023`) and the facility is not named: §2023:8.9 reserves RECEIVE and SEND at COBOL-85 too, for the
   '85 communication module, so the 2023 formats are recognized at every edition and gated on introduction, never
   on reservation (kb/Work PB374; `conformance:cases/_unassigned/pb374-async-messaging-below-2023`).
   The **message-tag type is PROVIDED** (kb/Work PB4740, slice S1 of `docs/rearchitecture/DESIGN-message-control.md`): a
   data item described with `USAGE MESSAGE-TAG` (both spellings, §2023:13.18.60.2 printing `[ USAGE IS ]` as optional) binds the
   category message-tag of class message-tag (§2023:13.18.60.4 GR9; `PicCategory.MessageTag`), starts as the null content
   (§2023:13.18.63.4 GR4), and takes part in the SET Format 17 statement (D-SET17), the message-tag relation condition
   (§2023:8.8.4.2.14), INITIALIZE (§2023:14.9.20.4 GR4, GR6), strongly-typed groups, BY REFERENCE and BY CONTENT argument passing
   (D-MCS-ARG) and the BY VALUE formal of §2023:14.2.2 SR2. Its size and representation are A.1 item 212 (section 7). Only the message
   control system makes a tag that is not NULL (SEND Format 1 returns the server's, RECEIVE the requestor's); a program copies and compares them.
   COBOL1943 (the usage's former refusal), COBOL2205 (SET Format 17's) and COBOL1578 (the facility's former decline) are retired and never
   reallocated. Pinned by `conformance:cases/13.18.60-USAGE/message-tag-usage`,
   `conformance:cases/14.9.39-SET/set-message-tag`, `conformance:cases/8.8.4.2-Simple-relation-conditions/message-tag-relation`
   and `conformance:cases/14.9.20-INITIALIZE/initialize-message-tag`, with one refusal per class rule that names message-tag
   (`message-tag-refused-*` beside each clause).
   The **EC-MCS exception-names** (the level-2 EC-MCS and every level-3 name under it, §2023:14.6.13.1.6 Table 13) are ordinary 2023 names, raised by SEND and RECEIVE
   (kb/Work PB4744; section 3) and written wherever an exception-name may be — the TURN directive, a USE statement, a PERFORM WHEN phrase, RAISE and the RAISING phrases —
   through the one exception-name funnel (`EcNameResolution`). Below `--std 2023` the names draw the introduction gate COBOL0878. Pinned by
   `conformance:cases/14.9.29-RAISE/pb374_ec_mcs_names_raise` and `conformance:cases/_unassigned/pb374-ec-mcs-name-below-2023`.
