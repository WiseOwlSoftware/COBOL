*>--- conformance-case
*> id: pb4107-call-byref-pointer-two-types
*> rules: [GR-14.8.2.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4107 - a restricted program-pointer passed BY REFERENCE needs a formal OF THE SAME TYPE, and two
      *> type-names that restrict alike are two types.
      *> 14.8.2.3.2: "If either is a restricted pointer, both shall be restricted and of the same type."
      *>   cite.py: OK  14.8.2.3.2 4)
      *> 8.5.3.1: "Two typed items are of the same type when:" "The items are described with TYPE clauses that reference
      *>   equivalent type declarations"   cite.py: OK  8.5.3.1 - and equivalent declarations have the same type-name.
      *> 14.9.4.3 SR25: "The rules for conformance specified in 14.8.2, Parameters and 14.8.3, Returning items apply."
      *>   cite.py: OK  14.9.4.3 25)
      *> S4107N is specified earlier in the group, so its formal is known at compile time (12.3.8.4 GR10 a)) and the CALL
      *> is refused there: PC is of type TC, the formal F of type TB, both TO P4107N (COBOL1688). A SET PB TO PC, or the
      *> same CALL BY CONTENT, is legal (14.9.39.3 SR22, one prototype): the positive golden
      *> pb4107_restricted_pointer_identity_and_signature.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4107N IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM P4107N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S4107N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P4107N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB TYPEDEF USAGE PROGRAM-POINTER TO P4107N.
       LINKAGE SECTION.
       01 F TYPE TB.
       PROCEDURE DIVISION USING F.
           GOBACK.
       END PROGRAM S4107N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T4107N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P4107N
           PROGRAM S4107N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TC TYPEDEF USAGE PROGRAM-POINTER TO P4107N.
       01 PC TYPE TC.
       PROCEDURE DIVISION.
           SET PC TO NULL
           CALL S4107N USING BY REFERENCE PC
           GOBACK.
       END PROGRAM T4107N.
