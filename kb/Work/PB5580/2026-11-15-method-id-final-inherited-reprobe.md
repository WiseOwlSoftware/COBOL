**Re-probe (the golden-2002-4 integrator, build of main d077329121).** The gl2002 writer of g2002-11-6-method-id
hit this note's refusal while witnessing `2002-SR-11.2.2-6` and the "any subclasses" scope of `2002-GR-11.6.3-3`:
`E:/COBOL-coord/scratch/gl2002/integrate-4/repro/mi_final.cob` (class G2MIC INHERITS FROM G2MIA G2MIZ; G2MIA's
method M1 IS FINAL, inherited and not overridden). Expected at --std 2002: accepted, printing `A-M1` then `Z-MZ`.
Observed: `error COBOL0849` at (34,44), "Multiple inheritance ... is an OPTIONAL element (§2023:4.2.7; Annex A.4.10
item 1)", and a consequent COBOL0825 for the INVOKE of MZ. Both rows are already in this note's `inventory_rows`; the
writer's single-inheritance goldens for 11.6 landed in golden-2002-4 and the multiple-inheritance leg stays open here.
