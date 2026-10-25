*>--- conformance-case
*> id: pb935-set-entry-restricted-identifier-list
*> rules: [GR-13.18.60.4-25, SR-14.9.39.3-22]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.4 GR25 over the vendor SET … TO ENTRY spelling, identifier operand and a receiving LIST
      *> (kb/Work PB935). "A restricted program-pointer shall contain only the predefined address NULL or the address
      *> of a program with the same signature as that identified by the specified program-prototype-name."
      *> cite.py: OK  §13.18.60.4 25)  (General rules)
      *> The ENTRY operand NM is an identifier whose content names the program (a value), so the sender is an
      *> unrestricted program-address however it is spelled. UPP is unrestricted and is written FIRST; RP is
      *> restricted to PB935Y and is the SECOND receiver of the list, so the verdict is asked of every receiver
      *> and never of a representative one (the SR22 reading of kb/Work PB3224, which this form shares). Refused
      *> COBOLNET1959 (prototype-pointer-signature) at every edition that has the program-pointer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB935Y IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(2).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM PB935Y.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB935N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB935Y.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RPT IS TYPEDEF USAGE PROGRAM-POINTER TO PB935Y.
       01 RP TYPE RPT.
       01 UPP USAGE PROGRAM-POINTER.
       01 NM PIC X(8) VALUE "PB935Y".
       PROCEDURE DIVISION.
       MAIN-P.
           SET UPP RP TO ENTRY NM
           STOP RUN.
       END PROGRAM PB935N2.
