      *>--- conformance-case
      *> id: pb5591_relative_output_use_excuses_phrases_85
      *> rules: [1985-SR-VIII.4.5.3-5, 1985-SR-VIII.4.3.3-2,
      *>     1985-SR-VIII.4.6.3-4, 1985-SR-VIII.4.7.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5591 - a LATITUDE CHOICE (docs/CONFORMANCE.md 3.5).
      *> The relative READ, DELETE, REWRITE and START each need their
      *> phrase where "no applicable" (REWRITE: "appropriate") "USE
      *> AFTER STANDARD EXCEPTION procedure is specified"
      *> (§1985:VIII.4.5.3 SR5, §1985:VIII.4.3.3 SR2, §1985:VIII.4.6.3
      *> SR4, §1985:VIII.4.7.3 SR3). The 1985 text defines neither
      *> word; WiseOwl COBOL reads both as a procedure that applies to
      *> the file in some mode it can be open in (reading A), so the
      *> only declarative, USE ... ON OUTPUT, excuses all five
      *> statements below. A compiler on reading B refuses them. At
      *> run time the file is open in the output mode, so each fails
      *> (§1985:VIII.1.3.4: status 47 for a READ or START "attempted
      *> on a file not open in the input or I-O mode", 49 for a DELETE
      *> or REWRITE "attempted on a file not open in the I-O mode")
      *> and the procedure runs "for any file open in the output mode"
      *> (§1985:VIII.4.8.4 GR5c). After a critical status WiseOwl
      *> COBOL continues with the next statement (docs/CONFORMANCE.md
      *> 3.5). The test fails if the program is refused, if a
      *> procedure does not run, or if a status differs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5591D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "P5591D.REL"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RLF.
       01  RREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  RK PIC 9(4).
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-OUT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON OUTPUT.
       D-OUT-P.
           DISPLAY "USE OUTPUT " FS.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN OUTPUT RLF.
           DISPLAY "OPEN " FS.
           READ RLF NEXT RECORD.
           DISPLAY "AFTER READ NEXT " FS.
           MOVE 1 TO RK.
           READ RLF.
           DISPLAY "AFTER READ KEY " FS.
           START RLF KEY IS EQUAL TO RK.
           DISPLAY "AFTER START " FS.
           MOVE "ALPHA" TO RREC.
           REWRITE RREC.
           DISPLAY "AFTER REWRITE " FS.
           DELETE RLF.
           DISPLAY "AFTER DELETE " FS.
           WRITE RREC.
           DISPLAY "WRITE " FS.
           CLOSE RLF.
           STOP RUN.
