      *>--- conformance-case
      *> id: g2-inspect-length-and-scan
      *> rules: [2002-GR-14.8.21.3-1, 2002-GR-14.8.21.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.21.3 GR1 - "For the purpose of determining its
      *> length, identifier-1 is treated as if it were a sending data
      *> item. If the data item referenced by identifier-1 is a
      *> zero-length item, execution of the INSPECT statement terminates
      *> immediately."
      *> §2002:14.8.21.3 GR2 - inspection "begins at the leftmost
      *> character position of the data item referenced by
      *> identifier-1, regardless of its class, and proceeds from left
      *> to right to the rightmost character position" (2002 has no
      *> BACKWARD phrase). Every cite.py --check here is OK.
      *> DERIVATION.
      *>  GR1, zero length. G's ODO object N lies outside G, so at N=0
      *>   G "is a zero-length item" (§2002:13.16.36.3 GR8a). A format 3
      *>   TALLYING/REPLACING CHARACTERS over G terminates at once: T
      *>   stays at its preset 05 and nothing is replaced  -> Z1=05 []
      *>  GR1 control: N=2, G="AB": the same statement now tallies 2
      *>   characters (GR11c) and replaces both (GR16a)  -> Z2=07 [ZZ]
      *>  GR1, sending length. H's ODO object HC lies inside H; a
      *>   sending operand uses only the current extent, a receiving
      *>   one "the maximum length of the group" (§2002:13.16.36.3
      *>   GR8b). The leg is format 3, where identifier-1 is also a
      *>   receiving operand (SR8 makes it sending in format 1 only),
      *>   so only GR1 gives the sending length. Storage "5AAAAA",
      *>   then HC=2: as a sending item H is "2AA", so ALL "A" tallies
      *>   2 and replaces those 2 (GR18), and HE(3..5) stay "A"; shown
      *>   after HC=5 again. The receiving maximum "2AAAAA" would give
      *>   [5BBBBB]                                -> S1=02 [5BBAAA]
      *>  GR2, BEFORE boundary from the left: "A12C21D12EF" CHARACTERS
      *>   BEFORE INITIAL "12": the first "12" scanning left to right
      *>   is at 2-3 (§2002:14.8.21.3 GR8b "from its leftmost character
      *>   position up to, but not including, the first occurrence of
      *>   literal-2"), so one character (a right-to-left scan would
      *>   find the "12" at 8-9 first and count 2)          -> L1=01
      *>  GR2, FIRST from the left: "AAAA" REPLACING FIRST "AA" BY "XY"
      *>   (GR16d the leftmost occurrence)                  -> L2=[XYAA]
      *>  GR2 with AFTER, format 3 (GR18 tally then replace): "ABABA"
      *>   TALLYING ALL "AB": matches at 1-2 and 3-4 (GR7c) -> 02;
      *>   REPLACING FIRST "A" BY "Q" AFTER INITIAL "B": the first "B"
      *>   is at 2, region 3-5 (§2002:14.8.21.3 GR8), leftmost "A" in
      *>   it is at 3                                 -> L3=02 [ABQBA]
      *>  GR2, LEADING from the left: "AABA" TALLYING LEADING "A": the
      *>   leading run starts at position 1                 -> L4=02
      *> A leg fails if a zero-length item is inspected, if the length
      *> of H is taken as a receiving item's, or if any scan starts
      *> anywhere but the leftmost position.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9 VALUE 0.
       01  T  PIC 99 VALUE 5.
       01  G.
           05 E PIC X OCCURS 0 TO 5 TIMES DEPENDING ON N.
       01  H.
           05 HC PIC 9.
           05 HE PIC X OCCURS 1 TO 5 TIMES DEPENDING ON HC.
       01  I  PIC 9.
       01  S1 PIC X(11) VALUE "A12C21D12EF".
       01  S2 PIC X(4) VALUE "AAAA".
       01  S3 PIC X(5) VALUE "ABABA".
       01  S4 PIC X(4) VALUE "AABA".
       01  T1 PIC 99 VALUE 0.
       01  T3 PIC 99 VALUE 0.
       01  T4 PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT G TALLYING T FOR CHARACTERS
                   REPLACING CHARACTERS BY "Z".
           DISPLAY "Z1=" T " [" G "]".
           MOVE 2 TO N.
           MOVE "AB" TO G.
           INSPECT G TALLYING T FOR CHARACTERS
                   REPLACING CHARACTERS BY "Z".
           DISPLAY "Z2=" T " [" G "]".
           MOVE 5 TO HC.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
               MOVE "A" TO HE(I)
           END-PERFORM.
           MOVE 2 TO HC.
           MOVE 0 TO T.
           INSPECT H TALLYING T FOR ALL "A"
                   REPLACING ALL "A" BY "B".
           MOVE 5 TO HC.
           DISPLAY "S1=" T " [" H "]".
           INSPECT S1 TALLYING T1 FOR CHARACTERS BEFORE INITIAL "12".
           DISPLAY "L1=" T1.
           INSPECT S2 REPLACING FIRST "AA" BY "XY".
           DISPLAY "L2=[" S2 "]".
           INSPECT S3 TALLYING T3 FOR ALL "AB"
                   REPLACING FIRST "A" BY "Q" AFTER INITIAL "B".
           DISPLAY "L3=" T3 " [" S3 "]".
           INSPECT S4 TALLYING T4 FOR LEADING "A".
           DISPLAY "L4=" T4.
           STOP RUN.
