**2026-11-15, the golden-2002-8 integrator (gl2002 wave u, slug g2002u-13-16-61-value-p2): six VALUE rows whose 2002 scope reaches format 5 join `inventory_rows`.**

Rows added: `2002-SR-13.16.61.2-2`, `2002-SR-13.16.61.2-4`, `2002-SR-13.16.61.2-5`, `2002-SR-13.16.61.2-6` (each headed ALL FORMATS, or FORMATS 1, 2, 3, AND 5 for SR6), `2002-SR-13.16.61.2-24` and `2002-GR-13.16.61.3-10` (FORMATS 3 AND 5). The writer's rows were `defect-suspected`: the format 1-4 legs are drafted, but a row is closed only with its format 5 leg, which this note's refusal blocks. The drafts (2 accepted cases and 23 refusals) stay out of the tree under `E:/COBOL-coord/scratch/gl2002/lane1/out/g2002u-13-16-61-value-p2/` until this lands.

**Rules (checked with `python scripts/spec/cite.py --check`, verdict OK).**

- `§2002:13.16.61.2 SR2`: "all literals in the VALUE clause shall be numeric"
- `§2002:13.16.61.3 GR10`: "The manner of determining the range of values specified by the THROUGH phrase"

**Repro, re-run on the golden-2002-8 build (main 5e49d46678).** `E:/COBOL-coord/scratch/gl2002/integrate-8/repro/f5/f5.cob` (from `lane1/run/g2002u-13-16-61-value-p2/probe-f5/`), command `cobol.exe f5.cob --std 2002 --run`: rc 65, `f5.cob(6,40): error COBOL1708: the VALUE clause's content-validation entry (ISO §2023:13.18.63 format 5 ...`.

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VQF5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(2) VALUE 7.
          88 GOOD-VALS VALUES 1 THRU 5 IS VALID.
       PROCEDURE DIVISION.
           VALIDATE WS-A
           DISPLAY "VALIDATED".
           STOP RUN.
```

Expected once VALIDATE is claimed: accepted, and `VALIDATE WS-A` treats 7 as outside 1 THRU 5; then a format 5 literal outside the PICTURE (`VALUES 1 THRU 100 IS VALID` on PIC 99, SR2), too long (SR4, SR5) or not boolean (SR6) is refused, and the THRU range follows §2002:14.6.7 (SR24, GR10). The fix's goldens are the held drafts plus those format 5 legs.
