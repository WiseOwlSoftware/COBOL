**2026-11-15, the golden-2002-8 integrator (gl2002 wave u, slug g2002u-13-16-61-value-p2, a writer's lead): the VALUE clause's literal refusals cite §2023:13.18.63.3 at --std 2002.**

No row added: the refusals are right in outcome at 2002, and the rows they reach (`2002-SR-13.16.61.2-2`, `-4`, `-5`, `-6`) are blocked by PB5577's format 5 leg.

Re-run on the golden-2002-8 build (main 5e49d46678), each `cobol.exe <file> --std 2002`, the writer's held drafts under `E:/COBOL-coord/scratch/gl2002/lane1/out/g2002u-13-16-61-value-p2/conformance-suite/cases/13.16.61@2002-VALUE-CLAUSE/`:

- `g2-value-sr4-ne-num-ws.cob`: `COBOL0900: numeric-literal VALUE for a numeric-edited item requires COBOL-2023 (targeting COBOL-2002) ... (ISO §2023:13.18.63.3 SR6; Annex E.3.3 item 43)`. The 2002 ground is `§2002:13.16.61.2 SR2` (a numeric-edited item takes alphanumeric literals, SR4).
- `g2-value-sr2-alnum-ws.cob`: `COBOL1657: ... all literals in the VALUE clause of a numeric item shall be numeric (ISO §2023...` where `§2002:13.16.61.2 SR2` governs.
- `g2-value-sr5-nne-alnum-ws.cob`: `COBOL5984` (class national) cites the 2023 rule where `§2002:13.16.61.2 SR5` governs.
- `g2-value-sr6-88-ws.cob`: `COBOL5982: ... (ISO §2023:13.18.63.3 SR10)` where `§2002:13.16.61.2 SR6` governs.

`python scripts/spec/cite.py --check "§2002:13.16.61.2 SR2" "all literals in the VALUE clause shall be numeric"`: OK. One mechanism with this note's other 2002 instances: the single 2023 `IsoSection` per diagnostic.
