- `WE_REJECT`: is the refused construct legal ISO COBOL <<EDITION>> at the point WiseOwl refused it? Read the refused source
  line (the diagnostic names line and column) against the cited rule. Legal and refused -> `WISEOWL-DEFECT`
  (rejects_legal_source; reduce it, re-run on the fresh judge, code site = the diagnostic's emission site, found by
  grepping the code `COBOLnnnn` in `src/`). Illegal -> `WISEOWL-CORRECT` with the checked citation, or `MODEL-ERROR` when the
  program is simply wrong COBOL. The earlier records' refusal paragraphs list the families that recur: name the family and
  move on. Watch the multi-code signatures (two or more codes on one program), and a refusal of a construct that is legal
  only in a LATER edition than <<EDITION>> (the diagnostic names the edition it requires): that is WiseOwl being strict by
  the standard, `WISEOWL-CORRECT` once the edition is checked in `docs/VERSION_CHANGE_REFERENCE.md`.
