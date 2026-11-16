      *>--- conformance-case
      *> id: g2alloc01_data_name_null_init
      *> rules: [2002-GR-14.8.3.3-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.3.3 GR9 - "If the INITIALIZED phrase is not
      *> specified and data-name-1 is specified, data items of class
      *> object or class pointer in the allocated storage are
      *> initialized to null". BR is a based strongly-typed record
      *> holding a data-pointer (BP), a program-pointer (BPP), an object
      *> reference (BO) and a table of data-pointers (BTP); none carries
      *> a VALUE. GR4b sets BR's address to the new storage, so each
      *> ALLOCATE BR without INITIALIZED must leave every one of them
      *> null. Derived lines:
      *>   1st ALLOCATE: BP NULL, BPP NULL, BO NULL, BTP NULL
      *>   SET BP and BTP(2) to ADDRESS OF W: BP SET, BTP SET (control)
      *>   2nd ALLOCATE: BP2 NULL, BTP2 NULL
      *> The content of BX (class alphanumeric) is undefined by the same
      *> rule and is never read.
      *> Fails if: any pointer or object reference in the new storage is
      *> not null (an uninitialized allocation, or a second ALLOCATE that
      *> hands back the first storage unreset: BP2 SET / BTP2 SET), or
      *> the SET control did not take effect (the second leg would then
      *> prove nothing).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALC01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W   PIC X(4) VALUE "ABCD".
       01 BRT TYPEDEF STRONG.
          05 BX  PIC X(4).
          05 BP  USAGE POINTER.
          05 BPP USAGE PROGRAM-POINTER.
          05 BO  USAGE OBJECT REFERENCE.
          05 BT  OCCURS 2.
             10 BTP USAGE POINTER.
       01 BR TYPE BRT BASED.
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE BR
           IF BP = NULL DISPLAY "BP NULL" ELSE DISPLAY "BP SET" END-IF
           IF BPP = NULL DISPLAY "BPP NULL" ELSE DISPLAY "BPP SET"
           END-IF
           IF BO = NULL DISPLAY "BO NULL" ELSE DISPLAY "BO SET" END-IF
           IF BTP(2) = NULL DISPLAY "BTP NULL" ELSE DISPLAY "BTP SET"
           END-IF
           SET BP TO ADDRESS OF W
           SET BTP(2) TO ADDRESS OF W
           IF BP = NULL DISPLAY "BP NULL" ELSE DISPLAY "BP SET" END-IF
           IF BTP(2) = NULL DISPLAY "BTP NULL" ELSE DISPLAY "BTP SET"
           END-IF
           ALLOCATE BR
           IF BP = NULL DISPLAY "BP2 NULL" ELSE DISPLAY "BP2 SET" END-IF
           IF BTP(2) = NULL DISPLAY "BTP2 NULL" ELSE DISPLAY "BTP2 SET"
           END-IF
           STOP RUN.
       END PROGRAM G2ALC01.
