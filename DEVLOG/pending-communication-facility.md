## The communication facility of COBOL-85 and 2002 is implemented, with a message control system in the runtime

Owner decision 12 of PB4614 (kb/Work PB4677). The re-probe on base 83365cbf3: a COMMUNICATION SECTION drew COBOL0001 at every edition, so all nine CCVS-85 CM programs failed to compile.

The design is new, `docs/rearchitecture/DESIGN-communication-facility.md`, read from the 2002 post-ballot text's Annex A and E.5 with the general formats rendered from its PDF; the 1985 text is not held, so CCVS-85 is the witness of the COBOL-85 KEY phrase and status 40. The grammar is the union grammar in `CobolCommunication.g4`, gated on recognition by `communication-facility-removed-2014` (obsolete 2002) and `communication-key-phrase-removed-2002`. `SEND x FROM y` is both A.2.6.1 Format 1 and the 2023 §14.9.38.2, so the 2023 arm is predicated on x not being a declared cd-name. A CD is a redefinition class: the implicit description the rules print, built from one table, and the written records, with the first written record as the anchor so its VALUE clauses seed the area. Each statement hands the area's image to `MessageControl` and stores back what it returns. The MCS is `Runtime/Communication`: definitions in `mcs.json`, state in `mcs-state.json`, one lock and one atomic write per statement, and what a run unit has half sent or half received lives on the `RunUnit`, which is ISO/IEC 1989:2002 14.5.10 rule 2 by construction. The determinations are `docs/CONFORMANCE.md` §3.3.

Goldens: `pb4677_cd_area_layout`, `pb4677_initial_cd_spaces`, `pb4677_user_words_from_2014`, `pb4677_mcs_receive`, `pb4677_mcs_send`, `pb4677_mcs_paths`, `pb4677_mcs_key_85`, `pb4677_mcs_transaction`, thirty refusals `pb4677-*` (COBOL6169, COBOL6170, COBOL6171, COBOL0902, COBOL0901, COBOL2739), `unit:MessageControlTests` for statuses 91 and 92 and the lifetime rules, and `CommunicationCcvsTests`: all nine CM programs compile at COBOL-85, and CM102M, CM103M, CM105M and CM201M run with no CCVS failure. Rows re-verdicted: none; the inventory derives from the 2023 text, which has no row for this facility, so the programs sit in `_unassigned` and `closes_rows` is empty with that reason.

Two stale sentences went with it: the grammar and `cobol-words.json` said RECEIVE and SEND were user words at 2002 (PB4297 corrected the table to 85 and 2002 reserved, 2014 free).

Self-review, four lenses: GATE_SELF_REVIEW

GATE_VERDICT
