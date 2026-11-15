## Entry 3672 — 2026-11-15 02:56 PST — Communication Section binder: an implicit CD field carries its PICTURE, and a pointer or object-reference record under a CD is out of line

Two fixes in `CommunicationSectionBinder`, both reproduced on this build before the change.

**PB5617.** `AddSlot` built each implicit CD field (TEXT LENGTH, STATUS KEY and the rest) as a bare `PicInfo` with no PICTURE clause identity. §2002:A.1.2.3 GR16 describes them as `02 data-name PICTURE 9(4)`, `PICTURE XX` and so on, and the §2002:14.7.1.2.1 conformance test compares PICTURE, so passing TEXT LENGTH to a PIC 9(4) formal or returning into it was refused COBOL0828 (`INVOKE "GET42" RETURNING OTL`). `AddSlot` now analyses the PICTURE text of the A.1.2.3 description through `PictureAnalyzer.Analyze` and keeps it in `PictureText`. The probe prints `5=0042`.

**PB5604.** `BindEntry` linked every CD record into one implicit redefinition class, with no arm for a record that has no character window. A level-1 USAGE POINTER, PROGRAM-POINTER or OBJECT REFERENCE record under an output CD (§2002:A.1.2.2 SR9, §2002:13.16.58.2 SR13) reached the class model and was staged COBOL0899 "not yet implemented". The class now takes only the records that have a window (`FileModel.IsOutOfLineRecord` says which, as for an FD since PB981); the anchor is the first of them, or the area. `cd_ptr_min.cob` and `cdptr.cob` compile and run.

Goldens: `pb5617-cd-implicit-field-by-reference-and-returning` (INVOKE RETURNING, INVOKE USING, CALL BY REFERENCE with TEXT LENGTH and STATUS KEY) and `pb5604-output-cd-pointer-records` (ALLOCATE, SET, CALL, INVOKE, RAISE and the SET LOCALE formats through CD pointer, program-pointer and object-reference records), each accepted at 2002 and refused at 85 (`wiseowl/diagnostics`). The 13 rows re-verdicted CONFORMS in the 2002 inventory are listed in the notes' landing files (kb/Work/PB5604, PB5617).

Lead, not fixed: `ImplicitRegisters.cs` (line 84) and `FileControlBinder.FileSection.cs` (line 351) also build an implicit item with a bare `PicInfo`; not probed.

