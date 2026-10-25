*>--- conformance-case
*> id: pb935-set-entry-restricted-literal
*> rules: [GR-13.18.60.4-25, SR-14.9.39.3-22]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.4 GR25 over the vendor SET … TO ENTRY spelling (kb/Work PB935).
      *>   "If program-prototype-name-1 is specified, this data item is a restricted program-pointer. A restricted
      *>   program-pointer shall contain only the predefined address NULL or the address of a program with the same
      *>   signature as that identified by the specified program-prototype-name."
      *>   cite.py: OK  §13.18.60.4 25)  (General rules)
      *> The obligation is on the ITEM, whichever statement loads it. PP is restricted to PB935X (RPT is a TYPEDEF,
      *> §13.18.60.3 SR19) and the ENTRY operand is a literal, which names a program by VALUE: its sender is an
      *> unrestricted program-address, associated with no program-prototype, exactly the literal-1 arm of
      *> ADDRESS OF PROGRAM that §14.9.39.3 SR22 refuses for a restricted receiver (§8.4.3.13.4 GR3 gives only the
      *> program-prototype-name-1 arm a restriction). Both prototypes precede the program and are named in its
      *> REPOSITORY. The conforming spelling is SET PP TO ADDRESS OF PROGRAM PB935X, the positive golden
      *> 13.18.60-USAGE/pb817_restricted_program_pointer. Refused COBOLNET1959 (prototype-pointer-signature) at
      *> every edition that has the program-pointer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB935X IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(2).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM PB935X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB935N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB935X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RPT IS TYPEDEF USAGE PROGRAM-POINTER TO PB935X.
       01 PP TYPE RPT.
       PROCEDURE DIVISION.
       MAIN-P.
           SET PP TO ENTRY "PB935X"
           STOP RUN.
       END PROGRAM PB935N1.
