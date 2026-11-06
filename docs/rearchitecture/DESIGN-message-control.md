# The asynchronous messaging facility (COBOL 2023)

Status: APPROVED by the owner 2026-11-04 (kb/Work PB4700; the decision to implement the facility is PB4117's; §0's nine
questions are decided as recommended). It becomes IMPLEMENTED when the slices of §9 (kb/Work PB4739-PB4745) land.

WiseOwl COBOL implements the asynchronous messaging facility: SEND (§14.9.38), RECEIVE (§14.9.31), USAGE MESSAGE-TAG
(§13.18.60), the message-tag class and its NULL, comparison, SET, INITIALIZE and BY VALUE, the implicit SEND at run-unit
end (§14.6.11, §14.6.12) and the EC-MCS exception conditions (§14.6.13.1.6 Table 13). Its message control system (MCS)
is a peer transport: run units talk directly over AF_UNIX stream sockets, hold every message in memory, and learn of a
partner's death when its connection closes (§5). The communication facility of COBOL-85 and 2002 (kb/Work PB4677,
`DESIGN-communication-facility.md`) has its own transport, a paged store with a wake-up bell. The two facilities share
the resolution of the MCS directory, the resolution of the socket directory and one framing layer, and nothing else (§6).
This document owns the asynchronous messaging transport; `DESIGN-communication-facility.md` owns the store.

Every quotation of ISO/IEC 1989:2023 in this document was run through `python scripts/spec/cite.py --check`; the log lands beside
this document as `docs/rearchitecture/DESIGN-message-control.cites.txt` (every check OK). The next revision's committee draft
(`COBOL_ISO_IEC_1989_202x-CD_1.3.pdf.txt`, "the CD" below) lies outside the tracked text, so it is cited by line, never
as authority. The general formats of SEND (PDF page 756), RECEIVE (PDF page 732), SET Format 17 (PDF page 762), relation Format 3 (PDF page 218) and USAGE (PDF page 533) were rendered from the PDF
and match the transcription.

## 0. Owner decisions

**Transport (decided 2026-11-04).** The facility uses the peer AF_UNIX transport of §5, gated on slice S0's
`PlatformAssumptionTests` on both CI operating systems (§7.2, §9). If Windows fails them, the liveness-byte design
summarised in §8 becomes the candidate and the transport is re-decided before any runtime slice starts.

The questions below remain open. The standard leaves each open, or prints a defect that decides behavior. Rule 1's
precedence was applied to each: GnuCOBOL 3.3-dev has no implementation (its `send_body_mcs` action is
`CB_PENDING ("COBOL 2023 MCS")`, its RECEIVE grammar has only the communication facility's format, and `libcob/exception.def` registers the EC-MCS names and nothing
else), no GnuCOBOL tracker ticket in the local cache decides any of them, and no IBM Enterprise COBOL or Micro Focus
source for the 2023 facility was available to read, so their behavior is unknown and none is claimed. Each question
carries a recommendation; §3 records each answer as a `docs/CONFORMANCE.md` determination.

1. **SET Format 17's direction.** The printed format is `SET data-name-4 TO { data-name-5 | NULL }`, but §14.9.39.4 GR40
   reads "If data-name-5 is specified, the content of data-name-4 is moved to data-name-5" and GR41 "If NULL is
   specified, the content of data-name-5 shall be set to that of the null value". GR41 names an operand that is not
   written when NULL is. **Recommend:** data-name-4 is the receiver in both rules, as the format, every other SET format
   and §14.9.39.1 ("setting the content of a message-tag data item to NULL or another message-tag") say; record it as a
   defect in the printed standard (kb/Work PB453 owes this decision).
2. **The other printed defects of SEND, and Table 13's attributions.** §14.9.38.4 GR4 says "The message contains the
   content of data-name-1 as well as any additional implementor-defined data", but data-name-1 is the RETURNING message
   tag; GR1 ends "the execution of the RECEIVE statement is unsuccessful" in the SEND statement; GR7 raises "the
   EC-MCS-NO-RECEIVER exception condition", a name Table 13 does not list. Table 13 also describes EC-MCS-INVALID-TAG as
   "RECEIVE or SEND statement" and EC-MCS-NO-REQUESTER as "RECEIVE statement", where no SEND rule raises the first and no
   RECEIVE rule the second. **Recommend:** the message is identifier-1's content; GR1 makes the SEND unsuccessful; GR7
   raises EC-MCS-NO-REQUESTER, the Table 13 name for a missing requestor (the D-MCS-NR determination, extended), and no new
   exception-name is invented. Table 13's other attributions are placed, not set aside: a SEND Format 2 tag that "is not
   a correct format for the MCS" (a tag of another MCS directory) is EC-MCS-INVALID-TAG, the test §14.9.31.4 GR1 b) makes
   of the same operand, and a RECEIVE FROM NULL in a run unit that serves no name, which no requestor can address, is
   EC-MCS-NO-REQUESTER at once (§2.4). The alternative, EC-MCS-NO-REQUESTER for every bad SEND tag as GR7 alone reads,
   leaves both attributions unused. The CD corrects only GR1: its 14.9.41.4 GR2 reads "the execution of the SEND
   statement is unsuccessful" (line 36294), while it still prints EC-MCS-NO-RECEIVER (GR8, line 36343) and still names a
   data-name, data-name-2, as the message's content (GR5, line 36324).
3. **How a message server is named and found.** SR2 says "Message-server-name-1 is the name of a message server run
   unit"; GR4 sends to the run unit "identified by the content of literal-1 or message-server-name-1". Two readings:
   (a) message-server-name-1 is a bare word naming the server, never resolved as a data item, which `ClassifySend` applies
   today (kb/Work PB4090); (b) it is a data-name whose content names the server, the reading GR4's "content of" and the CD
   support (14.9.41.4 GR5: "the run unit identified by the value of literal-1 or the content of identifier-1. Any trailing
   spaces in identifier-1 are ignored", line 36323). **Recommend (a):** the format and SR2 call it a name, never a data-name
   or identifier, and 2023 gives it no class rule, where SR1 constrains literal-1 and SR4 the data-names; the CD needs a new
   one to make (b) work ("Identifier-1 shall be defined as an alphanumeric, national, or UTF-8 data item", SR2, line
   36246). Under (b) an undefined name becomes COBOL1639 and a name resolving to an alphanumeric item is Format 1.
   Either way a server name is the word or the value with trailing spaces removed, compared without regard to case. A
   run unit serves a name when it is started with `WISEOWL_COBOL_MCS_SERVER=<name>`; `mcs.json` may define a server
   (`servers`: name, optional `start` command, optional `maxLength`), and the MCS starts the command when a request finds
   no live server. A name that no live run unit serves and no definition can start is EC-MCS-NO-SERVER, and a SEND
   waiting for a server that ends before taking its message completes the same way: once every server it offered the
   message to has ended without taking it, no new server is found and no start remains, the SEND is EC-MCS-NO-SERVER
   (§5.4.1 step 9). Several run units may serve one name; the first RECEIVE takes the request.
4. **EC-MCS-IMP's fatality.** Table 13 prints "EC-MCS-IMP | Imp | Implementor-defined message control exception".
   DOC-A.1-100 makes every provided level-2 -IMP name fatal; GnuCOBOL's `libcob/exception.def` registers every -IMP name,
   EC-MCS-IMP included, with its fatal flag 0. **Recommend:** fatal, the one rule DOC-A.1-100 states for the other 19
   provided names, so an MCS failure with checking on and no ON EXCEPTION phrase, WHEN phrase or declarative ends the run
   unit abnormally. The alternative, nonfatal as GnuCOBOL has it, makes EC-MCS-IMP the one exception to that rule. Either
   answer rewrites DOC-A.1-100 (§3).
5. **`CONTINUE AFTER MESSAGE RECEIVED`, and a negative interval.** §14.9.31.4 GR2 completes the statement "when the
   message or an exception is returned from the MCS or when the CONTINUE parameters are completed" and gives MESSAGE
   RECEIVED no other meaning. Two readings: (a) the parameters are complete as soon as the MCS reports that no message is
   waiting, so the phrase makes RECEIVE a poll; (b) they are complete when a message is received, which is what RECEIVE
   without CONTINUE already does. **Recommend (a):** D.6.6 offers the phrase as the alternative to a time limit ("one can
   wait forever or give a time limit by adding this", then `CONTINUE AFTER 23 SECONDS *> or MESSAGE RECEIVED`), and (b)
   makes the phrase a second spelling of its own absence. Under (a) a waiting message is received; otherwise identifier-1
   is spaces and the statement is successful. `arithmetic-expression-1 SECONDS` waits at most that long, evaluated once
   by the CONTINUE statement's rule (`CobolTiming`: whole seconds, at most `CobolTiming.MaxSeconds`). A negative value is
   zero, a poll, and EC-CONTINUE-LESS-THAN-ZERO is set to exist when its checking is enabled, as §14.9.9.4 GR1 b) does for
   the CONTINUE statement ("If checking for FC-CONTINUE-LESS-THAN-ZERO is enabled, the EC-CONTINUE-LESS-THAN-ZERO
   exception condition is set to exist"); it is nonfatal and leaves the RECEIVE successful. The alternative sets no
   condition, since GR2 does not invoke the CONTINUE statement's rules.
6. **An exception sent to the requestor.** §14.9.38.4 GR8 sends exception-name-1 "to the requestor program", and
   the implicit SEND of §14.6.11 and §14.6.12 sends EC-MCS-NORMAL-TERMINATION or EC-MCS-ABNORMAL-TERMINATION; no rule says
   what the requestor's RECEIVE does with it. **Recommend:** the requestor's RECEIVE moves the reply into identifier-1
   and its length into data-name-2, then is unsuccessful with that exception condition, set to exist when the requestor's
   checking for it is enabled at the RECEIVE (a level-3 name of any family, tested by name at run time, §4.5, at its own
   Table 13 fatality). §14.9.38.3 has no level-3 rule, where GOBACK has one ("Exception-name-1 shall be a level-3
   exception-name", §14.9.18.3 SR2), so `RAISING EXCEPTION EC-SIZE` is legal; the requestor's RECEIVE is then unsuccessful
   and sets no condition, because "Only the lowest level exception-names are associated with exception status
   indicators" (§14.6.13.1.1).
7. **A message longer than identifier-1.** §14.9.31.4 GR3 makes the RECEIVE unsuccessful with EC-MCS-MESSAGE-LENGTH and
   says nothing of the message. **Recommend:** the message is taken (it does not stay queued, where the next RECEIVE FROM
   NULL would loop on it), identifier-1 receives its first positions, data-name-2 is unchanged, data-name-1 identifies
   the sender, and on the server side the message is active, so the server can answer it, as the NOTE under §14.9.38.4
   GR1 expects ("The message server will determine if the size of the message sent differs from the size of the message
   expected and will return the appropriate exception condition").
8. **Unsuccessful execution when checking is off.** §14.6.13.1.1 NOTE 3: "An exception condition is set to exist only if
   checking for that condition is enabled by default or by the TURN compiler directive", and otherwise "the results of
   the operation are undefined". **Recommend:** whether a SEND or RECEIVE is successful never depends on checking; the
   ON EXCEPTION or NOT ON EXCEPTION phrase is chosen by the outcome, and the condition is set to exist (for
   EXCEPTION-STATUS, a declarative or a WHEN phrase) only when its checking is enabled. The CD recasts every SEND failure
   this way, "unsuccessful, and, if enabled" (14.9.41.4 GR2, GR6 and GR8, lines 36294, 36335 and 36343). EC-MCS-IMP covers
   every failure of the MCS itself: an unusable `mcs.json`, a socket directory the host refuses, a socket error that is
   not a partner's end, a server that cannot be started.
9. **Witnessing two run units in the conformance suite.** A case file runs one run unit. **Recommend:** a WiseOwl-only
   `servers` field, beside `build` (`{BALANCE: "BALSRV"}`: run the named program of the file as a concurrent run unit
   serving the name, in a fresh MCS directory, before the test), which amends the approved
   `DESIGN-conformance-suite.md` §3.1. The portable `cases/` keep the single-run-unit tests (§7).

## 1. The facility and its editions

The facility exists at COBOL 2023 only. Annex E.3.2, the 2023 standard's list of new features against 2014, opens with it
("Asynchronous messaging. A method of allowing communication between run units via messages is provided"), the 2014
FCD (`specs-drafts/t2014.txt`) has no SEND, RECEIVE, MESSAGE-TAG or EC-MCS text at all, and the repository already
records it as a 2023 introduction (`constructs.json` `async-messaging-2023` and `usage-message-tag-2023`,
`VERSION_CHANGE_REFERENCE.md` rows 32, 38 and 40).

| Edition | SEND and RECEIVE | USAGE MESSAGE-TAG, EC-MCS names |
|---|---|---|
| 85, 2002 | the communication facility's statements when their first operand is a cd-name (PB4677); the 2023 formats otherwise parse and answer COBOL0900 | USAGE MESSAGE-TAG: COBOL0900 (`usage-message-tag-2023`); an EC-MCS name, where exception-names exist: COBOL0878 (`ExceptionNameRule.Window`), unchanged |
| 2014 | the 2023 formats parse and answer COBOL0900; the communication facility is removed (COBOL0902) | as 2002 |
| 2014 `--permissive` | COBOL0900 for the 2023 formats (a construct not yet introduced stays an error, `EditionSeverityPolicy.For`); the communication facility runs with a COBOL0902 warning | as 2002 |
| 2023 | the asynchronous messaging facility, implemented; a communication-facility statement is COBOL0902 | implemented |
| 2023 `--permissive` | both facilities run, the communication facility with a COBOL0902 warning; they share only the three types of §6 | implemented |

A.3 item 4 makes the facility processor-dependent: "The asynchronous messaging facility is dependent on the capability of
a processor to allow run units to communicate with each other". The runtime implements the facility on every host
with AF_UNIX stream sockets: Linux, macOS, and Windows from build 17063 ("AF_UNIX comes to Windows"). It is claimed
where CI runs the S0 witness: Linux and Windows. macOS runs the same Unix code (§5.1 gives its paths, and its region
locks are `SidecarRegionLocks`, `HostRegionLocks.cs`), but no CI leg witnesses it, so CONFORMANCE.md's A.3 row 4 names
macOS as not claimed until a macOS leg runs `PlatformAssumptionTests`. Run units of one machine communicate; run units
of different machines do not (§5.1).

## 2. The language

### 2.1 SEND

| Rule | Text (cite.py OK) | Design |
|---|---|---|
| §14.9.38.2 | Format 1 `SEND [TO] {literal-1 \| message-server-name-1} FROM identifier-1 RETURNING data-name-1 [ON EXCEPTION] [NOT ON EXCEPTION] END-SEND`; Format 2 `SEND [TO] data-name-2 FROM identifier-1 [RAISING {EXCEPTION exception-name-1 \| LAST [EXCEPTION]}] ... END-SEND` | RETURNING and END-SEND are required (unbracketed, PDF page 756); TO and the second EXCEPTION are optional words. The binder assigns the format once (§4.3). |
| SR1 | "Literal-1 shall be an alphanumeric or national literal" | screened; a numeric or boolean literal is refused |
| SR2 | "Message-server-name-1 is the name of a message server run unit" | a bare word, never resolved as a data name (§3, Q3) |
| SR3 | identifier-1 and its subordinates contain no ANY LENGTH, BASED, DYNAMIC-LENGTH, OBJECT-REFERENCE, external-ODO, POINTER, FUNCTION-POINTER or PROGRAM-POINTER clause | one screen with RECEIVE SR2 (§4.3) |
| SR4 | "Data-name-1 and data-name-2 shall be the name of a message-tag data item" | screened (it is PB4117's subject) |
| GR1 | "The number of character positions of identifier-1 shall not be zero nor larger than the message server allows" | the runtime measures the image; EC-MCS-MESSAGE-LENGTH (Q2 reads "RECEIVE" as SEND) |
| GR2, GR3 | the ON EXCEPTION / NOT ON EXCEPTION transfer, then an exception-checking PERFORM's WHEN phrase, then a declarative | the shared exception-phrase emission (§4.5) |
| GR4 | "The statement suspends execution until the message is received by the server run unit"; "The server run unit does not necessarily have to respond to the message" | the SEND waits until a server's RECEIVE takes the message (§5.4) |
| GR5 | "If such a run unit is not identified, the EC-MCS-NO-SERVER exception condition is set to exist" | Q3 |
| GR6 | data-name-1 "contains the implementor-defined information (the message tag) that identifies the server run unit and the message sent" | a server tag (§4.2; §5.4.1 step 8) |
| GR7 | data-name-2 identifies a requestor whose message this run unit received and "has not yet been responded to by a SEND statement in the current run unit"; otherwise EC-MCS-NO-RECEIVER | EC-MCS-NO-REQUESTER; a tag of another MCS is EC-MCS-INVALID-TAG (Q2) |
| GR8 | "exception-name-1 or the name of the last exception raised in the current run unit is sent to the requestor program" | the reply carries the name (Q6); LAST with no exception raised sends none |

### 2.2 RECEIVE

| Rule | Text (cite.py OK) | Design |
|---|---|---|
| §14.9.31.2 | `RECEIVE [FROM] data-name-1 GIVING identifier-1 data-name-2 [CONTINUE [AFTER] {arithmetic-expression-1 [SECONDS] \| MESSAGE RECEIVED}] [ON EXCEPTION] [NOT ON EXCEPTION] [END-RECEIVE]` | as parsed today; the word after MESSAGE must be RECEIVED (§4.1) |
| SR1 | "Data-name-1 shall be the name of a message-tag data item" | screened |
| SR2 | identifier-1 as SEND SR3, plus "the CONSTANT RECORD clause" | the same screen, with the CONSTANT RECORD arm (a RECEIVE writes identifier-1) |
| SR3 | "Data-name-2 shall be an integer data item of the class numeric" | screened |
| GR1 | "in the case where the current run unit is being activated as a server run unit, from that requestor run unit" | §5.4 |
| GR1 a) | data-name-1 NULL: "the RECEIVE statement is waiting for any requestor run unit to send a message to the current run unit"; on success it holds the tag of the requestor | a request to any name this run unit serves |
| GR1 b) | otherwise the tag "identifies the run unit that sent a message to the current run unit via a SEND statement"; EC-MCS-INVALID-TAG for a tag that "does not identify a requestor or is not a correct format for the MCS"; EC-MCS-REQUESTOR-FAILED for "a requestor that does not exist" | §5.4 |
| GR2 | with CONTINUE, "In the latter case, the content of identifier-1 will be all space characters and the RECEIVE statement is assumed to be successful" | data-name-2 is set to 0 then, the length of the message received, as D.6.6 ("If bpo-length is zero, no message was received, and you had a time out") expects; Q5 |
| GR3 | "data-name-2 will contain the length, in alphanumeric characters, of the message received"; EC-MCS-MESSAGE-LENGTH when it "exceeds the length of identifier-1" | the image's length, which is `FUNCTION BYTE-LENGTH` of the sender's identifier-1 (A.1 item 213: two positions per national character); Q7 |
| GR4, GR5 | the exception transfers | as SEND |

### 2.3 The message-tag class

| Rule | Text (cite.py OK) | Design |
|---|---|---|
| §8.5.2.9 | "An elementary data item explicitly described as usage message-tag item" | `PicCategory.MessageTag` (§4.2) |
| §13.18.60.4 GR9 | "The class and category of a message-tag data item are message-tag"; the implementor specifies the alignment, representation and "the number of bytes contained in the data item" | the slot rule (§4.2); DOC-A.1-212 |
| §13.18.60.3 SR4, SR14, SR21 | not in a CONSTANT RECORD; "may be specified only for an elementary data item at level 1 or an elementary data item subordinate to a type declaration that includes the STRONG phrase"; "If MESSAGE-TAG is specified, no other usage clauses shall be specified in the data description entry" | SR4 and SR14 are screened today through `ItemCategory.Sr4PhraseOf` and `Sr14PhraseOf`, whose MESSAGE-TAG arms exist; SR21 is COBOL2158 (`MessageTagUsageExclusive`, `DataBinder`) and stays |
| §8.4.3.10.1; §8.4.3.10.3 SR1 b); §8.4.3.10.4 GR4 | "NULL is a predefined address of class pointer or a predefined content of class message-tag"; "it may be used only as a sending operand in an INITIALIZE or a SET statement or in a message-tag relation condition"; "The null content is an implementor-defined value that is guaranteed not to represent a message server or message requestor" | `MessageTag.Null` (§4.2) |
| §8.8.4.2.1 item 9, §8.8.4.2.3 SR5 | "Two operands of class message-tag"; "Identifier-3 and identifier-4 shall reference data items of class message-tag, object, or pointer, and shall be of the same category" | relation Format 3 gains the message-tag arm |
| §8.8.4.2.3 SR4; §8.8.4.2.12 | "Strongly-typed group items that contain elementary items of class boolean, message-tag, object, or pointer, may be compared only for equality or inequality"; "each elementary item of the first operand is compared with the corresponding elementary item of the second operand" | `StatementValidation.ContainsNonOrderableLeaf` sees `CobolClass.MessageTag` (COBOL1535); `StrongGroupComparison` lowers a message-tag leaf pair to the Format 3 relation, so two groups whose tags name different parties compare unequal |
| §8.8.4.2.14 | "The operands are equal if they reference the same message requestor or message server of the same name or one is NULL and the other is NULL" | `MessageTag.SameParty`: two server tags of one server name, two requestor tags of one requestor run unit, two NULLs. A tag's message number does not take part. |
| §14.9.39 Format 17 | SR35 "Data-name-4 and data-name-5 shall be message-tag data items"; GR40, GR41 | Q1 |
| FMT-14.9.20.2; §14.9.20.3 SR1, SR3, SR4; §14.9.20.4 GR4, GR5, GR6 | the MESSAGE-TAG category-name; SR3 "For each DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE, or PROGRAM-POINTER phrase specified as the category-name in the REPLACING phrase, identifier-2 shall be specified"; SR4 "a SET statement with identifier-2 as the sending operand and an item of the specified category as the receiving operand shall be valid"; GR5 c) 1. a. "Either the category of the elementary data item is data-pointer, message-tag, object-reference, or program-pointer"; GR6 "Message-tag \| Predefined content NULL" | `InitializeBinder` binds each form through its existing arms: VALUE and the default to `InitializeSetNull`, `REPLACING MESSAGE-TAG DATA BY tag2` to `InitializeSetFrom` after `SetBinder.ScreenImplicitSet`, SR3 and SR4 through COBOL1982 and COBOL1983. `InitializeEmitter` writes both through `PlaceRenderer`, so the emitter needs no arm; the "no elementary item can reach this test" remark on `QualifiesCategorically` goes. Witnessed in §7.1 |
| §13.18.63.4 GR4 | "data items of class message-tag, class object, and class pointer are initialized to null" | the field's default is `MessageTag.Null` |
| §14.2.2 SR2; §14.9.23.3 SR15; §14.9.4.3 SR22 | "Each data-name-1 specified in a BY VALUE phrase shall be defined as a data item of class numeric, message-tag, object, or pointer"; INVOKE: "identifier-5 shall be of class message-tag, numeric, object or pointer"; CALL: "identifier-4 shall be of class numeric, object, or pointer" | a BY VALUE formal and an INVOKE BY VALUE argument copy the struct; a CALL BY VALUE message-tag argument is refused by `CallBinder`'s SR22 screen once `CobolClass.MessageTag` exists (§4.2); BY REFERENCE passes the slot |
| §14.8.2.3.2 | "If neither the formal parameter nor the argument is of class message-tag, object, or pointer, the conformance rules are the following"; the pointer rule "shall be of class pointer and the corresponding items shall be of the same category" | message-tag is named only to exclude it from the length rules, so the pointer rule is applied to it: a message-tag argument for a formal of another class, or the reverse, is refused where a prototype makes it checkable and is EC-PROGRAM-ARG-MISMATCH at run time otherwise. An implementor choice, D-MCS-ARG (§3) |
| §14.9.25.4 | group moves are byte moves "except as may be required by an implementation for subordinate items of class message-tag, object, or pointer" | a message tag rides the managed slot of its strongly-typed group (`Place.SlotWindow`), as a function-pointer does |

The other class restrictions that name message-tag form one family, which excludes the classes index, message-tag,
object and pointer (a paraphrase; each rule words it its own way): §8.8.4.4.3 SR1, §13.16.3 SR8, SR10 and SR24 e), §13.18.38.3 SR8, §13.18.44.3 SR12 and SR14,
§13.18.45.3 SR8, §13.18.60.3 SR11, §13.18.63.3 SR9, §14.7.6 GR4, §14.9.1.3 SR1 and SR3 ("Identifier-2 shall not reference
a data item of class alphabetic, boolean, index, message-tag, object, or pointer"), §14.9.11.3 SR1, §14.9.13.3 SR4 and SR7,
§14.9.24.3, §14.9.25.3 SR1, §14.9.40.3, §15.19.3 rule 7, §15.59.3, §15.63.3, §15.71.3 and §15.72.3 rule 1, the alignment
rules §8.5.1.6.1 and §8.5.1.6.5, and §13.18.42.4's implicit property methods. They are written today through
`ItemCategory.IsIndexMessageTagObjectOrPointer`, `IsBooleanMessageTagObjectOrPointer` and `IsMessageTag`, or through the
class lattice (`IntrinsicArgumentRules.ClassOfItem`), which answers for the new category (§4.2), so each fires as soon as
the usage binds; slice S1 witnesses each one (§9). The VALIDATE facility's rules that name the class (§13.18.17.3 SR1,
§13.18.24.3 SR3, §13.18.31.3 SR1, §13.18.62.3 SR6, §14.9.50.3 SR2) stay DOCUMENTED-NON-SUPPORT with that facility: their
clauses are refused by name, so no message tag reaches them.

### 2.4 Run-unit end and the exception conditions

§14.6.11 item 7: "If the run unit has an active message or messages to which it has not replied, an implicit SEND
statement of the form 'SEND TO data-name FROM temp RAISING EXCEPTION EC-MCS-NORMAL-TERMINATION' is executed for each such
message", where "temp is a temporary data item that is the size of the receiving data item in the associated RECEIVE
statement for that message-tag". §14.6.12 executes the same SEND with EC-MCS-ABNORMAL-TERMINATION after the abnormal
ending's rollback, then "the runtime system attempts to perform the operations of normal termination as specified in
14.6.11". The temp is spaces (§5.6).

Table 13 lists the level-2 EC-MCS and eight level-3 names, all nonfatal except EC-MCS-IMP, whose category is Imp (Q4
recommends fatal). Every Table 13 description is placed:

| Condition | Table 13 description | Raised by |
|---|---|---|
| EC-MCS-NO-SERVER | SEND statement | SEND Format 1, GR5 (Q3) |
| EC-MCS-NO-REQUESTER | RECEIVE statement | SEND Format 2, GR7 (Q2); a RECEIVE FROM NULL in a run unit that serves no name (Q2) |
| EC-MCS-INVALID-TAG | RECEIVE or SEND statement | RECEIVE GR1 b): a tag of another MCS, or a server tag whose exchange is complete (§5.4.3); SEND Format 2: a tag of another MCS (Q2) |
| EC-MCS-REQUESTOR-FAILED | RECEIVE statement | RECEIVE GR1 b): the requestor the tag names has ended |
| EC-MCS-MESSAGE-LENGTH | RECEIVE or SEND statement | SEND GR1 (zero, or above the server's `maxLength`); RECEIVE GR3 |
| EC-MCS-NORMAL-TERMINATION, EC-MCS-ABNORMAL-TERMINATION | Server terminated before replying to message; Server failed before replying to message | the requestor's RECEIVE of a reply the implicit SEND made (Q6), and the MCS's reply for a server that ended without its epilogue (§5.5) |
| EC-MCS-IMP | Implementor-defined message control exception | any failure of the MCS itself (Q8), at Q4's fatality |
| EC-CONTINUE-LESS-THAN-ZERO | (the CONTINUE statement's) | a RECEIVE whose CONTINUE AFTER value is negative (Q5) |
| any name a server forwards | (its own) | the requestor's RECEIVE of a RAISING reply (Q6) |

### 2.5 Annex entries

- A.1 item 212, "USAGE MESSAGE-TAG (size and contents). This item is required. This item shall be documented in the
  implementor's user documentation": DOC-A.1-212 (§4.2), and a messaging section of the user guide page PB4677 adds,
  `docs/manual/user-guide/communication.md` (the tag, the `servers` definition, `WISEOWL_COBOL_MCS_SERVER`). Item 125
  (NULL) gains the message-tag content; item 100 (EC-level-2-IMP) gains EC-MCS-IMP as a name the implementation sets (Q4).
- A.3 item 4: the facility, claimed (§1).
- A.4: the facility is not an optional element; nothing to record.
- Annex E.2 item 25 reserves END-RECEIVE, END-SEND, MESSAGE-TAG, RECEIVE and SEND at 2023; E.3.2 items 1 and 3 introduce the
  facility and its exception-names (D-MCS-NR covers the -OR spelling of item 3).

## 3. Implementor latitude and docs/CONFORMANCE.md

| Latitude | ISO | GnuCOBOL | IBM, Micro Focus | Choice | CONFORMANCE.md |
|---|---|---|---|---|---|
| Whether the facility is provided | A.3 item 4 | pending | unknown | provided | §2 Annex A.3 row 4 flips from **Not claimed** to **Claimed**; §4 item 1 is deleted and every "§4 item N" reference renumbered in the same change; the preamble's COBOL1578 sentences go |
| Message-tag size, contents, alignment | §13.18.60.4 GR9; A.1 item 212 | none | unknown | the slot rule of DOC-A.1-210, -214 and -217 (D-SLOT), §4.2; a determination, not an owner question | new DOC-A.1-212 row |
| EC-MCS-IMP's fatality | Table 13 (Imp) | fatal flag 0 for every -IMP name | unknown | Q4 | DOC-A.1-100: under Q4's recommendation its "each is FATAL" stands and "the implementation itself sets only EC-PROGRAM-IMP" names EC-MCS-IMP too |
| Argument conformance of a message tag | §14.8.2.3.2 | none | unknown | the pointer rule (§2.3) | new D-MCS-ARG |
| NULL's content | §8.4.3.10.4 GR4 | none | unknown | `MessageTag.Null`, no party | DOC-A.1-125 extended |
| Naming and finding a server; activation | §14.9.38.3 SR2, GR5; §14.9.31.4 GR1 | none | unknown | Q3 | new D-MCS-SERVER |
| Transport between run units | §3.118: "implementor-defined system that sends and receives messages exchanged between run units" | none | unknown | peer AF_UNIX stream sockets between the run units of one user on one machine (§5) | new D-MCS-TRANSPORT |
| Tag contents and equality | §3.120, §8.8.4.2.14 | none | unknown | §4.2; §5.4.1 step 8 | inside D-MCS-SERVER |
| Message length limits | §14.9.38.4 GR1 ("larger than the message server allows") | none | unknown | `maxLength` of a server definition; none otherwise | new D-MCS-LENGTH, with Q7 |
| CONTINUE AFTER | §14.9.31.4 GR2; §14.9.9.4 GR1 ("the implementor shall specify the maximum meaningful value") | none | unknown | Q5; whole seconds, `CobolTiming.MaxSeconds`, a negative value | new D-MCS-WAIT; A.3 row 8 names RECEIVE too |
| Forwarded exceptions | §14.9.38.4 GR8 | none | unknown | Q6, levels 1 and 2 included | new D-MCS-RAISE |
| Outcome with checking off; what EC-MCS-IMP covers | §14.6.13.1.1 NOTE 3; Table 13 | none | unknown | Q8 | new D-MCS-CHECK |
| SET Format 17 direction | §14.9.39.4 GR40, GR41 | none | unknown | Q1 | new D-SET17 |
| SEND's misprints; Table 13's attributions | §14.9.38.4 GR1, GR4, GR7; Table 13 | none | unknown | Q2 | D-MCS-NR extended |

Every other line of `docs/CONFORMANCE.md` that describes the decline changes with the slice that changes its answer. The
query `git grep -n -i -E "message-tag|COBOL1578|COBOL1943|COBOL2205|EC-MCS|asynchronous messag" -- docs/CONFORMANCE.md`
finds 48 lines. They sit in the preamble's COBOL1578 sentences, Annex A.3 row 4, D-MCS-NR ("accepted inert like every
EC-MCS name"), §3's statement that the MCS statements carry the §4.2.6 warning, §4 item 1, the CORRESPONDING note that
"the message-tag class has no item to refuse", §4's COBOL1578 roll-call, the A.1 preamble that withdraws item 212,
DOC-A.1-46 (MESSAGE-TAG under "Not provided") and DOC-A.1-100. The file changes under every landing, so the query, not
a line number, finds them: the slice that retires the decline re-runs it and leaves no hit that describes the decline.

## 4. Compiler

### 4.1 Grammar

The union grammar stays: `mcsReceiveStatement` and `mcsSendStatement` parse at every edition, keyword-led, and
`VersionConformancePass.VisitMcsReceiveStatement` / `VisitMcsSendStatement` answer COBOL0900 below 2023. PB4677's
predicate `communicationNameFollows()` keeps a `SEND cd-name ...` out of the 2023 arm, and its RECEIVE parts by the word
after the operand. Two changes:

- The rules are renamed `messagingReceiveStatement` and `messagingSendStatement` (PB4743; they were `mcsReceiveStatement`
  and `mcsSendStatement`): both facilities have a message control system, so "MCS" does not tell the two statements
  apart. Every visitor, binder arm, audit line and drift test followed in the same change.
- `CONTINUE AFTER MESSAGE RECEIVED` was parsed as `messageKeyword cobolWord`, so `CONTINUE AFTER MESSAGE FOO`
  compiled. The arm is `messageKeyword {wordAhead("RECEIVED")}? cobolWord` (PB4743; a `FOO` is COBOL0001, "failed
  predicate"); RECEIVED stays a cobolWord because §8.9 does not reserve it and §8.10 does not list it as
  context-sensitive.

SEND keeps one rule for both formats: which format a `SEND TO name FROM x` is depends on the declaration of `name`, which
no parse alternative reads (kb/Work PB4090).

### 4.2 The type: a message tag is a native field

- **`PicCategory.MessageTag`** (`Binding/Model/PicInfo.cs`): a PICTURE-less elementary category beside `ProgramPointer` and
  `FunctionPointer`, occupying no character positions of its own. `PictureAnalyzer.ParseUsage` keeps returning
  `Usage.MessageTag` and loses its COBOL1943 refusal; `DataBinder.BindEntry`'s usage chain, which binds
  `PicInfo.MessageTagRecovery()` today, binds a new `PicInfo.MessageTagItem()` factory beside
  `PicInfo.FunctionPointerItem`. `PicInfo.MessageTagRecovery` and its recovery comments are deleted (in `DataBinder`,
  `ItemCategory`, `IntrinsicArgumentRules` and `VersionConformancePass`), and so is the `Usage.MessageTag` arm of
  `ItemCategory.IsMessageTag` that read the written usage for want of a category (the category answers).
- **`CobolClass.MessageTag`** (`Binding/IntrinsicArgumentRules.cs`): §8.5.2.1 Table 2's class, which `ClassOfItem`
  answers for the category, so every screen over the class lattice decides a message tag instead of failing open:
  `StatementValidation.ContainsNonOrderableLeaf` (§8.8.4.2.3 SR4), `CallBinder`'s §14.9.4.3 SR22 screen, and the
  intrinsic argument screens. `IntrinsicArgumentClassDriftTests.EveryUsageMember_HasATable2ClassDisposition` records the
  new disposition.
- **The representation (DOC-A.1-212)** follows the slot rule that DOC-A.1-210, -214 and -217 already state for every
  slot-carried class (D-SLOT; DOC-A.1-210: "shared with POINTER / PROGRAM-POINTER / OBJECT REFERENCE / INDEX so the four
  cannot drift"): a managed native field, no alignment, eight
  character positions of spaces inside a strongly-typed group, `FUNCTION BYTE-LENGTH` 8. A tag's contents are the MCS's
  identity, the party's role (server or requestor), the server's name or the requestor run unit's identity, and the
  message's serial number. NULL is the value with no party.
- **`StorageForm.MessageTagRef`** (`Binding/Model/StorageForm.cs`): zero image width, not a character image;
  `StorageFormPass.BaseElementary` maps the category to it and `StorageElementType` renders `MessageTag`.
- **`Place.SlotWindow.CarriedBySlot`** gains `PicCategory.MessageTag`, so a message tag in a strongly-typed group, a BASED
  area or an EXTERNAL item rides the managed slot exactly as a function-pointer does, and `MembersOf` carries it through
  SORT, MOVE and a message (§5.2). A FILE SORT over an SD record holding one (kb/Work PB4772) carries it as the record's leaf vector
  (`PlaceRenderer.GroupLeaves`, `CobolSort.ReleaseStatement`'s `slots`, `CobolSort.LastReturnedSlots`) beside the one-way
  transfer image the keys read, decided by `SortEmitter.HoldsSlots` (a slot leaf, never the image capability), so a later
  slice that gives a tag group a two-way image must give it the leaf-vector methods too: the sort still takes this path
  and fails to compile, rather than lose a tag, if it does not.
- **The runtime type `MessageTag`** (`src/WiseOwl.Cobol.Runtime/Messaging/MessageTag.cs`): a `readonly struct`, NOT a
  record, so C# has no `==` an emitter could mistake for §8.8.4.2.14's equality.

```csharp
public readonly struct MessageTag
{
    public static readonly MessageTag Null = default;
    internal MessageTag(string mcs, MessageParty party, string name, long message) { ... }
    public bool IsNull => Party == MessageParty.None;
    /// <summary>ISO §8.8.4.2.14: the same server name, the same requestor run unit, or both NULL.</summary>
    public static bool SameParty(MessageTag a, MessageTag b) { ... }
}
public enum MessageParty { None, Server, Requestor }
```

  `Mcs` is the MCS identity `<h>` (§6: 16 hex digits of the hash of the canonical MCS directory; the 2023 facility has
  no store, and the communication facility's store identity in `Meta` is another value), `Name` the server name (a server tag) or the requestor run unit's identity (a
  requestor tag), `Message` the message's serial number. Only the MCS makes a non-null tag; a program copies tags
  (SET, MOVE of a strongly-typed group, a parameter, a message) and compares them.

### 4.3 Binding

**`MessagingBinder`** (`Binding/Procedure/Verbs/MessagingBinder.cs`) replaces `DeclinedFacilityBinder.BindSend`,
`BindReceive`, `ClassifySend`, `ScreenSendRequiredPhrases` and `ScreenName`'s SEND and RECEIVE uses;
`DeclinedFacilityBinder` keeps VALIDATE alone and its class summary says so. `StatementBinder`'s two arms bind through
`MessagingBinder` below an availability check that keeps today's below-2023 behavior (`BoundNop`, the COBOL0900 gate fails
the compile), not through `BindDeclinedStatement`. S3 (PB4743) lands the binder and still routes the two arms through
`BindDeclinedStatement` (its operand action calls `MessagingBinder` and drops the node), so COBOL1578 and the decline
stand; `BindDeclinedStatement` now takes the deferred-shape operands back off the unbuilt ledger for every declined
statement, a duty `DeclinedFacilityBinder.BindValidate` and the two removed members each carried. S4 replaces the arms
with the real binding and `StatementEmitter`'s three throwing arms with `MessagingEmitter`.

The binder builds three nodes, each carrying its exception phrases through `PhraseBlocks.Bind(...,
ConditionalPhraseFamily.Exception, ...)`:

| Node | Members |
|---|---|
| `BoundSendToServer` | the server (a `string` name or a literal), the FROM operand (a place, or a function or method value), the RETURNING place, the phrases |
| `BoundSendResponse` | the TO place, the FROM operand (a place, or a function or method value), the RAISING exception-name or LAST, the phrases |
| `BoundReceiveMessage` | the FROM place, the GIVING place, the length place, the wait (none, seconds expression, poll), the phrases |

`ClassifySend` moves unchanged: a literal-1 and RETURNING are Format 1's evidence, RAISING and a message-tag or qualified TO
name are Format 2's; both is COBOL5488; a bare name that is no message-tag item is message-server-name-1, and Format 1
then requires RETURNING (COBOL1850). Format 2's data-name-2 resolves; Format 1's message-server-name-1 never does (it is
"the name of a message server run unit", no data item). The screens, in order:

1. The name shape of every data-name operand (`DataBinder.ScreenDataNameShape`, COBOL2024), as today.
2. SEND SR1: literal-1 is alphanumeric or national.
3. SEND SR4, RECEIVE SR1: RETURNING data-name-1, TO data-name-2 and RECEIVE data-name-1 are message-tag items.
4. RECEIVE SR3: data-name-2 is an integer numeric item.
5. SEND SR3, RECEIVE SR2: one walk over identifier-1 and its subordinates (`DataItem.SelfAndDescendantsOf`) asks for each
   forbidden clause (`MessagingBinder.ForbiddenClause`). A depending-on item outside identifier-1 is found from the
   table's `OccursSpec` (`Binding/Model/OdoModel.cs`). RECEIVE's CONSTANT RECORD arm is the receiving funnel's, not the
   walk's (PB4743's correction of this paragraph): a RECEIVE writes identifier-1, so `ResolveReceiving` has already
   refused every item of a CONSTANT RECORD (COBOL1548, §13.18.15.3 SR2) before the walk sees a place, and an arm in the
   walk would be unreachable. SEND reads identifier-1, so SEND accepts a constant record, as its list of clauses says.
6. The written exception-name of RAISING resolves through `EcNameResolution.TryResolve`, the one funnel, with no level-3
   site: §14.9.38.3 has no level-3 rule, so a level-1 or level-2 name binds (Q6), and a 2023 name below 2023 is still
   COBOL0878.

Each refusal states its rule with the `cite.py`-checked text, the COBOL1720 precedent. Screens 2 to 5 are two codes in
`DiagnosticCatalog`, allocated by PB4743: COBOL6265 `MessagingOperandClass` (screens 2 to 4: literal-1, the message-tag
items, RECEIVE's integer item) and COBOL6266 `MessagingMessageArea` (screen 5). PB4677's COBOL6171 names the
communication facility's operands and its rule texts, so its message does not fit and it is not reused. The one
operand-class helper (`MessagingBinder.RefuseOperandClass`) quotes the rule; the third allocated code is unused.

**SET Format 17** stops being refused: `SetBinder.BindMessageTagSet` builds a `BoundSetMessageTag` (receiver, sender or
NULL) by Q1; `SetFormatSelection`'s F17 row stays as it is. **Relation Format 3** gains the message-tag arm in
`RelationComparability` and the condition binder: equality and inequality only, same category, NULL admitted (§8.4.3.10.3
SR1 b); `StrongGroupComparison` reaches the arm for a message-tag leaf with no change of its own (§2.3). **INITIALIZE**
binds through its existing arms (§2.3), and its "no elementary item can reach this test" remark goes.

### 4.4 Code generation

`MessagingEmitter` (`CodeGen/Verbs/MessagingEmitter.cs`) renders one runtime call per statement and stores the
outcome back through the ordinary channels:

- The message is identifier-1 as a file record is sent: its image, `OperandText.RecordAreaImage(place)`, and beside it
  its extent table, `OperandText.RecordAreaExtents(place)`, decided by the same predicate. A national item sends two
  positions per character. A variable-length group (one with an OCCURS DYNAMIC table, which SEND SR3 and RECEIVE SR2
  leave legal) sends its contiguous image at its current extent, and that image "carries no marker of where a dynamic
  member ends" (`OperandText.cs#RecordAreaExtents`), so its `RecordExtents` table travels with it through the API, the
  `GRANT` and `REPLY` frames and the receiver's store, as it travels through a file frame. Every other identifier-1
  sends no table. The message-tag slots of a strongly-typed identifier-1 travel beside the image
  (`SlotWindow.MembersOf`).
- SR3 and RECEIVE SR2 refuse POINTER, FUNCTION-POINTER, PROGRAM-POINTER and OBJECT-REFERENCE in identifier-1 and its
  subordinates at compile time, so the only slot-carried leaf a message holds is a message tag. A strongly-typed group
  holding one has an image only if `DataItem.ElementImageCapable` admits the category: it admits `PicCategory.MessageTag`
  with an image of eight spaces (DOC-A.1-212), as R40 admitted INDEX: the generated `FromImage` skips those positions
  and the slot carries the value. A pointer-class leaf stays excluded, and `DataItem.TransferImageCapable`'s slot arm is
  unchanged. Slice S1 (kb/Work PB4740) landed the tag as a slot-carried category (`SlotWindow.CarriedBySlot`) and left
  `ElementImageCapable` as it was, so a strongly-typed group holding a tag has today the one-way transfer image an object
  reference's group has (DISPLAY, MOVE sender, the `BYTE-LENGTH` of 8 positions per tag); the two-way image that SEND and
  RECEIVE need is this bullet's admission, which belongs to slice S4 with the emitter that consumes it. An identifier-1
  that is a single elementary message tag has the image `""` (`RecordAreaImage`'s slot-carried arm), zero character
  positions, so its SEND is EC-MCS-MESSAGE-LENGTH under GR1 and a RECEIVE into it takes any message as too long (GR3).
- RECEIVE stores the message by the receiving inverse a READ uses, arm for arm. An identifier-1 that is a
  variable-length group decomposes the image by the received extent table through
  `PlaceRenderer.WriteVarGroupContiguous` (the group arm of `SequentialIoEmitter.EmitOutOfLineInto`), which honours the
  table only when the components correspond and splits by the D-FRA take step otherwise (`RecordExtents`). Any other
  identifier-1 takes the image through `SequentialIoEmitter.EmitImageInto`, the one record-area store, left-justified
  and space-filled as a group move is (§14.9.25.4). The slots follow. data-name-2 takes the length through the
  ordinary numeric store; data-name-1 takes the tag. The capacity a RECEIVE reports to the MCS is identifier-1's
  maximum length.
- RETURNING data-name-1 takes the server tag; Format 2 hands the tag over by value.
- The phrase pair follows the outcome's `Successful` (`SequentialIoEmitter.EmitPhrasePair`'s shape).
- SET Format 17 is a field assignment; the relation is `MessageTag.SameParty(a, b)` (negated for NOT and `<>`).
- A message-tag field is seeded `MessageTag.Null`, through `PicInfo.DefaultInitializer`, the seed every pointer uses.

### 4.5 The exception conditions

A statement's raisable names are one structure, `EcBinder.DeclaredRaisableNames`, the enumeration `EcWrap` filters by
`TurnState.Enabled`; a name a statement can set and does not declare is "lost in silence". The three nodes therefore get
`EcBinder.NodeKindRaises` rows:

| Node | Declared names |
|---|---|
| `BoundSendToServer` | EC-MCS-NO-SERVER, EC-MCS-MESSAGE-LENGTH, EC-MCS-IMP |
| `BoundSendResponse` | EC-MCS-NO-REQUESTER, EC-MCS-INVALID-TAG, EC-MCS-MESSAGE-LENGTH (GR1's zero length), EC-MCS-IMP |
| `BoundReceiveMessage` | EC-MCS-INVALID-TAG, EC-MCS-REQUESTOR-FAILED, EC-MCS-MESSAGE-LENGTH, EC-MCS-NO-REQUESTER, EC-MCS-NORMAL-TERMINATION, EC-MCS-ABNORMAL-TERMINATION, EC-MCS-IMP; EC-CONTINUE-LESS-THAN-ZERO when it waits on seconds (Q5) |

Each of these fixed names follows `CheckingFlags`' five declarations (a field, the engine property, its forwarder, the
`EcEmitter` gate row, a one-line raise helper), and `ExceptionRaiseHelperDriftTests` holds each helper to its one flag.

A forwarded name (Q6) is not fixed: the server chooses it, and the requestor learns it at run time, so no flag and no
declaration can name it. `BoundReceiveMessage` therefore carries the profile an activating statement carries
(`IActivatingStatement.ActivatorChecking`, an `EcCheckingProfile` "queryable at run time for a name chosen at run time",
stamped by `BindStatement`'s one exit); a RECEIVE is no activation, so the slice renames that interface for what it
carries and changes every implementer. `AsyncMessaging.Receive` takes the profile and asks
`profile.Enabled(name)` before `ExceptionState.Set(name, fatal)`, the test the activating statement of a GOBACK or EXIT
RAISING already makes for a propagated name (kb/Work PB408). A level-1 or level-2 name sets nothing (Q6).

Retired with the decline: the EC-MCS row of `EcNameResolution.DeclinedEcFamilies` (the names become ordinary 2023 names,
still COBOL0878 below 2023), the EC-MCS family entry of `tests/doc-facts/declined-raise-sites.json`, EC-MCS-IMP from
that file's DOC-A.1-100 entry (Q4), and the "accepted inert" wording of D-MCS-NR. `EcRaiseSiteRegistryDriftTests` then
requires a raise site for each, so these retire in the slice that adds the raise sites (S4).

### 4.6 What retires

| Retires | Where |
|---|---|
| COBOL1578 `McsFacilityUnsupported` | `DiagnosticCatalog`, `StatementBinder` arms, `EcNameResolution` |
| COBOL1943 `MessageTagUsageUnsupported` | `PictureAnalyzer.ParseUsage` |
| COBOL2205 `McsMessageTagSetUnsupported` | `SetBinder.BindMessageTagSet` |
| `PicInfo.MessageTagRecovery`, the written-usage arm of `ItemCategory.IsMessageTag` | Binding/Model |
| `DeclinedFacilityBinder`'s SEND and RECEIVE members | Binding/Procedure/Verbs |
| `derived-verdicts.mcs-only`, `DerivedVerdictDriftTests.NoAsynchronousMessagingConditionedRow_Diverges` and `TheMcsSelector_IsStillSharp` | `tests/version-matrix/inventory-schema.json`; each slice narrows the selector to the rows still declined, and S4 deletes it |
| the EC-MCS row of `EcNameResolution.DeclinedEcFamilies`; the EC-MCS entries of `declined-raise-sites.json` | §4.5 |
| `DocumentedNonSupportWitnessTests.McsFacility_*`, `McsStatements_Below2023_*`, `McsExceptionNames_*` | replaced by the positive and negative witnesses of §7 (the below-2023 gate stays witnessed by `pb374-async-messaging-below-2023`) |
| `constructs.json`: `usage-message-tag-2023` stops being PENDING; `async-messaging-2023`'s description loses "accepted inert" | |

The traceability inventory rows the decline decides, 31 of them, are re-adjudicated by the slices that implement their
rules: DOC-A.1-212, FMT-14.9.31.2, FMT-14.9.38.2, GR-8.4.3.10.4-4, GR-8.8.4.2.1-9, GR-13.18.60.4-9, GR-14.6.11-7,
GR-14.9.31.4-1 to -5, GR-14.9.38.4-1 to -8, GR-14.9.39.4-40, GR-14.9.39.4-41, SR-13.18.60.3-21, SR-14.9.31.3-1 to -3,
SR-14.9.38.3-1 to -4 and SR-14.9.39.3-35. A further 48 rows mention message-tag in a class list or a note and carry
CONFORMS on their other classes (among them SR-8.8.4.2.3-5, GR-13.18.63.4-4, SR-13.18.60.3-4, -11 and -14,
SR-14.9.11.3-1 and SR-14.2.2-2); each gains a message-tag witness in slice S1, as do the CONFORMS rows §2.3 adds to the
design's reach (SR-8.8.4.2.3-4, SR-14.9.1.3-3, SR-14.9.4.3-22, SR-14.9.20.3-3 and -4, GR-14.9.20.4-5, FMT-14.9.20.2).
§8.8.4.2.14 and §14.6.12 are prose with no inventory row; slice S1 files a kb/Work note for the first and S4 for the
second rather than inventing a row here.

## 5. Runtime: the peer transport

The runtime lives in `src/WiseOwl.Cobol.Runtime/Messaging/`. It holds every message in the memory of the run units that
exchange it and keeps nothing on disk but socket files. The standard asks no more: a SEND "suspends execution until the
message is received by the server run unit" (§14.9.38.4 GR4), and a reply goes only to a requestor whose message "has not
yet been responded to by a SEND statement in the current run unit" (GR7), so no message outlives both parties.

### 5.1 The MCS directory and the socket directory

The MCS directory, its identity `<h>`, the socket directory `<S>`, its permissions and the run id `<r>` are the shared
resolutions of §6, which `McsDirectory` and `McsSocketDirectory` implement for both facilities. The 2023 facility
passes `MessagingSettings.Directory`, whose default is `WISEOWL_COBOL_MCS`, so two run units of one process can name
different MCS directories (§7.2). Every tag carries `<h>`; a tag with another `<h>` is EC-MCS-INVALID-TAG (Q2).

The facility's entries under `<S>` are:

| Entry | What it is |
|---|---|
| `t/<r>` | a listener while it is being set up: bound and listening here, then renamed into place (§5.4.1) |
| `s/<N>/<r>` | a server's listener for the name `<N>` |
| `p/<N>/<r>` | the pending bell of a requestor whose SEND waits for a server of `<N>` (§5.4.1) |
| `n/<r>` | the notify socket of a requestor that activates a server (§5.4.5) |
| `start/<N>.lock` | the activation lock (§5.4.5), a regular file; where the region-lock host is `SidecarRegionLocks` (macOS, any other Unix, and Linux when `OfdRegionLocks.Available` is false) it adds its lock directory `start/.wolk-<16 hex digits>/` beside it (`SidecarLockFiles.LockDirectoryOf`) |

`<N>` spells a server name (Q3: trailing spaces removed, upper-cased). A name that matches `[A-Z0-9-]{1,14}` is spelled
as itself; any other is `_` followed by 13 characters, the RFC 4648 base32 of the first 8 bytes of the SHA-256 of the
name's UTF-8 bytes, no padding. `_` lies outside the kept alphabet, so a hashed spelling never equals a kept one. Two
names whose hashes collide share a directory, so a server's `HELLO` carries the name it serves, and a requestor closes a
link whose name differs (§5.3).

**The path budget.** Sockets stay out of the MCS directory because an AF_UNIX path is short: §6 gives each OS's limit,
which S0 pins as `MaxPathBytes`. The longest entry adds 33 bytes to `<S>`: `p/` (2), `<N>/` (at most 15) and `<r>` (16). With §6's `<S>`:

| Host | `<S>` | Worst case | Reported limit |
|---|---|---|---|
| Linux, `$XDG_RUNTIME_DIR` = `/run/user/<uid>` | `/run/user/<uid>/wiseowl-mcs-<uid>/<h>/` | 94 bytes with a 10-digit uid (82 with uid 1000) | 107 |
| Linux or macOS, `/tmp` | `/tmp/wiseowl-mcs-<uid>/<h>/` | 78 bytes with a 10-digit uid | 107 (Linux), 103 (macOS) |
| Windows | `Path.GetTempPath()` + `wiseowl-mcs\<h>\` | 91 bytes plus the user-name segment of `C:\Users\<user>\AppData\Local\Temp\` | 107 (S0) |

macOS never uses its `$TMPDIR`, a per-user directory of about 49 bytes under `/var/folders`, which would make the worst
case about 115 bytes for uid 501 against 103. `McsSocketDirectory.Resolve` falls back from an
`$XDG_RUNTIME_DIR` under which `Fits(33)` fails to `/tmp` (§6). The peer then asks `McsSocketDirectory.Fits(33)` once,
and a failure is EC-MCS-IMP at the run unit's first messaging statement, with a message naming `<S>` and the limit, so
whether a run unit can message never depends on the length of a server name.

### 5.2 State, in memory only

A requestor holds `Pending` (its one SEND in flight) and `Replies` (per serial: a reply, or "server ended"); a server holds
`Offers` (per link, oldest first) and `Active` (per message taken: link, receiving size). A crash leaves nothing to
recover but socket files, which the refusal rule of §5.4.1 removes.

### 5.3 Frames

`McsFrame` is §6's frame. A link opens with `HELLO{version, <h>, <r>, pid, served name}` both ways (a requestor's
served name is empty), or closes on a mismatch of version or `<h>` at either end, or, at a requestor, of the served name
against the name it asked for (the only place a hash-collided directory shows; the requestor does not connect to that
server again during the SEND). A requestor writes no `OFFER` on a link before the server's `HELLO` has passed that check.
A frame a role never receives, or one before `HELLO`, closes the link; a `CLAIM` that crosses a `WITHDRAW` is neither
(the server has not yet read it) and is ignored. `PeerFrameCodec` (Runtime/Messaging) encodes each payload: text as a
UTF-16 code-unit count and the units, integers little-endian. A message travels as three parts: its image as UTF-16 code units, so no encoding is chosen; its extent table
(§4.4), absent or the `FixedAt` and `Lengths` arrays of a `RecordExtents`, which is what a file frame records of it; and
its slots, each a `MessageTag` as its four fields. SEND SR3 and RECEIVE SR2 refuse every pointer and object-reference
leaf at compile time, so no other slot reaches a frame. Kinds: `Ring` (no payload; the only frame a bell receives),
`HELLO`, `OFFER`, `CLAIM`, `GRANT` (with the message), `WITHDRAW`, `ACK`, `REPLY` (with the message and an exception-name), each
of the last six carrying the serial and, before `ACK`, the offer round. The requestor keeps one link per server.

### 5.4 The protocol

`MessagingProtocol` takes the current statement and the events `FrameArrived`, `LinkEnded`, `LinkOpened`, `BellRang` and
`DeadlinePassed`, and returns actions (`Write`, `Connect`, `Enumerate`, `ArmDeadline`, `Activate`, `Complete`) that
`MessagingPeer` executes on a transport. It calls nothing.

Slice S2a builds the protocol and its medium without a socket. The full action set is `Write`, `CloseLink`, `Publish`,
`Unpublish`, `Enumerate`, `Connect`, `RingBells`, `ArmDeadline`, `Activate` and `Complete`; the full event set adds the
answers to those actions (`Enumerated`, `Connected`, `ActivationEnded`) and `ActionFailed`, which is EC-MCS-IMP for the
statement the failed action served (Q8). `IPeerTransport` is the medium an action runs on: `Publish`, `Unpublish`,
`EnumerateServers`, `Connect` (which applies the refusal rule), `Write`, `Close`, `RingBells` and `TryTake`, which hands
the peer the next `FrameReceived`, `LinkLost`, `LinkAccepted` or `BellRung` in arrival order. `InMemoryTransport` implements it over a shared
`InMemoryNetwork` that stands for the socket directory: `Abort` ends every link as a kill does and leaves the published
files, `Dispose` ends as `Terminate` does, and `BetweenListenAndRename` is the §5.4.1 window a test can probe. The peer
answers every action in the order it issued them, and an action that fails answers with `ActionFailed` in place of its own
answer; a statement that ends while answers are owed (EC-MCS-IMP) leaves them stale, and the protocol consumes them first.
A requestor keeps a link per server across SENDs (`Sent`, `Replies`); a link the protocol closes after a bad `HELLO` is
burned for the current SEND only.

#### 5.4.1 SEND Format 1

**Publishing a listener.** .NET's `Socket.Bind` and `Socket.Listen` are separate calls, and a connect that lands
between them finds a socket that is not listening. On Linux that connect is refused; S0 witnesses the window on each
OS. A requestor that removed a refused file in that window would delete a live server's listener and leave the server
unreachable with no error. So no listener the facility publishes is ever visible before it listens: the run unit binds
it at `t/<r>`, calls `Listen`, and only then renames the file to its final name with `File.Move`. That a rename moves a
bound socket's file and a connect at the new name reaches the listener is unverified from documentation, and S0
witnesses it on each OS. A file under `s/` or `p/` therefore always belonged to a listening socket, and a connect to it
that is refused means its owner has ended. **The refusal rule:** whoever is refused at a final name, a requestor at a
server's `s/<N>/<r>` or a server ringing a requestor's `p/<N>/<r>`, deletes the file; a file that is already gone is
skipped. If the rename witness fails on an OS, that OS publishes in place instead, holding a new region-lock byte,
`HostRegionLocks.McsPublishByte` on `start/<N>.lock`, exclusively from `Bind` until `Listen` returns; a refused
requestor or ringer then takes the byte, connects once more, and deletes the file only if refused again.

1. A message of zero character positions, or longer than the `maxLength` that `mcs.json` defines for the name, is
   EC-MCS-MESSAGE-LENGTH (GR1, Q2). Nothing has been published yet.
2. Publish the pending bell `p/<N>/<r>`.
3. Enumerate `s/<N>/` and connect to each entry with no link, applying the refusal rule. Any other failure is EC-MCS-IMP.
4. No link: activate (§5.4.5) when `mcs.json` defines `start` and this SEND has not yet activated the name; otherwise
   "If such a run unit is not identified, the EC-MCS-NO-SERVER exception condition is set to exist" (§14.9.38.4 GR5).
5. Write `OFFER` of the current round on every link, and wait.
6. On the round's first `CLAIM`, write `GRANT` to that server and `WITHDRAW` to every other offered link. Other CLAIMs
   are ignored; their links already have a WITHDRAW.
7. On `BellRang` or `LinkOpened`, repeat step 3 for new links; while the round has no `GRANT`, offer it on each new link.
8. On `ACK`, the SEND succeeds with the server tag (Server, the name, the serial), bound to that link.
9. On `LinkEnded`: an offered link that was not granted is dropped. The granted link ending before `ACK` means no program
   saw the message, since a server writes `ACK` before its RECEIVE returns, so a new round starts on the links that
   remain. When no link remains, whether its servers were granted or not, the SEND repeats step 3 once; a new link gets
   a new round, and with none step 4 decides: activation if this SEND has not activated, otherwise EC-MCS-NO-SERVER.

Invariant, which bounds a server's wait after its CLAIM: every link offered a round receives one `GRANT` or `WITHDRAW`
for it before the SEND returns, or ends.

**The pending bell.** A server publishes its listener, then rings each bell under `p/<N>/` (connect, write one `McsFrame`
of kind `Ring`, close), applying the refusal rule, and a requestor that hears the ring repeats step 3 for new links. The
requestor publishes before it enumerates and the server before it scans, so one always sees the other. A ring decides
nothing; a lost one costs one enumeration.

The bell keeps no SEND alive. A SEND waits only while it holds a link (steps 5 to 9) or waits for the server it
activated (§5.4.5); when it holds neither, step 9 completes it. Every completion of the SEND, successful or not, and
the run unit's end during one (§5.6), closes the bell and deletes `p/<N>/<r>`. A requestor killed inside its SEND leaves
its bell behind, and the next server that rings it is refused and deletes it.

#### 5.4.2 RECEIVE and SEND Format 2 (server)

RECEIVE FROM NULL in a run unit that serves no name is EC-MCS-NO-REQUESTER (Q2). Otherwise the server writes `CLAIM` for
its oldest offer and moves on at a `WITHDRAW`. At a `GRANT` it writes `ACK`, records the message in `Active` with
identifier-1's maximum length, and only then completes with the requestor tag (Requestor, `<r>`, serial); a message longer than
identifier-1 is taken and answered EC-MCS-MESSAGE-LENGTH (Q7). RECEIVE FROM a requestor tag considers only that link, and
a missing or ended link gives "the EC-MCS-REQUESTOR-FAILED exception condition is set to exist" (§14.9.31.4 GR1 b).

SEND Format 2 tests in GR order. A message of zero character positions is EC-MCS-MESSAGE-LENGTH (GR1); `maxLength` does
not apply, because it is the limit a server definition sets on what that server accepts (§2.1 GR1, D-MCS-LENGTH), and a
reply goes to a requestor, which has no definition and measures the reply against its own identifier-1 (§14.9.31.4 GR3).
Then the tag: the SEND needs an `Active` entry, so a NULL tag, a server tag or an answered message is
EC-MCS-NO-REQUESTER, and another `<h>` EC-MCS-INVALID-TAG (Q2). It writes `REPLY` with GR8's name (Q6) and removes the
entry; to an ended link it succeeds and drops the reply. The requestor's pump drains every frame, so the write never
waits on its program.

#### 5.4.3 RECEIVE FROM a server tag (requestor)

A held reply is delivered, and a forwarded name makes the RECEIVE unsuccessful (Q6). A link that ended with no reply gives
EC-MCS-ABNORMAL-TERMINATION; a reply already taken, or a serial never sent, EC-MCS-INVALID-TAG. Otherwise it waits.

#### 5.4.4 CONTINUE AFTER, decided by the holder

The seconds are evaluated once by `CobolTiming`'s rule (Q5), split from its suspension so the rule is shared.
`MessagingPeer` arms the deadline with `MessagingSettings.Time.CreateTimer`, whose callback posts `DeadlinePassed`. The
run unit's `Clock`, which `WISEOWL_COBOL_CLOCK` may freeze, is never used. MESSAGE RECEIVED, zero and a negative value ask
once.

At the deadline, the holder of the message decides. A server with a CLAIM in flight waits for its `GRANT` or `WITHDRAW`:
the requestor still holds the message, and §5.4.1's invariant bounds the wait. A requestor first takes every frame its pump
has queued, so a reply that arrived is delivered. Otherwise "the content of identifier-1 will be all space characters and
the RECEIVE statement is assumed to be successful" (§14.9.31.4 GR2), with data-name-2 zero. A SEND has no time limit
(GR4); a dead partner ends it through `LinkEnded`.

#### 5.4.5 Activation

A requestor that activates a name opens `start/<N>.lock` and holds `HostRegionLocks.McsStartByte` (a new byte of the one
region map) exclusively, waiting, through `HostRegionLocks.Open(handle, path, writable: true)`. On Linux and Windows the
byte is an open-file-description or `LockFileEx` lock on the file; elsewhere (macOS, any other Unix, and Linux when
`OfdRegionLocks.Available` is false) `HostRegionLocks` hands out `SidecarRegionLocks`, whose lock files live in the `.wolk-` directory beside `start/<N>.lock` (§5.1).
It never relies on a `FileShare.None` open: on Unix that is a `flock` that `SafeFileHandle.Unix.cs` calls "only advisory
locking" and drops when the file system answers ENOTSUP or EACCES. An `Unavailable` answer is EC-MCS-IMP. Holding the
lock, the requestor enumerates again, binds and listens at `n/<r>`, and launches `start` in the MCS directory with
`WISEOWL_COBOL_MCS`, `_SERVER`, `_NOTIFY` and `_LOG` (`servers/<N>.<r>.log`) set. `n/<r>` needs no rename: no one
enumerates `n/`, and the server learns the path only after it listens. The server publishes its listener, rings pending
bells, writes its standard output and error to the log, and connects to the notify path; a process that exits first is
EC-MCS-IMP. The requestor releases the lock, closes and deletes `n/<r>`, and continues at §5.4.1 step 5.

### 5.5 The peer, its pumps and partner death

`RunUnit.Messaging` is created at a run unit's first messaging statement, or when it begins if
`MessagingSettings.ServedName` is set. `MessagingSettings` joins `RunUnit.HostConfiguration` beside `Clock` and
`DebugMode`, so two run units of one process can differ (§7.2).

The protocol stays single-threaded. Each `PeerLink` runs one thread-pool read pump that posts `FrameArrived` or
`LinkEnded` to the peer's `Channel<PeerEvent>`; accept loops post `LinkOpened` and `BellRang`. Pumps touch no protocol
state; only the run unit's thread runs the protocol and writes. The pumps keep a server's reply from blocking on a busy
requestor, at the price that unreceived replies stay in the requestor's memory.

`LinkEnded` is a zero-byte read or any `SocketException`. `Socket.Receive` returns zero "only if zero bytes were requested
or if no more bytes are available because the peer socket performed a graceful shutdown". That the OS closes a killed
process's sockets so its peer reads end-of-stream or a reset is unverified from documentation, for Windows AF_UNIX above
all, and S0 decides it first. Exactly once, as programs see it, follows from `ACK` preceding the server's return.

### 5.6 Run-unit end

`RunUnit.Terminate` calls `Messaging?.End(EndedAbnormally)` in its `finally`, so a failed close cannot skip it. On an
abnormal ending it runs after `Files.EndTransactions(abnormal: true)`, since after the rollback "the runtime system
attempts to perform the operations of normal termination as specified in 14.6.11" (§14.6.12); on a normal one, after
`Files.CloseAll` and the static-storage reset (§14.6.11 item 7). It writes, for each `Active` entry,
`REPLY` with EC-MCS-NORMAL-TERMINATION or EC-MCS-ABNORMAL-TERMINATION and spaces of the recorded size, because "temp is a
temporary data item that is the size of the receiving data item in the associated RECEIVE statement for that message-tag"
(§14.6.11 item 7) that no statement set. It then closes links and listeners, its pending bell among them, and deletes
their socket files, in that order. A run unit killed without `End` reaches its partners as `LinkEnded`, and the
refusal rule of §5.4.1 deletes the files it left.

### 5.7 The runtime's public surface

```csharp
public static class AsyncMessaging      // Runtime/Messaging; PublicAPI.Unshipped.txt
{
    public static MessageOutcome SendToServer(string server, string image, RecordExtents? extents, object?[] slots);
    public static MessageOutcome SendResponse(MessageTag to, string image, RecordExtents? extents, object?[] slots, string? raising);
    public static MessageOutcome Receive(MessageTag from, int capacity, MessageWait wait, EcCheckingProfile profile);
}
public readonly record struct MessageOutcome(bool Successful, MessageTag Tag, string? Image, RecordExtents? Extents,
    object?[]? Slots, long Length);
public readonly record struct MessageWait(MessageWaitKind Kind, long Seconds);
```

`extents` is `OperandText.RecordAreaExtents`'s table, or null (§4.4). `MessageOutcome.Extents` is the table that came
with the received message, which the emitter hands to `PlaceRenderer.WriteVarGroupContiguous` for a variable-length
identifier-1.

## 6. Relation to the Communication facility

The communication facility keeps the paged store and bell of `DESIGN-communication-facility.md`: its messages outlive
their senders, and 2023 messages do not. The two share three things, in `src/WiseOwl.Cobol.Runtime/Mcs/`. This section
defines them once; `DESIGN-communication-facility.md` §6 ("Shared with the 2023 facility") refers here and names only
its own `bell/` entries. Its slice 2 built them, `McsFrame` with the kind `Ring`; slice S2a of §9 added the 2023 kinds
to the enumeration. `MaxPathBytes` is pinned by S0.

- **`McsDirectory.Resolve(configured)`** names the MCS directory: `configured`, else the environment variable
  `WISEOWL_COBOL_MCS`, else the current directory, made absolute with `Path.GetFullPath`. Files open at that path. Its
  canonical form, from which the identity is computed, resolves every symbolic link on the path, walking from the root
  and resolving each existing component with `FileSystemInfo.ResolveLinkTarget(returnFinalTarget: true)`, and on
  Windows is then upper-cased with `ToUpperInvariant`, because Windows file systems compare names without regard to case
  by default. So two spellings of one directory name one MCS. That this walk matches the OS's own resolution is not
  verified from documentation here; `McsResolutionTests` witnesses a link in a middle and in the final component, and a
  case variant on Windows. The identity `<h>` is the first 16 lower-case hex digits of the SHA-256 of the canonical
  form's UTF-8 bytes. `Resolve` returns a `McsDirectory` carrying `Path`, `Canonical` and `Identity`; the last two are
  computed on first use, so `MessageStore.Open`, which calls `Resolve(null)` and uses `Path`, pays no walk; the 2023
  facility passes `MessagingSettings.Directory`. An empty `configured` names nothing, as an unset variable does. The walk
  is lexical for `..` (`Path.GetFullPath` removes it first), follows a link's target through the same walk (so a link
  inside a target path resolves too), keeps a component that does not exist as written, and refuses a chain of more than
  40 links as a loop (`IOException`).
- **`McsSocketDirectory.Resolve(h)`** names the socket directory `<S>`. On Unix (Linux, macOS) it is
  `<base>/wiseowl-mcs-<uid>/<h>/`, where `<uid>` is the effective user id in decimal (`geteuid` through `libc`) and `<base>` is `$XDG_RUNTIME_DIR` when it is set, absolute and leaves room
  for the longest entry of either facility (`Fits(33)`, the 2023 facility's `p/<N>/<r>`, §5.1), else `/tmp`. It never uses `Path.GetTempPath()` on Unix, because macOS's per-user `$TMPDIR` is too long (§5.1).
  On Windows it is `Path.GetTempPath()` followed by `wiseowl-mcs\<h>\`, inside a per-user directory. On Unix every
  directory it creates is created with mode 0700 (`Directory.CreateDirectory(string, UnixFileMode)`), and an existing
  `wiseowl-mcs-<uid>` or `<h>` whose mode (`File.GetUnixFileMode`) grants anything to group or others is refused, by `McsSocketDirectoryRefusedException` (an `IOException`), which a directory that cannot be created
  throws too. `$XDG_RUNTIME_DIR` must also exist. `Resolve` returns a `McsSocketDirectory` whose `Path` ends in the
  separator. A
  directory this user can create entries in and whose mode grants nothing to anyone else is this user's, so no owner
  test is needed: a directory another user made first denies access and is refused the same way. Under `/tmp` another
  user can therefore deny a user the facility, but never read or plant a socket. A refusal is each facility's own
  failure: EC-MCS-IMP for 2023, a bell that cannot be bound for the communication facility. There is no setting that
  names another socket directory, so two users never share an MCS's sockets.
- **The budget.** `Fits(tailBytes)`, a member of the resolved `McsSocketDirectory` (its `<S>` is the one measured), is true when `<S>`'s length in UTF-8 bytes plus `tailBytes` is
  at most `McsSocketDirectory.MaxPathBytes` (the limit without its NUL: 107, and 103 on macOS; `PlatformAssumptionTests` asserts it against
  the longest path `Bind` accepts). Linux's `sun_path` "is 108 bytes in size" (unix(7),
  man7.org, read 2026-11-04) and macOS's is `char sun_path[104]` (`bsd/sys/un.h`, apple-oss-distributions/xnu main,
  read 2026-11-04), each counting the terminating NUL; "AF_UNIX comes to Windows" states no limit. §5.1 states the 2023 facility's longest entry (33 bytes) and
  its worst case per OS, and `DESIGN-communication-facility.md` §6 its bell's (21 bytes).
- **The run id `<r>`**: `McsSocketDirectory.NewRunId()`, 16 random lower-case hex digits. Each facility draws its own.
- **`McsFrame`**: `u32 length | u8 kind | payload`, little-endian, where `length` counts the payload only (not the kind byte), with a 2^31-byte guard (a payload of 2^31 bytes or more is refused,
  writing and reading). `Read` answers the kind and the caller compares it; a truncated frame, a kind byte no build knows, a `Ring` with a payload
  and an oversize length are `InvalidDataException`, and the caller closes the link. The kinds are one enumeration:
  `Ring`, which both facilities send to a bell with no payload, and the 2023 kinds of §5.3. A reader that meets a kind
  it does not expect closes the link.

Nothing else is shared: no session, state, lock, thread or process. `RunUnit.MessageControl` stays the communication
facility's and `RunUnit.Messaging` is this one's; under 2023 `--permissive` both run in one run unit independently. Each
reads its own members of `mcs.json`; `MessageStoreState.FromDefinitions` reads its four by name and ignores `servers`.

## 7. Tests

### 7.1 Single run unit: the per-edition suites

These are case files under the clause folders of the conformance suite (`DESIGN-conformance-suite.md` §3, §4), kind
`processor-dependent`, `requires: [module-async-messaging]` once revision 3's module rows exist, `accept: {from: 2023}`:

| Case file | Tests |
|---|---|
| `cases/13.18.60-USAGE/message-tag-usage.cob` | a level-1 and a strongly-typed message tag; NULL at start (§13.18.63.4 GR4); `FUNCTION BYTE-LENGTH` of a strongly-typed group holding one (DOC-A.1-212) |
| `cases/14.9.39-SET/set-message-tag.cob` | SET TO NULL; SET TO another tag (Q1) |
| `cases/8.8.4.2-Relation-condition/message-tag-relation.cob` | NULL = NULL; tag = NULL after INITIALIZE; NOT EQUAL; `<>`; two strongly-typed groups holding NULL tags compare equal (the unequal case needs a live party, §7.2) |
| `cases/14.9.20-INITIALIZE/initialize-message-tag.cob` | INITIALIZE sets NULL, bare, with the VALUE phrase and with DEFAULT; `REPLACING MESSAGE-TAG DATA BY tag2` copies tag2 |
| `cases/14.9.38-SEND/send-no-server.cob` | SEND to an unserved name: ON EXCEPTION taken, EXCEPTION-STATUS EC-MCS-NO-SERVER with checking on, the phrase still taken with checking off (Q8) |
| `cases/14.9.38-SEND/send-response-no-requester.cob` | SEND Format 2 with a NULL tag and with a tag never received: EC-MCS-NO-REQUESTER (Q2) |
| `cases/14.9.31-RECEIVE/receive-no-requester.cob` | RECEIVE FROM NULL in a run unit that serves no name: EC-MCS-NO-REQUESTER, ON EXCEPTION taken (Q2) |
| `cases/14.9.31-RECEIVE/receive-poll.cob` | run as a server of a name nobody sends to: RECEIVE FROM NULL ... CONTINUE AFTER MESSAGE RECEIVED, CONTINUE AFTER 0 SECONDS and CONTINUE AFTER -1 SECONDS: identifier-1 spaces, data-name-2 zero, NOT ON EXCEPTION taken; EC-CONTINUE-LESS-THAN-ZERO in EXCEPTION-STATUS for -1 with its checking on (Q5) |
| `cases/14.6.11-Normal-run-unit-termination/no-active-message.cob` | a run unit with no active message ends normally |

Each runs with `WISEOWL_COBOL_MCS` pointing at a fresh empty directory, which the runner supplies to every test
(`wiseowl/options` sets no runtime environment today; slice S4 adds this, with the first case that exchanges a message,
and `receive-poll` sets `WISEOWL_COBOL_MCS_SERVER` the same way). The refusals are one test each in
`wiseowl/diagnostics/`: SEND SR1, SR3, SR4 (both operands), RECEIVE SR1, SR2 (each clause, and CONSTANT RECORD for RECEIVE
only), SR3; SET Format 17 with a non-tag operand; the relation with a tag and a pointer; an ordering relation over a
strongly-typed group holding a tag (COBOL1535); a message tag as CALL BY VALUE argument (§14.9.4.3 SR22) and as ACCEPT
identifier-2 (§14.9.1.3 SR3); INITIALIZE REPLACING MESSAGE-TAG without identifier-2 and with a non-tag one (COBOL1982,
COBOL1983); NULL in a MOVE; the `CONTINUE AFTER MESSAGE` word that is not RECEIVED. Each refusal also runs at 2014, where
COBOL0900 is the expected diagnosis and no second one may appear; an EC-MCS name at 2014 stays COBOL0878
(`pb374-ec-mcs-name-below-2023`).

### 7.2 Two run units

**Platform witnesses first** (`unit:PlatformAssumptionTests`, Windows and Linux CI, slice S0), one test per OS fact §5
rests on that no document read states:

- a child killed while holding a connected AF_UNIX stream: the parent's receive returns zero or throws
  `SocketException`;
- the same for a connection still unaccepted in the killed process's listen backlog;
- a dead listener's socket file: `Connect` fails with `ConnectionRefused`, or the witness records the OS's error for
  `SocketTransport` to map;
- `Bind` over an existing socket file fails, and succeeds once it is removed;
- a `Connect` that lands after `Bind` and before `Listen`: the witness records that it is refused, the window §5.4.1's
  publishing closes;
- a listening socket's file renamed with `File.Move`: a `Connect` at the new name reaches the listener, and one at the
  old name finds no file (the publishing of §5.4.1; on failure that OS publishes under `McsPublishByte`);
- the longest path `Bind` accepts (`MaxPathBytes`), and §5.1's worst-case entry under the host's `<S>` fits it;
- `Socket.Select` over AF_UNIX, recorded for the fallback;
- the communication facility's bell cases (`DESIGN-communication-facility.md` §7): `Socket.Poll` on a listening socket
  returns at a pending connection; a non-blocking connect to a listener with room completes or answers `WouldBlock`,
  never `ConnectionRefused`; one to a listener whose backlog is full answers `WouldBlock` (Linux), `NoBufferSpaceAvailable` (Windows) or `ConnectionRefused`, recorded; a `WouldBlock` counts as the full-backlog answer only when the connect then fails to finish.

S0 lands the whole class, so the communication facility's slices add no case to it.

**Witnessed on Windows (slice S0, PB4739; .NET 10, Windows 11).** A killed child's sockets end for the peer as
`SocketException` `ConnectionReset`, both for an accepted connection and for one still in the listen backlog, so the
transport stands on Windows with no liveness byte. A killed listener's socket file survives and `Connect` to it answers
`ConnectionRefused` (native 10061), while a listener closed by its own process removes its file; `Bind` over a live
listener's file answers `AddressAlreadyInUse`. A connect between `Bind` and `Listen` is `ConnectionRefused`. `File.Move` of a
listening socket's file works: the new name reaches the listener and the old one answers `ConnectionRefused`, so Windows
publishes by rename and needs no `McsPublishByte`. `MaxPathBytes` is 107, as on Linux. A non-blocking connect to a listener
with room answers `WouldBlock` (Windows answers it whatever the backlog holds), then completes, and the byte arrives; so on
Windows a `WouldBlock` does not mean a full backlog, and a ring that answers it still arrives once the connect finishes.
A full backlog (`Listen(1)`, non-blocking connects held open) answers the sixth connect `NoBufferSpaceAvailable` (WSAENOBUFS);
connects 0 to 4 answer `WouldBlock` and complete. A `Bind` at the path of a listener closed by its own process succeeds.
The class's output carries each observed answer, and CI's Linux leg adds its own to this record.

The class runs on macOS unchanged when a macOS leg exists, and §1's claim widens with it. The first two decide the
transport (§9 S0); the rest set constants and mappings.

**Protocol** (`unit:MessagingProtocolTests`): `MessagingProtocol` over `InMemoryTransport`, whose `Abort` ends a link as a
kill does, under a scripted scheduler: two servers racing a CLAIM, WITHDRAW crossing CLAIM, `LinkEnded` at every step of
both parties, a server starting while a SEND waits (ring before, during and after the enumeration; lost or repeated), a
deadline with a CLAIM in flight or a reply queued; every offered link ending with none granted (EC-MCS-NO-SERVER after
one more enumeration, and a new server found by it); a server that publishes while a requestor enumerates, which is
never refused at its final name; a refused `s/` entry and a refused bell deleted; the bell deleted on every completion of
a SEND (success, EC-MCS-NO-SERVER, EC-MCS-MESSAGE-LENGTH before publishing, EC-MCS-IMP, run-unit end); a zero-length
SEND Format 2. A test `TimeProvider` supplies deadlines.

**In-process** (`conformance-test:MessagingExchangeTests`, Windows and Linux): requestor and server run in their own
`RunUnit`s and threads with their own `MessagingSettings` against fresh directories, and the test asserts their DISPLAYs.
The harness starts the requestor when the server's `WISEOWL_COBOL_MCS_NOTIFY` connection arrives; then the protocol
orders the two, so nothing sleeps. `MessagingPeer.Abort()` is the kill. Cases: the D.6.6
example; no server; two requestors and one server, replies routed by tag; tag equality, and groups differing only in a
tag's party; two servers of one name; a server started while a SEND waits takes the request; RAISING EXCEPTION and LAST
with checking on and off, and a level-2 name (Q6); a foreign tag; a long message (Q7) and `maxLength`; a server ending
normally, fatally and aborted with a message active; a requestor ending before the reply; a server tag reused; a message
carrying a tag; a variable-length identifier-1 with two OCCURS DYNAMIC tables, received by the same description (the
extent table restores both extents); an elementary message tag as identifier-1 (EC-MCS-MESSAGE-LENGTH); the only server
aborted during a SEND; `CONTINUE AFTER 2 SECONDS` with no request.

**Across processes** (`conformance-test:MessagingProcessTests`): `Process.Kill` on a server holding an active message, on a
requestor inside its SEND, and on a server a test hook stops between `GRANT` and `ACK` (never a message seen twice); a
`start` that activates a server; two requestors racing to activate one name (one launch); a `start` that exits first.

**Unit** (`unit:McsResolutionTests`): the two resolutions and `<h>` (links in a middle and the final component, a
case variant on Windows), the `$XDG_RUNTIME_DIR` fallback to `/tmp` on overflow, on Unix §6's directories created with
mode 0700 and an existing one open to group or others refused, the 14-byte kept and the hashed name
spellings, the run id, `McsFrame`, `MessageTag.SameParty`. Under Q9 the in-process pairs become case files.

### 7.3 Existing goldens

Every current SEND, RECEIVE and MESSAGE-TAG witness was written against the decline, and many compile only because no
item could be a message tag. A hand list of them drifts, so the inventory is a query, re-run by each slice before it
lands:

```
git grep -l -E "COBOL1578|COBOL1943|COBOL2205|McsFacilityUnsupported|MessageTagUsageUnsupported|McsMessageTagSetUnsupported|mcs-only|MessageTagRecovery" -- conformance-suite tests src docs
```

At origin/main 7ccb2822b it finds 130 files: 35 under `conformance-suite` (26 in `wiseowl/cases`, among them nine `pb4118-*` and six
`pb4090-*`), 49 non-C# files under `tests` (45 inventory rows, `inventory-schema.json`, `constructs.json`,
`conformance-register.json`, `tests/doc-facts/compiler-facts.json`), 16 test classes (among them
`DeclinedFacilityDriftTests`, `DerivedVerdictDriftTests`, `IntrinsicArgumentClassDriftTests`,
`UsageClausePhraseDriftTests` and `ValueClauseUsageSetDriftTests`), 22 `src` files and 8 docs. A slice rewrites each hit
whose answer it changes, with message-tag operands or as a §7.1 test, and leaves no hit that describes a retired
decline. `SendFormatClassificationTests` and `MessageTagIntroductionGateTests` stay, rebased on `MessagingBinder`.

## 8. Rejected alternatives

- **A file store for messaging** (whole-state JSON, share-none lease files, a sweep, a `FileSystemWatcher` wait). On Unix
  the lease is a `flock` that .NET drops on ENOTSUP or EACCES (`SafeFileHandle.Unix.cs`), so the sweep would read a live
  run unit as dead and answer its requestors EC-MCS-ABNORMAL-TERMINATION: a wrong answer. The watcher can miss events and
  needs a poll behind it.
- **A per-user broker process.** It leads on semantics and test seams, but a batch program that spawns a background
  process meets AppLocker, job objects and CI tree-kills; a broker of runtime N locks out a program built with N+1;
  elevated and normal processes of one user cannot talk; self-relaunch is unproven under single-file and AOT publishing.
  Operations outweigh its lead; its holder-decided time-out is §5.4.4.
- **The liveness-byte design (the fallback).** It keeps every 2023 obligation in the communication facility's paged
  store, `mcs.store` in the MCS directory, as rows: `RunUnits` (run unit, process id, liveness byte offset, bell path,
  served names), `Requests` (name and serial: requestor, image, slots, length), `Active` (serial: server, requestor,
  receiving size), `Replies` (requestor and serial: image, slots, length, exception-name) and `Waits`. Each statement is
  one store statement under the store mutex (`HostRegionLocks.StoreMutexByte`). **Liveness:** a run unit registers by
  taking its own liveness byte exclusively through its handle on `mcs.store`, then binding its bell, then inserting its
  `RunUnits` row; it deregisters in the reverse order, and the OS releases the byte when the process dies. **The sweep:**
  every 2023 statement first tests each registered byte; a free byte means a dead run unit, whose `Active` rows become
  EC-MCS-ABNORMAL-TERMINATION replies of spaces, whose requests, replies, waits and registration are deleted, and whose
  waiters' bells are rung. **The wake:** a waiter blocks on its bell and holds a watch, an open connection to the bell of
  the partner it depends on; a ring or the watch's end-of-stream means "ask again", and the store decides every outcome.
  It is rejected because it pays four durable commits per round trip for data that dies with its parties, and because
  its wake on byte release races: `LockFileEx`'s remarks say "the time it takes for the operating system to unlock these
  locks depends upon available system resources", so a watch can fire while the byte still reads held, and the waiter
  then has no further event. Its watches rest on the same AF_UNIX close semantics as §5.5. If S0's kill or backlog
  witness fails on Windows, it is the candidate the transport is re-decided with, and that race needs a second wake
  (a bounded re-ask after a watch ends) before it can be built.
- **A `record struct` tag.** Its `==` compares the message number, which §8.8.4.2.14 excludes: "The operands are equal if
  they reference the same message requestor or message server of the same name or one is NULL and the other is NULL".
- **Leaving a message longer than identifier-1 offered.** RECEIVE FROM NULL would take it forever.

## 9. Slices

Each slice is one kb/Work note. PB4700 depends on PB4677 only for the §6 types, which PB4677's slice 2 builds; S2a
starts after it lands. Every slice carries its goldens,
CONFORMANCE.md determinations and docs, narrows `derived-verdicts.mcs-only` to the rows still declined, re-runs the §7.3
query, and leaves no bound statement that no emitter renders.

| Slice | Content | Closes |
|---|---|---|
| S0 (PB4739) | `unit:PlatformAssumptionTests` (§7.2) green on Windows and Linux CI. **If the kill or backlog witness fails on Windows, stop:** no runtime slice starts, and the owner re-decides the 2023 transport with §8's liveness-byte design as the candidate. A failed rename witness only switches that OS to `McsPublishByte` publishing (§5.4.1) | |
| S1 (PB4740) | the type: `PicCategory.MessageTag`, `CobolClass.MessageTag`, `StorageForm.MessageTagRef`, the slot, `MessageTag`, NULL, VALUE, SET Format 17, relation Format 3 and strongly-typed groups, INITIALIZE, argument passing; COBOL1943, COBOL2205 and `MessageTagRecovery` retire; §7.1's cases for these; one witness per class rule of §2.3 and row of §4.6; DOC-A.1-212, -125, -46, D-SET17, D-MCS-ARG, CONFORMANCE §4 item 1's MESSAGE-TAG and SET paragraphs; the user guide's tag section; the §8.8.4.2.14 gap note | GR-13.18.60.4-9, SR-13.18.60.3-21, SR-14.9.39.3-35, GR-14.9.39.4-40, -41, GR-8.4.3.10.4-4, GR-8.8.4.2.1-9, DOC-A.1-212; witnesses for §4.6's CONFORMS rows |
| S2a (PB4741) | `MessagingProtocol`, `IPeerTransport`, `InMemoryTransport`, their unit tests; no socket, no `RunUnit` change | |
| S2b (PB4742) | `SocketTransport`, `PeerLink`, `MessagingPeer`, `MessagingSettings`, server start-up and pending bells, `End` in `Terminate`, `McsStartByte`, the `RuntimeConfig` rows; behavior-neutral for existing tests; D-MCS-TRANSPORT | |
| S3 (PB4743) | binding: `MessagingBinder`, the screens, the three nodes, the grammar renames and RECEIVED, with refusal goldens; `StatementBinder` still answers COBOL1578 and the EC-MCS decline stands | FMT-14.9.31.2, FMT-14.9.38.2, SR-14.9.31.3-1 to -3, SR-14.9.38.3-1 to -4 |
| S4 (PB4744) | the exchange: `AsyncMessaging`, `MessagingEmitter`, the `NodeKindRaises` rows and raise helpers, CONTINUE AFTER, the runner's per-test MCS directory, §7.1's SEND and RECEIVE cases, §7.2's in-process and kill cases; COBOL1578, the EC-MCS rows of `DeclinedEcFamilies` and `declined-raise-sites.json`, `mcs-only` and CONFORMANCE §4 item 1 retire; A.3 row 4 flips; D-MCS-SERVER, -LENGTH, -WAIT, -RAISE, -CHECK, D-MCS-NR, DOC-A.1-100; the user guide's messaging section; the §14.6.12 gap note | GR-14.9.31.4-1 to -5, GR-14.9.38.4-1 to -8, GR-14.6.11-7 |
| S5 (PB4745) | activation: `ServerActivation`, `start/<N>.lock`, notify, server logs, EC-MCS-IMP for a failed start, its process tests; Q9's `servers` field | |

S0 comes first, with S1 beside it. PB4677's slice 2 needs S0; S2a needs S0 and PB4677's slice 2, S2b needs S2a, S3 needs S1, S4 needs S2b and S3, S5 needs S4.
