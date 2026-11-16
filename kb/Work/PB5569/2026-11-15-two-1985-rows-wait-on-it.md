**2026-11-15: two 1985 rows wait on this fix (registrar re-read of the open PARTIAL 1985 rows).**

Re-run on the build of branch `records-1985-reread` (cut from main 2722570af0), `--std 85`, through
`E:/COBOL-coord/scratch/gl1985/run_case.py` (run directories under
`E:/COBOL-coord/scratch/op-20261115/registrar-1985-reread/runs/misc/`). Both rows' stored records now name this note as
their owner; the fix's landing re-records them (a witness of its own at 85 for each, from the repros below once they
print the expected lines).

**`1985-SR-IV.4.2.2.2.3-L3.2`.** §1985:IV.4.2.2.2.3 GRL2.1: an ALL literal is "repeated character by character on the
right until the size of the resultant string is greater than or equal to the number of character positions in the
associated data item", and §1985:VI.5.12.4 GR6: a SIGN SEPARATE position "is counted in determining the size of the
item" (both `cite.py --check` OK). Repro `E:/COBOL-coord/scratch/op-20261115/open-rows-1985/work-a/r1-sep.cob`:
prints `a F | b T | c T | c2 T | e T`; expected `b F`, `c F`, `c2 F` (S4 PIC S9(3) SIGN LEADING SEPARATE VALUE 121
against ALL "12": the operational sign is not moved, so the 4-position temporary is "121 ", and ALL "12" repeated to 4
positions is "1212": false; the repro's own `e T`, S4 = "121 ", shows the temporary). The MOVE arm holds:
`work-a/r1-main.cob` prints `N4 [1212] | N3 [121] | NV [121.00] | NE [212.00] | NZ [21,212] | NS [+212] | NC [1212] |
r1 T | r2 F | I [1212][212.00]`, the 1985 results (PB5176, landed).

**`1985-GR-VI.6.3.1.1.2-1`.** §1985:VI.6.3.1.1.2 GR1: the numeric operand "is treated as though it were moved to an
elementary alphanumeric data item of the same size as the numeric data item" (OK), the SIGN SEPARATE position counted
(VI.5.12.4 GR6). Repro `work-a/r4-main.cob`: prints `m1 T | m2 T | m3 F | m4 T | m5 T | a T | b F | c F | MOVE N1 TO X3
[120] | d F`; expected `c T` and `d T` (S99PP SIGN TRAILING SEPARATE -1200 compared with its own three-character MOVE
image "120"). The unsigned P-scaled arm (`a T`, `b F`) is PB5187's landed fix.

Site, as the adjudicator located it: `src/WiseOwl.Cobol.Compiler/CodeGen/Emit/OperandText.cs` (`OperandText.AsItemCharacters`),
read by `ConditionRenderer.ComparisonText` and `ConditionRenderer.RenderFigurativeRelational`.
