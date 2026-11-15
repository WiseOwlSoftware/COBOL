## The VALIDATE facility has a design: S1 to S5, and the decline ends at 2002

PB5577 says 2002 requires VALIDATE and the compiler declines it. The note named no design doc, so the first change is the
doc: `docs/rearchitecture/DESIGN-validate-facility.md` (listed in DESIGN.md §0.5 and DOC_INDEX). It decides that the
facility is built at every edition that has it, as a bind-time expansion into per-element plans (the INITIALIZE shape), a
statement-local internal indicator, five stages laid out as five passes, EC-VALIDATE raised through the existing EC
machinery, and five build stages each landing parser, binder, emitter and golden together. No compiler code changed and no
row closed; the implementation starts at S1. Inert gate: see the branch's record.
