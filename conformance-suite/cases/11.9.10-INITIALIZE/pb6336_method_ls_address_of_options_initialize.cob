*>--- conformance-case
*> id: pb6336_method_ls_address_of_options_initialize
*> rules: [GR-11.9.10.4-2, GR-13.6.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6336 - a method LOCAL-STORAGE record whose ADDRESS OF is taken lives in a per-activation cell, and
      *>   that cell starts from the SAME initial state as every other local-storage item of the activation.
      *> §2023:11.9.10.4 GR2: "If LOCAL-STORAGE is specified, all data items in the local-storage section are
      *>   initialized as indicated in the rules for initial state." (cite.py --check OK)
      *> §2023:8.6.4: local-storage "storage is allocated and set to initial state each time the runtime element
      *>   containing them is activated." (cite.py --check OK)
      *> §2023:11.9.4 GR1: a clause applies to the source element and to the source elements it contains "unless
      *>   overridden by a clause in an OPTIONS paragraph in a contained source element" (cite.py --check OK)
      *> Derivation (VALUE-less PIC X(4) items; the ADDRESS OF target R and the plain sibling Q):
      *>   ADRKCLS.M1 has its own INITIALIZE LOCAL-STORAGE TO X"41": both R and Q start AAAA at every activation,
      *>     whatever the first activation stored (the MOVE below makes the second activation prove the re-seed).
      *>   ADRKCLS.M2 has none and inherits the class clause TO X"42": both R and Q start BBBB.
      *>   ADRLCLS has no class clause and its method M4 has no OPTIONS: no background, so the alphanumeric items
      *>     are spaces (no fill leaks between classes).
       IDENTIFICATION DIVISION.
       CLASS-ID. ADRKCLS INHERITS FROM BASE.
       OPTIONS.
           INITIALIZE LOCAL-STORAGE TO X"42".
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       OPTIONS.
           INITIALIZE LOCAL-STORAGE TO X"41".
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R.
          05 F PIC X(4).
       01 Q.
          05 G PIC X(4).
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF R
           DISPLAY "M1 R=[" F "] Q=[" G "]"
           MOVE "zzzz" TO F
           MOVE "yyyy" TO G.
       END METHOD M1.
       METHOD-ID. M2.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R.
          05 F PIC X(4).
       01 Q.
          05 G PIC X(4).
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF R
           DISPLAY "M2 R=[" F "] Q=[" G "]".
       END METHOD M2.
       END OBJECT.
       END CLASS ADRKCLS.
       IDENTIFICATION DIVISION.
       CLASS-ID. ADRLCLS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M4.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R.
          05 F PIC X(4).
       01 Q.
          05 G PIC X(4).
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF R
           DISPLAY "M4 R=[" F "] Q=[" G "]".
       END METHOD M4.
       END OBJECT.
       END CLASS ADRLCLS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6336.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ADRKCLS
           CLASS ADRLCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O1 USAGE OBJECT REFERENCE ADRKCLS.
       01 O2 USAGE OBJECT REFERENCE ADRLCLS.
       PROCEDURE DIVISION.
           INVOKE ADRKCLS "NEW" RETURNING O1
           INVOKE O1 "M1"
           INVOKE O1 "M1"
           INVOKE O1 "M2"
           INVOKE ADRLCLS "NEW" RETURNING O2
           INVOKE O2 "M4"
           STOP RUN.
