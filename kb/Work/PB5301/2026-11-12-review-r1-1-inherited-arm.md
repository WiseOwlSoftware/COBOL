**Review finding R1-1 of the w20262006b finisher (2026-11-12, wave 20262008 group B): the inherited arm.**

**Sites:** `src/WiseOwl.Cobol.Compiler/Binding/DataBinder.Configuration.cs` (`ConstantOfIdentifierPosition`).

The finisher's fix counted only depth-0 data items against a constant the element declares itself, and still counted every
data item for a constant INHERITED from a container. A GLOBAL data item of a FARTHER container then made the name
ambiguous: `E:/COBOL-coord/scratch/review-worktree-wf_ea985d42-ace-7/n3.cob` (OUTERP: `01 KK PIC 9 VALUE 1 GLOBAL`; MIDP:
`01 KK CONSTANT IS GLOBAL AS 5`; INNERP: `DISPLAY KK`) was refused with COBOL1639 at 7011bacefb.

§2023:8.4.6.2.1 GR3b.1 (cite.py --check OK): "The item in source element A if the name is declared in source element A."
The nearest declaring element wins, so only a data item declared AT THE CONSTANT'S OWN TIER competes with it: the tier is 0
for the element's own constant (or the method's), else `ConstantEntryBinder.InheritedDepthOf`; a nearer data item has
already removed an inherited constant (`InheritStep.DropShadowed`). After the fix n3 displays `5`, and a container that
declares both a GLOBAL constant KK and a GLOBAL data item KK still draws COBOL1639 in its contained program.

Goldens: `cases/8.4.2.2-Qualification/pb5301_global_constant_nearer_than_farther_global_data` (accepted from 2002, `5`) and
`pb5301-global-constant-and-data-same-container` (refused at 2002, 2014, 2023, COBOL1639).
