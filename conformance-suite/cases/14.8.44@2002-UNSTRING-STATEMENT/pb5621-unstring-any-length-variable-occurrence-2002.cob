*>--- conformance-case
*> id: pb5621-unstring-any-length-variable-occurrence-2002
*> rules: [2002-GR-14.8.44.3-2, 2002-GR-14.8.44.3-9]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5621 - an ANY LENGTH formal takes the length of its argument (§2002:13.16.2.3 GR1: "a zero-length item
      *> when the corresponding argument or returning item of the activating runtime element is a zero-length item",
      *> otherwise "n repetitions of the picture symbol ... where n is the length of the corresponding argument"), and
      *> the length of an OCCURS DEPENDING group argument is fixed by §2002:14.7.1.1: "For an argument or formal
      *> parameter that is described as a variable-occurrence data item passed by reference, the maximum length is
      *> used. For a variable-occurrence data item passed by content, the length of the argument is determined by the
      *> rules of the OCCURS clause for a sending data item", the current one. So the formal is zero-length for the
      *> BY CONTENT group while its DEPENDING ON item is 0, 3 characters for N = 3, and 5 (the maximum) BY REFERENCE.
      *> A zero-length formal as identifier-1 of UNSTRING ends the statement at once (§2002:14.8.44.3 GR2: "If the data
      *> item referenced by identifier-1 is a zero-length item, execution of the UNSTRING statement terminates
      *> immediately"): the receivers and the pointer keep their values and the TALLYING item is not touched. As
      *> identifier-2 of DELIMITED BY it is ignored (§2002:14.8.44.3 GR9: "If the data item referenced by identifier-2
      *> or identifier-3 is a zero-length item, that delimiter is ignored"), so the ";" of OR alone delimits.
      *> Why each leg can fail: a formal that took the maximum length for the BY CONTENT group would examine five
      *> spaces (P=06, T=1 in the first leg) and would be a five-space delimiter in the second.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5621U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N  PIC 9 VALUE 0.
       01 WS-D.
          02 WS-DE PIC X OCCURS 0 TO 5 DEPENDING ON N.
       PROCEDURE DIVISION.
           DISPLAY "CONTENT, N=0"
           CALL "PB5621I" USING BY CONTENT WS-D
           MOVE 3 TO N
           MOVE "Q;R" TO WS-D
           DISPLAY "CONTENT, N=3"
           CALL "PB5621I" USING BY CONTENT WS-D
           DISPLAY "REFERENCE, N=3"
           CALL "PB5621I" USING BY REFERENCE WS-D
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5621I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S  PIC X(5) VALUE "AB;CD".
       01 A1 PIC X(4).
       01 A2 PIC X(4).
       01 P  PIC 99.
       01 T  PIC 9.
       01 LN PIC 9.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L.
           MOVE FUNCTION LENGTH(L) TO LN
           DISPLAY "  LEN=" LN
           MOVE "****" TO A1 A2
           MOVE 1 TO P
           MOVE 0 TO T
           UNSTRING L DELIMITED BY ";" INTO A1 A2 WITH POINTER P TALLYING IN T
           END-UNSTRING
           DISPLAY "  SENDER [" A1 "][" A2 "] P=" P " T=" T
           MOVE "****" TO A1 A2
           MOVE 1 TO P
           MOVE 0 TO T
           UNSTRING S DELIMITED BY L OR ";" INTO A1 A2 WITH POINTER P TALLYING IN T
           END-UNSTRING
           DISPLAY "  DELIM  [" A1 "][" A2 "] P=" P " T=" T
           GOBACK.
       END PROGRAM PB5621I.
       END PROGRAM PB5621U.
