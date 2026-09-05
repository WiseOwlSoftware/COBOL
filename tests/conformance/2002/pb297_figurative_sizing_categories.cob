      *> ISO 8.3.3.6.4 GR2 (with NOTE 1 - a figurative constant is
      *> associated with a data item "when ... compared with it") sizes
      *> a figurative to "the number of character positions in the
      *> associated data item", and 8.4.3.3.4 GR5 makes a
      *> reference-modified operand "a unique data item" whose
      *> positions are the SLICE's.  85/pb297_figurative_refmod_relation
      *> pins that over plain alphanumeric operands; THIS program pins
      *> the arms that carry a category or a collating sequence with
      *> them (kb/Work PB297) - each one previously sized the
      *> figurative from the BASE item's PICTURE, or (for a
      *> function-result operand, which has no PICTURE at all) from a
      *> hard-coded 1.
      *>
      *> Every expected value is DERIVED, not measured.
      *>  U01 ALPHABET AL lists Z..A, so 'Z' has the LOWEST ordinal
      *>      position and 8.3.3.6.4 GR7 makes LOW-VALUE 'Z' at
      *>      runtime.  P(1:1) is one position holding 'Z'; GR2 sizes
      *>      the figurative to ONE position -> Y.
      *>  U02 the same over two positions of a four-position item, the
      *>      shape the base-width sizing got wrong: "ZZ" against
      *>      LOW-VALUES sized to 2 -> Y.
      *>  U03 'A' is the LAST character of the literal phrase, not the
      *>      highest of the sequence: 12.3.7.4 GR7 - "Any characters
      *>      of the native collating sequence that are not specified
      *>      in the literal phrase shall assume a position in the
      *>      collating sequence that is greater than that of the
      *>      highest character specified in this literal phrase".  So
      *>      HIGH-VALUE is an unlisted character above 'A', and
      *>      "AA" is LESS than HIGH-VALUES sized to 2 -> Y.
      *>  U04 category NATIONAL: 8.4.3.3.4 GR6 keeps a national slice
      *>      national, and 8.8.4.2.9 compares under the national
      *>      collating sequence (the alphanumeric PROGRAM COLLATING
      *>      SEQUENCE above never applies to it).  NN(1:2) is two
      *>      national 'a'; ALL N"a" sized to 2 national positions is
      *>      the same -> Y.
      *>  U05 the national LOW-VALUE over a slice narrower than the
      *>      item -> Y.
      *>  U06 category BOOLEAN: 8.8.4.2.8 rule 2 right-extends the
      *>      shorter operand with boolean ZEROS, and ZERO is the only
      *>      figurative a boolean operand admits - so the pad and the
      *>      fill coincide and this line is TRUE under any sizing.  It
      *>      is here as the no-regression guard for the boolean arm
      *>      (pad '0'), which is structurally immune to the defect for
      *>      exactly the reason the alphanumeric SPACE case was.
      *>  U07 an INTERMEDIATE RESULT is an associated operand too (GR2
      *>      names "the associated data item, LITERAL, OR INTERMEDIATE
      *>      RESULT").  FUNCTION UPPER-CASE(A) is four positions
      *>      "ABAB"; ALL "AB" repeated to 4 is "ABAB" -> Y.
      *>  U08 the same intermediate result reference-modified
      *>      (8.4.3.3.3 SR2 admits a function-identifier): "ABAB"(2:2)
      *>      is "BA" and ALL "BA" sized to 2 is "BA" -> Y.
      *>  U09 WHICH SEQUENCE a figurative reads when the operand it
      *>      is compared with is itself a figurative.  8.3.3.6.4
      *>      GR1 - "When a figurative constant is used in a context
      *>      requiring national characters, the figurative constant
      *>      represents a national character value" - and GR7 - "If
      *>      the context of the figurative constant requires
      *>      national characters, the national program collating
      *>      sequence is used; otherwise, the alphanumeric program
      *>      collating sequence is used".  8.3.3.6.3 SR2 -
      *>      "Literal-1 shall be an alphanumeric, boolean, or
      *>      national literal" - makes ALL N"Z" a NATIONAL operand,
      *>      so the context is national and LOW-VALUE is the lowest
      *>      NATIONAL position, not this program's alphanumeric one
      *>      ('Z' under ALPHABET AL).  National 'Z' is not the
      *>      national low value -> N.
      *>  U10 THE AGREEMENT THIS PAIR EXISTS TO ASSERT: the same
      *>      comparison written over a national ITEM holding 'Z'
      *>      must answer identically -> N.  Reading the category off
      *>      the anchor SLOT rather than the context made U09 answer
      *>      Y and U10 answer N (kb/Work PB297).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB297G2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX PROGRAM COLLATING SEQUENCE AL.
       SPECIAL-NAMES. ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P  PIC X(4) VALUE "ZZZZ".
       01 Q  PIC X(4) VALUE "AAAA".
       01 NN PIC N(4) VALUE ALL N"a".
       01 NL PIC N(4) VALUE LOW-VALUES.
       01 B  PIC 1(4) USAGE BIT VALUE B"0000".
       01 A  PIC X(4) VALUE "abab".
       01 NZ PIC N(1) VALUE ALL N"Z".
       PROCEDURE DIVISION.
       MAIN.
           IF P(1:1) = LOW-VALUE
              DISPLAY "U01=Y" ELSE DISPLAY "U01=N" END-IF
           IF P(1:2) = LOW-VALUES
              DISPLAY "U02=Y" ELSE DISPLAY "U02=N" END-IF
           IF Q(1:2) < HIGH-VALUES
              DISPLAY "U03=Y" ELSE DISPLAY "U03=N" END-IF
           IF NN(1:2) = ALL N"a"
              DISPLAY "U04=Y" ELSE DISPLAY "U04=N" END-IF
           IF NL(1:2) = LOW-VALUES
              DISPLAY "U05=Y" ELSE DISPLAY "U05=N" END-IF
           IF B(1:2) = ZERO
              DISPLAY "U06=Y" ELSE DISPLAY "U06=N" END-IF
           IF FUNCTION UPPER-CASE(A) = ALL "AB"
              DISPLAY "U07=Y" ELSE DISPLAY "U07=N" END-IF
           IF FUNCTION UPPER-CASE(A)(2:2) = ALL "BA"
              DISPLAY "U08=Y" ELSE DISPLAY "U08=N" END-IF
           IF ALL N"Z" = LOW-VALUE
              DISPLAY "U09=Y" ELSE DISPLAY "U09=N" END-IF
           IF NZ = LOW-VALUE
              DISPLAY "U10=Y" ELSE DISPLAY "U10=N" END-IF
           STOP RUN.
