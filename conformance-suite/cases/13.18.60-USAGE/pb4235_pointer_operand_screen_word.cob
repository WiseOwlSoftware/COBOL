*>--- conformance-case
*> id: pb4235_pointer_operand_screen_word
*> rules: [FMT-13.18.60.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §13.18.60.2 with §8.10 and §8.3.2.1 3) — a TO-less pointer operand spelled with a
      *> SCREEN attribute word is the operand in a data description entry. kb/Work PB4235.
      *>
      *> §13.18.60.2 prints `POINTER [ TO type-name-1 ]` and `PROGRAM-POINTER [ TO program-prototype-name-1 ]`
      *> with TO not underlined (folio 503, rendered), so by §5.2.3 TO is an optional word that "may be written
      *> to add clarity". §8.10 lists AUTO, BELL, SECURE (and ten more) as context-sensitive words whose
      *> construct is the "screen description entry", and §8.3.2.1 3): "Context-sensitive words may be used as
      *> user-defined words and system-names in contexts other than the language construct in which they are
      *> defined". In WORKING-STORAGE the words below are therefore type-names and a program-prototype-name, and
      *> each TO-less entry declares EXACTLY the restriction its TO-full twin declares:
      *> §13.18.60.4 GR23 — "If type-name-1 is specified, this data item is a restricted data-pointer."
      *> §13.18.60.4 GR25 — "If program-prototype-name-1 is specified, this data item is a restricted
      *>   program-pointer."  §13.18.60.3 SR18 — the TYPEDEF clause is specified for the subject.
      *> The observable proof is assignment between the twins: §14.9.39.3 SR19 ("identifier-6 shall be the
      *> predefined address NULL or shall reference a data-pointer restricted to the same type") and SR22 (the
      *> program-pointer twin) ADMIT each copy only because both declarations name the same type / prototype.
      *> WHY EACH LEG CAN FAIL: a compiler that reads AUTO / SECURE / BELL as the screen clause keyword refuses the
      *> entry (the PB4235 defect: COBOL1941 "'AUTO' is not a clause of the data description entry"); one that
      *> drops the operand makes the first pointer unrestricted and the SET a compile-time error.
      *> COBOL-2002: POINTER, PROGRAM-POINTER, TYPEDEF and program prototypes are 2002 constructs.
      *>
      *> EXPECTED OUTPUT, derived line by line:
      *>   AUTO-SAME    DA1 (POINTER AUTO) took W-A's address; DA2 (POINTER TO AUTO) copied it (SR19); pointers
      *>                that address the same item are equal (§8.8.4.2.16).
      *>   SECURE-SAME  DS1 (POINTER SECURE) and DS2 (POINTER TO SECURE) likewise, over W-S.
      *>   BELL-SAME    PB1 (PROGRAM-POINTER BELL) took BELL's address; PB2 (… TO BELL) copied it (SR22).
      *>   CALLED 0042  CALL through PB2 activates BELL, which displays its argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BELL IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM BELL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4235PO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM BELL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AUTO IS TYPEDEF STRONG.
          05 A PIC X(2).
       01 SECURE IS TYPEDEF STRONG.
          05 S PIC 9(3).
       01 DAT1 IS TYPEDEF USAGE POINTER AUTO.
       01 DAT2 IS TYPEDEF USAGE POINTER TO AUTO.
       01 DST1 IS TYPEDEF USAGE POINTER SECURE.
       01 DST2 IS TYPEDEF USAGE POINTER TO SECURE.
       01 PBT1 IS TYPEDEF USAGE PROGRAM-POINTER BELL.
       01 PBT2 IS TYPEDEF USAGE PROGRAM-POINTER TO BELL.
       01 DA1 TYPE DAT1.
       01 DA2 TYPE DAT2.
       01 DS1 TYPE DST1.
       01 DS2 TYPE DST2.
       01 PB1 TYPE PBT1.
       01 PB2 TYPE PBT2.
       01 W-A TYPE AUTO.
       01 W-S TYPE SECURE.
       01 WS-N PIC 9(4) VALUE 0042.
       PROCEDURE DIVISION.
       MAIN.
           SET DA1 TO ADDRESS OF W-A
           SET DA2 TO DA1
           IF DA2 = DA1
               DISPLAY "AUTO-SAME"
           ELSE
               DISPLAY "AUTO-DIFF"
           END-IF
           SET DS1 TO ADDRESS OF W-S
           SET DS2 TO DS1
           IF DS2 = DS1
               DISPLAY "SECURE-SAME"
           ELSE
               DISPLAY "SECURE-DIFF"
           END-IF
           SET PB1 TO ADDRESS OF PROGRAM BELL
           SET PB2 TO PB1
           IF PB2 = PB1
               DISPLAY "BELL-SAME"
           ELSE
               DISPLAY "BELL-DIFF"
           END-IF
           CALL PB2 USING WS-N
           STOP RUN.
       END PROGRAM PB4235PO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BELL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
           DISPLAY "CALLED " L-X
           GOBACK.
       END PROGRAM BELL.
