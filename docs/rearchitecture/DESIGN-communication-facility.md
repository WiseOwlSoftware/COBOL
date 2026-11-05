# The communication facility (COBOL-85 and COBOL-2002)

Status: IMPLEMENTED (kb/Work PB4677, owner decision 12 of PB4614, 2026-11-04). WiseOwl COBOL implements the facility at COBOL-85 and
2002, so the per-edition suites can witness it and NIST-85's CM module runs.

The authority is ISO/IEC 1989:2002 Annex A (Communications facility), with E.5 as its concepts section, read from the 2002 post-ballot text
(`E:/COBOL-coord/specs-drafts/t2002.txt`, general formats rendered from `x2002/cobolv200112.pdf`). The 2002 text is outside the tracked
tree, so citations of it are not `cite.py`-checkable and are written `ISO/IEC 1989:2002 A.n`, never with the `§` of the 2023 text. The
1985 text is not held: CCVS-85's CM programs are the witness of the COBOL-85 KEY phrase and status 40.

The facility has nothing to do with the 2023 asynchronous messaging statements (§14.9.31, §14.9.38), which share the words RECEIVE and
SEND and stay declined (docs/CONFORMANCE.md §4 item 1).

## 1. Editions

| Edition | The facility | How |
|---|---|---|
| 85 | Accepted, with the KEY phrase of ENABLE and DISABLE | no diagnostic |
| 2002 | Accepted, obsolete, no KEY phrase | COBOL0903 at each construct; COBOL0902 for the KEY phrase |
| 2014, 2023 | Removed | COBOL0902 at each construct; a warning under `--permissive`, and the program runs |

The rows are `communication-facility-removed-2014` (obsolete 2002) and `communication-key-phrase-removed-2002`. The 2002 Annex A says the
facility "is an obsolete element and is to be deleted from the next revision of standard COBOL", and the 2014 FCD has no such section.
Everything parses at every edition (the union grammar); the parse arm of `VersionConformancePass` gates each rule on recognition, so a
statement is named even where no section declares its cd-name. §8.9 already reserved the facility's words at 85 and 2002 only, so each
new lexer token is a nameSlot row of `cobol-words.json`, gated by the reservation table, and is an ordinary user word from 2014
(`pb4677_user_words_from_2014`).

## 2. Grammar

`Grammar/Core/CobolCommunication.g4` holds `communicationSection`, `communicationDescriptionEntry` and the statements. The printed page
decides the optional words: SYMBOLIC, IS, FOR, TIMES, BY, WITH, ADVANCING, the MESSAGE of MESSAGE COUNT and the LINE of REPLACING LINE
carry no underline.

Two statements need the union with the 2023 forms. RECEIVE parts by the word after its operand (`GIVING` against `MESSAGE` or `SEGMENT`).
`SEND x FROM y` is the shape of both A.2.6.1 Format 1 and §14.9.38.2, and only x's declaration can tell them apart: the `cdName` rule's action
records each cd-name, and the `statement` arm of the 2023 SEND is predicated on `communicationNameFollows()`. A data division precedes its
procedure division and an action never runs during prediction, so the set is complete when a statement asks. A SEND that writes an indicator
fails the 2023 arm and reaches `communicationSendStatement` whatever x is, so an undeclared name there is COBOL6170.

The three formats of the entry are one rule. `CommunicationFormats` (the binder) is the one table of which clause each format prints, so a clause of
another format is refused by name.

## 3. The record area

A CD is a redefinition class, the FD's implicit record area applied to a new section (`CommunicationSectionBinder`):

- `CommunicationDescription.Area` is the record A.1.2.3 prints as the "IMPLICIT DESCRIPTION", built from `CommunicationFormats`: an unnamed level 1 group of
  elementary items, each named by its clause or by its place in the series, unnamed otherwise (`FILLER`). The output table is flat without a DESTINATION TABLE
  OCCURS clause (A.1.2.2 SR11) and an `OCCURS` group with it (SR12, with its INDEXED BY names).
- Every record written after the CD joins the class by `RedefinitionKind.ImplicitCommunicationRecord`. The class's anchor is the FIRST written record when there is
  one, because "only the first redefinition may contain VALUE clauses" (SR5, SR9, SR14) and a class's seed is its anchor's.
- The items are static like working-storage's (ISO/IEC 1989:2002 8.6.3), so they join `WorkingStorageRoots`, under `EntrySection.Communication`.
- A cd-name is a user word of its own type (`UserWordKind.CdName`) and no data-name. The `cdName` rule does not ride `dataName`, whose FILLER and PROCEDURE are reserved words (A.1.2.1, 8.3.1.1.1): a word
  §8.9 reserves draws COBOL0901 through `reservedGatedWord` and `IsProvableUserWordPosition`, and FILLER or PROCEDURE draws COBOL5506. It qualifies an item of its entry (the file-cd-report-qualifier of 8.4.1.1.1), through the
  one qualifier walk.

Asked at the entry: SR2 (INITIAL once, and not with USING), SR4 and SR8 (some way of describing the area), SR6, SR10 and SR15 (unique data-names), and Annex A's "only in a program definition". `LevelNumberPass` asks 13.16.31 SR2 (no level 77) with the FD's arm, and `ResolveRedefinesPass` 13.16.42
SR4 (no REDEFINES at level 1). A residence row of `ClausePlacementRules` lists the sections its sentence lists, so EXTERNAL, GLOBAL (2002 13.16.25.2 SR1 b) and BASED refuse a CD's record as COBOL2404. Asked once the forest is complete (`CheckCommunicationRecordsPass`): SR5 and SR14 (exactly 87 or 33
positions) and the VALUE rule. All are COBOL6169 with the rule quoted, the COBOL1720 precedent.

## 4. The statements

`CommunicationBinder` builds one `BoundCommunicationStatement` per statement, carrying `Cd.Area`'s place: they are one operation over one area, and the optional members are the operands
each format prints. COBOL6170 is a name that is no cd-name, or a cd-name of a format the statement's rule does not admit. COBOL6171 is an operand of the wrong class or category.

The ADVANCING phrase's operands are screened and kept nowhere. The devices of this MCS are oriented to variable-length messages (A.2.6.3 GR2), so vertical positioning "is not
applicable" and "the MCS will ignore" it (GR12). REPLACING LINE follows.

The facility raises no exception condition: "exception handling [is] intentionally omitted from the obsolete communications facility" (Annex A NOTE; 14.5.12.1 NOTE 2).

## 5. Code generation

`CommunicationEmitter` renders one `MessageControl` call per statement. The area goes over through `OperandText.RecordAreaImage`, the one record-area channel, and comes back through
`SequentialIoEmitter.EmitImageInto` verbatim (other descriptions read the same storage). "The message control system (MCS) always references the record according to the data description
defined in" GR2, GR16 and GR24, so the runtime reads positions and a user's redefinitions never reach it. RECEIVE returns a `CommunicationReceipt`: the area, the receiving item's image, and
which phrase runs. A CD with INITIAL sets its names once per instance, before the first procedure division statement (GR6, GR28).

## 6. The message control system

E.5.1 requires "that a message control system (MCS) be present in the run unit's operating environment", and everything about it is the implementor's. No .NET host has one, so the runtime is
one (`Runtime/Communication`): `MessageControl` the entry points, `CommunicationArea` the positions, `MessageControlSession` the run unit's half, `MessageStore` the MCS's.

**The MCS directory.** The environment variable `WISEOWL_COBOL_MCS`, else the current directory. It holds `mcs.json`, the user's definitions ("defined by the user to the message control system
at some time prior to the execution of the run unit", E.5.1), and the MCS's own `mcs-state.json`, `mcs.lock` and `<device>.out`. With no `mcs.json`, every symbolic name is unknown.

```json
{
  "queues": [
    { "name": "Q1", "key": "PASSWORD", "enabled": true,
      "messages": [ "ONE SEGMENT", { "segments": ["A", "B"], "group": true, "source": "T1", "date": "260102", "time": "03040506" } ],
      "subQueues": [ { "name": "S1" } ] }
  ],
  "sources": [ { "name": "T1", "key": "PASSWORD", "enabled": true } ],
  "destinations": [ { "name": "D1", "key": "PASSWORD", "enabled": true, "capacity": 100, "deliverTo": "Q1/S1", "source": "T1" } ],
  "terminals": [ { "name": "T2", "key": "PASSWORD", "enabled": true, "messages": [] } ]
}
```

Every member but `name` is optional. A name is 1 to 12 characters with no `/` or `\`, since a queue path and a device file are built from names; queues nest to the four levels of E.5.5.4. `key` is the COBOL-85 password (a KEY phrase's value against it, status 40; an endpoint with
none admits any). `messages` enter once, when the store first answers. `capacity` bounds a destination's waiting messages (status 65, error key 6). `deliverTo` makes a destination feed a queue path or
`terminal:<name>`, so one run unit talks to another; a destination without it transmits to its device, the file `<name>.out`, and a terminal's SEND does too.

**The transaction.** Each statement takes the exclusive lock (`mcs.lock`, a share-none open, waited out for up to 500 retries of 20 ms, then status 92), reads the state and acts. The commit is `MessageStore.Commit`: the state goes to a
temporary file, the devices take their messages, and the state file moves into place. Two run units never see a half-applied statement. The cost is linear in what the store holds, which an obsolete facility's traffic does not make worth more.

**Status 9x takes no action, by construction.** `MessageControlSession.Statement` is the one path of every statement: the action works on a copy of the CD's area and a copy of the run unit's half, and only a committed store publishes them, so a refused statement
answers the area it was handed with its status key changed. A commit the host refuses at any step takes back what the devices took (each device file goes back to its length before the statement, or away), so a program that retries on 9x delivers once. A crash
between the devices and the move leaves the device's message and the old state: the one window a file system with no two-file transaction leaves, which can repeat a message and never loses one.

**The run unit's half.** A portion released and not ended, the remainder of a message begun, and each input-output CD's first terminal live in `MessageControlSession`, on the `RunUnit`. ISO/IEC 1989:2002 14.5.10 rule 2 follows by
construction: "remaining portions of any partially-received messages are eliminated from the queue. Any portions of messages transferred from the run unit by a SEND statement, and not terminated by an EMI or EGI, are purged
from the system."

**Scheduling.** A process that starts the run unit on the MCS's behalf sets `WISEOWL_COBOL_MCS_SCHEDULED`, the levels of the queue separated by `/` or the terminal's name; the INITIAL CD then holds those names (GR6, GR28) and an
input-output CD's terminal is fixed from the start (GR30). Unset, the names are spaces.

**Choices the rules leave.** A level not named at RECEIVE is chosen alphabetically, a level's messages first in, first out (E.5.5.3, E.5.5.4). Data is "not made available" when the designated structure holds no complete message (B.1 item 116).
A disabled queue still answers RECEIVE: DISABLE deactivates "the logical paths between the queue ... and all the associated enabled sources" (A.2.2.3 GR3), and CCVS-85 CM105M reads after disabling. A message waits in its destination's
output queue while the destination, the input queue it feeds, or its source's path is disabled, and moves on the statement that opens the path (E.5.5.1). An error key is written '0' for a destination with no error (Table A.2, and the CM102M
report's `00/0`). Status 80 appears only for destinations, the one place one statement names several. Status 9x: 91 an unusable `mcs.json`, 92 a store the host refuses (B.1 item 28), each with no action, and a RECEIVE passes control as if data had been made
available. No alphabetic error key is defined (B.1 item 27). ADVANCING is §4 above.

## 7. Witnesses

- `conformance:cases/_unassigned/pb4677_cd_area_layout`, `pb4677_initial_cd_spaces`, `pb4677_user_words_from_2014`: what any MCS answers.
- `conformance:wiseowl/cases/_unassigned/pb4677_mcs_receive`, `pb4677_mcs_send`, `pb4677_mcs_paths`, `pb4677_mcs_key_85`, `pb4677_mcs_transaction`, over the shared `mcs.json` beside them.
- The refusals `wiseowl/diagnostics/pb4677-*`: each rule above, one program each.
- `unit:MessageControlTests`: statuses 91 and 92, no action on each statement a store refuses, a refused device, the lock waited out and never released, a message across run units, a RECEIVE waiting on another run unit, GR30 and 14.5.10 rule 2.
- `conformance:CommunicationCcvsTests`: all nine CM programs compile at COBOL-85; CM102M, CM103M, CM105M and CM201M run against `tests/nist/data/mcs.json` with no CCVS failure. CM101M, CM104M and CM202M wait out
  wall-clock delays and an operator, and CM303M and CM401M test the federal flagging layer, outside the ISO suite (PB4614 decision 11).

The programs sit in `_unassigned` because the traceability inventory derives from the 2023 text, which has no rows for this facility. Placing them under a clause waits for the inventory to hold pre-2023 rules (PB4614's design revision).
