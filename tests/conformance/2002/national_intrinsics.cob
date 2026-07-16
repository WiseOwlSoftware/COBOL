      *> ISO §15.26 DISPLAY-OF / §15.66 NATIONAL-OF — the sanctioned national<->alphanumeric
      *> repertoire conversions (COBOL-2002 intrinsics; P10 national wave). The implementor-defined
      *> correspondence is the D-N4 Latin-1 identity: national is UTF-16 one char per position (D-N1),
      *> the alphanumeric coded set is Latin-1, so ANUM->NAT (§15.66.4 r1) always corresponds and
      *> NAT->ANUM (§15.26.4 r1) corresponds for code points <= U+00FF. A wider national character
      *> takes the substitution character: argument-2 when specified (§15.26.4 r2 — no exception),
      *> else the implementor-defined "?" + EC-DATA-CONVERSION (§15.26.4 r3 — nonfatal Table 13;
      *> checking off here, so the substitution stands and execution continues). FUNCTION LENGTH
      *> over both results proves the §15.26.4 r4 / §15.66.4 r4 lengths (character positions).
      *> The wide character below is U+4E16 (CJK, caseless) — the source file is UTF-8.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NATINTRP10N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-SRC    PIC N(5) VALUE N"HELLO".
       01 N-WIDE   PIC N(3).
       01 N-DEST   PIC N(4).
       01 N-CMP    PIC N(2).
       01 A-SRC    PIC X(3) VALUE "XYZ".
       01 A-DEST   PIC X(6).
       01 LEN-R    PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
      *> DISPLAY-OF format 1: national item -> alphanumeric (§15.26.4 r1, identity repertoire).
           MOVE FUNCTION DISPLAY-OF(N-SRC) TO A-DEST.
           DISPLAY "DOF=" A-DEST.
      *> NATIONAL-OF format 1: alphanumeric item -> national (§15.66.4 r1, widening).
           MOVE FUNCTION NATIONAL-OF(A-SRC) TO N-DEST.
           DISPLAY "NOF=" N-DEST.
      *> Round trip over a literal, nested calls (§8.4.3.2 — a function is an argument shape).
           MOVE FUNCTION DISPLAY-OF(FUNCTION NATIONAL-OF("AB")) TO A-DEST.
           DISPLAY "RT=" A-DEST.
      *> The NATIONAL-OF result IS category national (§15.66.1): compares equal to an N"" literal.
           MOVE FUNCTION NATIONAL-OF("AB") TO N-CMP.
           IF N-CMP = N"AB" THEN DISPLAY "EQ=YES" ELSE DISPLAY "EQ=NO".
      *> An out-of-repertoire national character (U+4E16 > U+00FF has no alphanumeric
      *> correspondent under D-N4).
           MOVE N"A世B" TO N-WIDE.
      *> Argument-2 unspecified: implementor substitution "?" + EC-DATA-CONVERSION (§15.26.4 r3).
           MOVE FUNCTION DISPLAY-OF(N-WIDE) TO A-DEST.
           DISPLAY "SUB1=" A-DEST.
      *> Argument-2 specified: the explicit substitution character, no exception (§15.26.4 r2).
           MOVE FUNCTION DISPLAY-OF(N-WIDE, "#") TO A-DEST.
           DISPLAY "SUB2=" A-DEST.
      *> Result lengths in character positions (§15.50; §15.26.4 r4 / §15.66.4 r4).
           MOVE FUNCTION LENGTH(FUNCTION DISPLAY-OF(N-SRC)) TO LEN-R.
           DISPLAY "LEN-D=" LEN-R.
           MOVE FUNCTION LENGTH(FUNCTION NATIONAL-OF(A-SRC)) TO LEN-R.
           DISPLAY "LEN-N=" LEN-R.
           STOP RUN.
