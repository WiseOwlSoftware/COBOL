      *>--- conformance-case
      *> id: g2-replace-debugging-line-mode
      *> rules: [2002-SR-7.1.3.2-4, 2002-SR-7.1.3.2-10,
      *>     2002-SR-7.1.3.2-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> WITH DEBUGGING MODE: §2002:12.2.4.3 GR4 "all debugging lines
      *> are compiled as if the debugging indicator was replaced by
      *> three spaces".
      *> L1 §2002:7.1.3.2 SR4 - "Pseudo-text-2 shall contain zero, one,
      *> or more text-words": the text-words of pseudo-text-2 are on a
      *> D line (§2002:7.1.3.2 SR10). With DEBUGGING MODE the inserted
      *> text-words compile whatever line they land on (§2002:12.2.4.3
      *> GR4 on a debugging line; an ordinary line compiles anyway), so
      *> DISPLAY EEE becomes DISPLAY FFF: NEW3 (OLD3 if unreplaced,
      *> nothing if the D line is lost). Whether REPLACE keeps them on a
      *> debugging line is not decided by the 2002 text (§2002:H.2 1));
      *> this leg holds under either reading.
      *> L2 §2002:7.1.3.2 SR11 - "Compiler directive lines shall not be
      *> specified within pseudo-text-1, pseudo-text-2, partial-word-1,
      *> or partial-word-2". A >>D line is not a compiler directive
      *> line: §2002:6.1.2 makes >>D "a debugging line when followed by
      *> at least one space character", >> a compiler-directive line
      *> only "when followed by a compiler-directive word". So the >>D
      *> line in pseudo-text-2 is permitted (SR10): XYZ becomes
      *> DISPLAY "DBG", compiled: DBG (a refusal if >>D is taken for a
      *> directive line). The refusal leg is
      *> g2-replace-pseudo-text-directive-line-refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPL02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       OBJECT-COMPUTER. COMPUTER-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EEE PIC X(4) VALUE "OLD3".
       01 FFF PIC X(4) VALUE "NEW3".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "MAIN".
           REPLACE ==DISPLAY EEE== BY ==
      D    DISPLAY FFF
           ==.
           DISPLAY EEE
           DISPLAY "L1".
           REPLACE ==XYZ== BY ==
           >>D DISPLAY "DBG"
           ==.
           XYZ
           DISPLAY "END".
           STOP RUN.
