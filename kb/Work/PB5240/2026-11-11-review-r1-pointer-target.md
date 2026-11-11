**Review round 1 (2026-11-11, wave 20262001 group B, finding R1-1): a program-pointer function result as the CALL target.**

The CALL arm this note added for a function-identifier or inline method invocation target had no program-pointer case. A
user-defined function whose RETURNING item is USAGE PROGRAM-POINTER was refused COBOL1681, although §2023:14.9.4.3 SR1
admits a program-pointer identifier-1 and §2023:8.4.3.2.4 GR1 gives the result temporary the returning item's category
(probe E:/COBOL-coord/scratch/review-w20262001-b/p4.cob, refused at the reviewed head 4c39ad681d).

Fix. Both CALL arms go through one screen, `CallBinder.Identifier1Screen`: a program-pointer operand of either form sets
`IsPointerTarget` (the SR14 signature check, then `ProgramRegistry.CallPointer`); any other category goes through
`OperandTargetRefusal`, which reads a data item or a returning item through `NameTargetRefusal`. Golden:
`conformance-suite/cases/14.9.4-CALL/pb5240_call_function_program_pointer` (2002, 2014, 2023). The report SOURCE site
stays open on this note.
