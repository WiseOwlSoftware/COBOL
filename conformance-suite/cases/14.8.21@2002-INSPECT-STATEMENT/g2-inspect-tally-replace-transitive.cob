      *>--- conformance-case
      *> id: g2-inspect-tally-replace-transitive
      *> rules: [2002-SR-14.8.21.2-8, 2002-GR-14.8.21.3-6,
      *>    2002-GR-14.8.21.3-10, 2002-GR-14.8.21.3-15]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.21.2 SR8 (FORMAT 1) - "Identifier-1 is a sending
      *>   operand." So a sending-only identifier, a function-identifier
      *>   (§2002:8.4.2.2.2 SR1 "A function-identifier shall not be
      *>   specified as a receiving operand."), is a format 1
      *>   identifier-1: lines F1, F2, D4.
      *> §2002:14.8.21.3 GR6 - "each properly matched occurrence of
      *>   literal-1 is tallied (format 1) or replaced by literal-3
      *>   (format 2)": lines P1, P2, P3, D1, D2.
      *> §2002:14.8.21.3 GR10 - "The content of the data item referenced
      *>   by identifier-2 is not initialized by the execution of the
      *>   INSPECT statement.": lines F2, C1, C2, D1, D3.
      *> §2002:14.8.21.3 GR15 - "The ALL, FIRST, and LEADING phrases are
      *>   transitive across the operands that follow them until
      *>   another ALL, FIRST, or LEADING phrase is encountered.":
      *>   lines C2, T1, T2, T3, D3.
      *> The D lines put identifier-1, identifier-2, identifier-3 and
      *> identifier-5 in the record of a 2002 output CD (the
      *> communication facility is obsolete at 2002 and draws a
      *> warning; no INSPECT rule names a section). Every cite.py
      *> --check here is OK.
      *> DERIVATION.
      *>  F1 UPPER-CASE("abbcb") = "ABBCB": ALL "B" (GR11a) adds 3 to 0;
      *>     S itself is untouched (sending)          -> F1=03 [abbcb]
      *>  F2 same identifier-1, ALL "A", counter NOT reset: 3 + 1
      *>                                              -> F2=04
      *>  P1 "ABCABCAB" ALL "AB": matches at 1, 4, 7  -> P1=03
      *>  P2 same, REPLACING ALL "AB" BY "xy"         -> P2=[xyCxyCxy]
      *>  P3 "AAA" ALL "AA": match at 1-2, scan resumes at 3 (GR7c),
      *>     one character is left, no second match   -> P3=01
      *>  C1 counter preset 05, "AAB" ALL "A" adds 2  -> C1=07
      *>  C2 format 3 (GR18), counter preset 10, "ABABAB" TALLYING ALL
      *>     "B" adds 3; REPLACING FIRST "A" BY "Q" "B" BY "R": FIRST
      *>     carries to "B", so only the leftmost "A" and the leftmost
      *>     "B" change (GR16d)                       -> C2=13 [QRABAB]
      *>  T1 "ABAB" FIRST "A" BY "X" "B" BY "Y": "B" is FIRST too
      *>     (an ALL "B" would give XYAY)             -> T1=[XYAB]
      *>  T2 "ABCABC" FIRST "A" BY "X" ALL "B" BY "Y" "C" BY "Z": ALL
      *>     ends FIRST's reach, "C" takes ALL        -> T2=[XYZAYZ]
      *>  T3 "AABA" LEADING "A" BY "X" "B" BY "Y": "B" is LEADING too;
      *>     its occurrence at 3 is not at the point where comparison
      *>     began (GR16c), so it stays (ALL would give XXYA)
      *>                                              -> T3=[XXBA]
      *>  D1 CD record: R1="AABAAC", RN preset 01, ALL "A" adds 4
      *>                                              -> D1=05
      *>  D2 R1 REPLACING ALL RP ("AB") BY RQ ("xy"): "AA" at 1 does
      *>     not match, "AB" at 2-3 does, then "AA", "AC", "C" do not
      *>                                              -> D2=[AxyAAC]
      *>  D3 R1="ABABAB", RN preset 10, as C2         -> D3=13 [QRABAB]
      *>  D4 R1="aqbqcq", RN=0, INSPECT FUNCTION UPPER-CASE(R1)
      *>     TALLYING RN FOR ALL "Q"                  -> D4=03 [aqbqcq]
      *> A leg fails if a function-identifier is refused as format 1
      *> identifier-1 or is written to, if a match is missed or counted
      *> twice, if a counter is cleared, or if an adjective does not
      *> carry to the operands that follow it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S  PIC X(5) VALUE "abbcb".
       01  T  PIC 99 VALUE 0.
       01  N5 PIC 99 VALUE 5.
       01  N9 PIC 99 VALUE 10.
       01  X2 PIC X(8) VALUE "ABCABCAB".
       01  X3 PIC X(3) VALUE "AAA".
       01  X4 PIC X(3) VALUE "AAB".
       01  X5 PIC X(4).
       01  X6 PIC X(6).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL.
       01  OUTREC.
           05 R1 PIC X(6).
           05 RN PIC 99.
           05 RP PIC X(2).
           05 RQ PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT FUNCTION UPPER-CASE(S) TALLYING T FOR ALL "B".
           DISPLAY "F1=" T " [" S "]".
           INSPECT FUNCTION UPPER-CASE(S) TALLYING T FOR ALL "A".
           DISPLAY "F2=" T.
           MOVE 0 TO T.
           INSPECT X2 TALLYING T FOR ALL "AB".
           DISPLAY "P1=" T.
           INSPECT X2 REPLACING ALL "AB" BY "xy".
           DISPLAY "P2=[" X2 "]".
           MOVE 0 TO T.
           INSPECT X3 TALLYING T FOR ALL "AA".
           DISPLAY "P3=" T.
           INSPECT X4 TALLYING N5 FOR ALL "A".
           DISPLAY "C1=" N5.
           MOVE "ABABAB" TO X6.
           INSPECT X6 TALLYING N9 FOR ALL "B"
                   REPLACING FIRST "A" BY "Q" "B" BY "R".
           DISPLAY "C2=" N9 " [" X6 "]".
           MOVE "ABAB" TO X5.
           INSPECT X5 REPLACING FIRST "A" BY "X" "B" BY "Y".
           DISPLAY "T1=[" X5 "]".
           MOVE "ABCABC" TO X6.
           INSPECT X6 REPLACING FIRST "A" BY "X"
                                ALL "B" BY "Y" "C" BY "Z".
           DISPLAY "T2=[" X6 "]".
           MOVE "AABA" TO X5.
           INSPECT X5 REPLACING LEADING "A" BY "X" "B" BY "Y".
           DISPLAY "T3=[" X5 "]".
           MOVE "AABAAC" TO R1.
           MOVE 1 TO RN.
           INSPECT R1 TALLYING RN FOR ALL "A".
           DISPLAY "D1=" RN.
           MOVE "AB" TO RP.
           MOVE "xy" TO RQ.
           INSPECT R1 REPLACING ALL RP BY RQ.
           DISPLAY "D2=[" R1 "]".
           MOVE "ABABAB" TO R1.
           MOVE 10 TO RN.
           INSPECT R1 TALLYING RN FOR ALL "B"
                   REPLACING FIRST "A" BY "Q" "B" BY "R".
           DISPLAY "D3=" RN " [" R1 "]".
           MOVE "aqbqcq" TO R1.
           MOVE 0 TO RN.
           INSPECT FUNCTION UPPER-CASE(R1) TALLYING RN FOR ALL "Q".
           DISPLAY "D4=" RN " [" R1 "]".
           STOP RUN.
