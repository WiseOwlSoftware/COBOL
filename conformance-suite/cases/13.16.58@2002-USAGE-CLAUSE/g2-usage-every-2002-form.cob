      *>--- conformance-case
      *> id: g2-usage-every-2002-form
      *> rules: [2002-FMT-13.16.58.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.1 general format - [USAGE IS] BINARY,
      *> BINARY-CHAR / -SHORT / -LONG / -DOUBLE [SIGNED | UNSIGNED], BIT,
      *> COMPUTATIONAL, COMP, DISPLAY, FLOAT-SHORT / -LONG / -EXTENDED,
      *> INDEX, NATIONAL, OBJECT REFERENCE [interface-name-1 | [FACTORY
      *> OF] ACTIVE-CLASS | [FACTORY OF] class-name-1 [ONLY]],
      *> PACKED-DECIMAL, POINTER [TO type-name-1], PROGRAM-POINTER [TO
      *> program-prototype-name-1]. Every form is written, with and
      *> without the optional USAGE IS, and each item is used:
      *>  - numeric usages hold their VALUE (within the BINARY-* minimum
      *>    ranges of §2002:13.16.58.3 GR11; 1.5, 2.5, 3.5 are exact in
      *>    any binary float) and compare equal to it;
      *>  - BIT holds B"1010", DISPLAY "DSP", NATIONAL N"NN";
      *>  - INDEX: an index-name's value is saved in U18 and restored, so
      *>    TE(IX) is the fourth element "D";
      *>  - object references and pointers start null ("data items of
      *>    class object and class pointer are initialized to null",
      *>    §2002:13.16.61.3 GR4);
      *>  - POINTER TO and PROGRAM-POINTER TO are written on TYPEDEF
      *>    entries, as §2002:13.16.58.2 SR17 and SR18 require; ADDRESS OF
      *>    the strongly-typed W28 is restricted to T28 (§2002:8.4.2.11.3
      *>    GR2), so it may be SET into U28 (§2002:14.8.35.2 SR19);
      *>  - a G2USGC object is created and reached through the interface
      *>    reference U21 (HELLO) and the class reference U22, whose method
      *>    MK holds ACTIVE-CLASS and FACTORY OF ACTIVE-CLASS references.
      *> The program prototype G2USGPR comes first in the compilation
      *> group (§2002:10.5.2 SR1 "function-prototypes and
      *> program-prototypes shall precede all other types of source
      *> units").
      *> DERIVATION: each IF prints its OK line; "INDEX D"; "HELLO" from
      *> the interface invocation; "ACTIVE-CLASS OK" (R1 SET TO SELF is
      *> not null, R2 untouched is null). A leg fails if any 2002 form is
      *> refused or an item does not hold or compare to its value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USGPR IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L1 PIC X.
       PROCEDURE DIVISION USING L1.
       END PROGRAM G2USGPR.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. G2USGI.
       PROCEDURE DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
       END METHOD HELLO.
       END INTERFACE G2USGI.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USG07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2USGC
           INTERFACE G2USGI
           PROGRAM G2USGPR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U01 PIC S9(4) USAGE IS BINARY VALUE -7.
       01  U02 USAGE IS BINARY-CHAR VALUE -5.
       01  U03 USAGE BINARY-CHAR SIGNED VALUE -6.
       01  U04 BINARY-CHAR UNSIGNED VALUE 250.
       01  U05 USAGE IS BINARY-SHORT SIGNED VALUE -300.
       01  U06 BINARY-SHORT UNSIGNED VALUE 60000.
       01  U07 USAGE BINARY-LONG VALUE -70000.
       01  U08 BINARY-LONG UNSIGNED VALUE 4000000000.
       01  U09 USAGE IS BINARY-DOUBLE SIGNED VALUE -9.
       01  U10 BINARY-DOUBLE UNSIGNED VALUE 9.
       01  U11 PIC 1(4) USAGE IS BIT VALUE B"1010".
       01  U12 PIC S9(4) USAGE IS COMPUTATIONAL VALUE 12.
       01  U13 PIC S9(4) COMP VALUE 13.
       01  U14 PIC X(3) USAGE IS DISPLAY VALUE "DSP".
       01  U15 USAGE IS FLOAT-SHORT VALUE 1.5.
       01  U16 FLOAT-LONG VALUE 2.5.
       01  U17 USAGE FLOAT-EXTENDED VALUE 3.5.
       01  U18 USAGE IS INDEX.
       01  U19 PIC N(2) USAGE IS NATIONAL VALUE N"NN".
       01  U20 USAGE IS OBJECT REFERENCE.
       01  U21 USAGE OBJECT REFERENCE G2USGI.
       01  U22 OBJECT REFERENCE G2USGC.
       01  U23 USAGE OBJECT REFERENCE FACTORY OF G2USGC.
       01  U24 USAGE OBJECT REFERENCE G2USGC ONLY.
       01  U25 USAGE OBJECT REFERENCE FACTORY OF G2USGC ONLY.
       01  U26 PIC S9(5) USAGE IS PACKED-DECIMAL VALUE -26.
       01  U27 USAGE IS POINTER.
       01  T28 TYPEDEF STRONG.
           05  T28A PIC X(4) VALUE "W28!".
       01  W28 TYPE T28.
       01  P28 TYPEDEF USAGE POINTER TO T28.
       01  U28 TYPE P28.
       01  U29 USAGE IS PROGRAM-POINTER.
       01  P30 TYPEDEF PROGRAM-POINTER TO G2USGPR.
       01  U30 TYPE P30.
       01  TBL.
           05  TE PIC X OCCURS 5 INDEXED BY IX.
       PROCEDURE DIVISION.
           IF U01 = -7 AND U02 = -5 AND U03 = -6 AND U04 = 250
              AND U05 = -300 AND U06 = 60000 AND U07 = -70000
              AND U08 = 4000000000 AND U09 = -9 AND U10 = 9
               DISPLAY "BINARY OK"
           ELSE
               DISPLAY "BINARY BAD"
           END-IF.
           IF U11 = B"1010" DISPLAY "BIT OK" ELSE DISPLAY "BIT BAD".
           IF U12 = 12 AND U13 = 13 AND U14 = "DSP" AND U26 = -26
               DISPLAY "COMP DISPLAY PACKED OK"
           ELSE
               DISPLAY "COMP DISPLAY PACKED BAD"
           END-IF.
           IF U15 = 1.5 AND U16 = 2.5 AND U17 = 3.5
               DISPLAY "FLOAT OK"
           ELSE
               DISPLAY "FLOAT BAD"
           END-IF.
           MOVE "ABCDE" TO TBL.
           SET IX TO 4.
           SET U18 TO IX.
           SET IX TO 1.
           SET IX TO U18.
           DISPLAY "INDEX " TE(IX).
           IF U19 = N"NN" DISPLAY "NATIONAL OK" ELSE
               DISPLAY "NATIONAL BAD".
           IF U20 = NULL AND U21 = NULL AND U22 = NULL AND U23 = NULL
              AND U24 = NULL AND U25 = NULL
               DISPLAY "OBJREFS NULL"
           ELSE
               DISPLAY "OBJREFS BAD"
           END-IF.
           IF U27 = NULL AND U28 = NULL AND U29 = NULL AND U30 = NULL
               DISPLAY "POINTERS NULL"
           ELSE
               DISPLAY "POINTERS BAD"
           END-IF.
           SET U28 TO ADDRESS OF W28.
           SET U27 TO ADDRESS OF U14.
           IF U28 = ADDRESS OF W28 AND U27 NOT = NULL
               DISPLAY "POINTER TO OK"
               ELSE DISPLAY "POINTER TO BAD".
           INVOKE G2USGC "NEW" RETURNING U22.
           SET U21 TO U22.
           SET U20 TO U22.
           INVOKE U21 "HELLO".
           INVOKE U22 "MK".
           STOP RUN.
       END PROGRAM G2USG07.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2USGC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE G2USGI.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS G2USGI.
       PROCEDURE DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
           DISPLAY "HELLO".
       END METHOD HELLO.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01  R1 USAGE OBJECT REFERENCE ACTIVE-CLASS.
       01  R2 USAGE OBJECT REFERENCE FACTORY OF ACTIVE-CLASS.
       PROCEDURE DIVISION.
           SET R1 TO SELF.
           IF R1 NOT = NULL AND R2 = NULL
               DISPLAY "ACTIVE-CLASS OK"
           END-IF.
       END METHOD MK.
       END OBJECT.
       END CLASS G2USGC.
