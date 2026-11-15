      *>--- conformance-case
      *> id: g2rel02_release_figurative_to_numeric_2002
      *> rules: [2002-SR-14.8.30.2-3, 2002-GR-14.8.30.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.30.2 SR3 - identifier-1 or literal-1 "shall be
      *>   valid as a sending operand in a MOVE statement specifying
      *>   record-name-1 as the receiving operand". Under the 2002
      *>   MOVE rules the figurative constants QUOTE, HIGH-VALUE and
      *>   ALL "A" are valid senders to the numeric integer record
      *>   SRT-NUM: §2002:14.8.24.2 SR5 forbids only SPACE to a
      *>   numeric item, a figurative constant is an alphanumeric
      *>   character value (§2002:8.3.1.2.5.3 GR1), and SR8 table 17
      *>   makes alphanumeric to numeric valid (§2002:G.1 item 2:
      *>   "The MOVE statement allows any figurative constant except
      *>   SPACE to be moved to an integer numeric item"). These
      *>   three RELEASE statements are the 2002-only reach of SR3.
      *> The CONTENT of each released record is undefined:
      *>   §2002:14.8.24.3 GR4 (numeric receiver) - a sender that
      *>   "would result in a false value in a numeric class
      *>   condition" makes "the results of the execution of the MOVE
      *>   statement" undefined. So no record is displayed and the
      *>   sort key order is not observed; only the COUNT is pinned.
      *>   No TURN directive enables EC-DATA-INCOMPATIBLE checking
      *>   (§2002:14.5.12.1), so execution continues.
      *> §2002:14.8.30.3 GR2 - each RELEASE "causes the record named
      *>   by record-name-1 to be released to the initial phase of a
      *>   sort operation", and §2002:14.8.36.3 GR8a makes those
      *>   records available to the sort: three records are
      *>   returned, N=3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REL02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "g2rel02.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SRTF.
       01 SRT-REC PIC X(4).
       01 SRT-NUM PIC 9(4).
       WORKING-STORAGE SECTION.
       01 EOF-FLAG PIC X VALUE "N".
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           SORT SRTF ON ASCENDING KEY SRT-REC
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           DISPLAY "N=" N.
           STOP RUN.
       FEED.
           RELEASE SRT-NUM FROM QUOTE.
           RELEASE SRT-NUM FROM HIGH-VALUE.
           RELEASE SRT-NUM FROM ALL "A".
       DRAIN.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SRTF RECORD
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END ADD 1 TO N
               END-RETURN
           END-PERFORM.
