*>--- conformance-case
*> id: pb3224-set-program-address-unrestricted-first-prototype
*> rules: [SR-14.9.39.3-22]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 SR22 over a receiving LIST (kb/Work PB3224). identifier-7 is written `{ identifier-7 } ...`
      *> and the rule's condition is EACH receiver's own restriction:
      *>   "If identifier-7 references a restricted program-pointer, identifier-8 shall be the predefined address
      *>   NULL or shall reference a program-pointer and the program-prototypes associated with identifier-7 and
      *>   identifier-8 shall have the same signature."   cite.py: OK  §14.9.39.3 22)  (Syntax rules)
      *> RP is restricted to PB3224X (one USING formal); UPP is unrestricted and is written FIRST.
      *> The sender ADDRESS OF PROGRAM PB3224Y is restricted to PB3224Y (two USING formals, GR3), whose
      *> signature differs from RP's.
      *> Every other rule is met: both prototypes precede the program and are named in its REPOSITORY
      *> (§8.4.3.13.3 SR3), RPT is a TYPEDEF (§13.18.60.3 SR19). The same statement with RP written first was always
      *> refused, so the verdict must not depend on the order: COBOLNET1959 (prototype-pointer-signature) at
      *> every edition that has ADDRESS OF PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3224X IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(2).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM PB3224X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3224Y IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(2).
       01 L-Y PIC X(2).
       PROCEDURE DIVISION USING L-X L-Y.
       END PROGRAM PB3224Y.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3224N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB3224X
           PROGRAM PB3224Y.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RPT IS TYPEDEF USAGE PROGRAM-POINTER TO PB3224X.
       01 RP TYPE RPT.
       01 UPP USAGE PROGRAM-POINTER.
       01 NM PIC X(8) VALUE "PB3224Y".
       PROCEDURE DIVISION.
       MAIN-P.
           SET UPP RP TO ADDRESS OF PROGRAM PB3224Y
           STOP RUN.
       END PROGRAM PB3224N1.
