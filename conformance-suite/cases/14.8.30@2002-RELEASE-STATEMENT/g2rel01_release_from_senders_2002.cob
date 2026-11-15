      *>--- conformance-case
      *> id: g2rel01_release_from_senders_2002
      *> rules: [2002-SR-14.8.30.2-3, 2002-GR-14.8.30.3-2,
      *>    2002-GR-14.8.30.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.30.2 SR3 - identifier-1 or literal-1 "shall be
      *>   valid as a sending operand in a MOVE statement specifying
      *>   record-name-1 as the receiving operand": a CD record group
      *>   and a CD record item (identifier-1 under a 2002 CD), an
      *>   alphanumeric literal, and the figurative constant ALL "7"
      *>   to a NUMERIC record (§2002:14.8.24.2 SR5 forbids only
      *>   SPACE to a numeric item; SR8 table 17 alphanumeric to
      *>   numeric is valid). The ALL "7" leg is edition-neutral (an
      *>   all-digit ALL literal to an integer item stays valid after
      *>   2002); the CD senders are this case's 2002-only reach, and
      *>   g2rel02_release_figurative_to_numeric_2002 carries the
      *>   2002-only figurative senders (QUOTE, HIGH-VALUE, ALL "A").
      *> §2002:14.8.30.3 GR2 - RELEASE "causes the record named by
      *>   record-name-1 to be released to the initial phase of a sort
      *>   operation": the record "3333" released without FROM is
      *>   returned by the sort.
      *> §2002:14.8.30.3 GR4 - RELEASE ... FROM is MOVE then "The
      *>   same RELEASE statement without the FROM phrase", "according
      *>   to the rules specified for the MOVE statement": FROM CA and
      *>   the explicit MOVE CA + RELEASE give the same record.
      *> DERIVATION (2002 text only). Records released:
      *>   1 FROM CREC (group "11"+"22"): a group move, GR2 of
      *>     §2002:14.8.24.3 (alphanumeric to alphanumeric): "1122".
      *>   2 FROM CA ("11", 2 chars) to the 4-char record: "11  "
      *>     (alphanumeric move, space fill on the right).
      *>   3 MOVE CA TO SRT-REC + RELEASE SRT-REC: "11  " (GR4).
      *>   4 MOVE "3333" + RELEASE SRT-REC (no FROM): "3333" (GR2).
      *>   5 FROM "5555" (literal-1): "5555".
      *>   6 SRT-NUM PIC 9(4) FROM ALL "7": §2002:14.8.24.3 GR4b 3.b
      *>     the sending operand is an unsigned integer whose digits
      *>     number the receiver's, "the figurative constant is
      *>     replicated in this item, from left to right": 7777.
      *>   After record 1, CREC is shown: §2002:14.8.30.3 GR5 "the
      *>   information in the area referenced by identifier-1 is
      *>   available" - [1122]. The record area itself is NOT shown
      *>   after a RELEASE (GR3: "no longer available").
      *>   §2002:14.8.36.3 GR7a returns the lower key first; digits
      *>   ascend and the space precedes the digits in the native
      *>   sequence (ASCII and EBCDIC alike): 11  , 11  , 1122,
      *>   3333, 5555, 7777. The two "11  " records are identical,
      *>   so their order (GR4, no DUPLICATES) cannot show.
      *> The CD draws the obsolete warning COBOL0903 at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REL01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "g2rel01.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SRTF.
       01 SRT-REC PIC X(4).
       01 SRT-NUM PIC 9(4).
       WORKING-STORAGE SECTION.
       01 EOF-FLAG PIC X VALUE "N".
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CA PIC X(2).
          05 CB PIC X(2).
       PROCEDURE DIVISION.
       MAIN-P.
           SORT SRTF ON ASCENDING KEY SRT-REC
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           DISPLAY "DONE".
           STOP RUN.
       FEED.
           MOVE "11" TO CA.
           MOVE "22" TO CB.
           RELEASE SRT-REC FROM CREC.
           DISPLAY "CREC=[" CREC "]".
           RELEASE SRT-REC FROM CA.
           MOVE CA TO SRT-REC.
           RELEASE SRT-REC.
           MOVE "3333" TO SRT-REC.
           RELEASE SRT-REC.
           RELEASE SRT-REC FROM "5555".
           RELEASE SRT-NUM FROM ALL "7".
       DRAIN.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SRTF RECORD
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY "R=[" SRT-REC "]"
               END-RETURN
           END-PERFORM.
