      *>--- conformance-case
      *> id: g2opt01_optional_open_modes
      *> rules: [2002-GR-12.3.4.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.3 GR2 - "The OPTIONAL phrase applies only to files
      *> opened in the input, I-O, or extend mode. Its specification is
      *> required for physical files that are not necessarily present
      *> each time the runtime element is executed."
      *>   cite.py --check "§2002:12.3.4.3 GR2" (both sentences) -> OK
      *> §2002:9.1.12.1 item 4: "I-O status = 05. An OPEN statement is
      *> successfully executed but the file is described as optional
      *> and the physical file is not present at the time the OPEN
      *> statement is executed. If the open mode is I-O or extend, the
      *> physical file has been created."  -> cite.py OK §2002:9.1.12.1
      *> §2002:9.1.12.3 item 1c: status 10 when "a sequential READ
      *> statement is attempted for the first time on a file described
      *> as optional and the physical file is not present"
      *>   -> cite.py OK §2002:9.1.12.3
      *> §2002:9.1.12.5 item 4: status 35 when "an OPEN statement with
      *> the INPUT, I-O, or EXTEND phrase is attempted on a file that is
      *> not described as optional and the physical file is not
      *> present"  -> cite.py OK §2002:9.1.12.5
      *> §2002:14.8.26.2 SR2: EXTEND only with sequential access, so the
      *> relative and indexed entries say ACCESS MODE IS SEQUENTIAL.
      *> Each case runs in a fresh directory: no file exists at start.
      *> XO is an OPTIONAL entry, XN the same entry without OPTIONAL,
      *> for X = SQ (sequential), RL (relative), IX (indexed). The
      *> physical file is named by ASSIGN USING, so one entry reaches a
      *> fresh absent file in each open mode.
      *> DERIVATION OF THE EXPECTED OUTPUT (per organization X):
      *>   INPUT, optional, absent: OPEN 05, first READ 10 (AT END)
      *>                                         => X OPT INPUT 05 10
      *>   INPUT, not optional, absent: 35       => X NON INPUT 35
      *>   I-O, optional, absent: 05 and the physical file has been
      *>     created, so a non-optional OPEN INPUT of that same file
      *>     then succeeds, 00                   => X OPT I-O 05 00
      *>   I-O, not optional, absent: 35         => X NON I-O 35
      *>   EXTEND, optional, absent: 05, created => X OPT EXTEND 05 00
      *>   EXTEND, not optional, absent: 35      => X NON EXTEND 35
      *>   OUTPUT, optional, absent: GR2 - OPTIONAL does not apply to
      *>     output mode, so an ordinary OPEN OUTPUT: 00
      *>                                         => X OPT OUTPUT 00
      *> A leg fails if OPTIONAL is ignored (35 for an OPT line), if an
      *> absent non-optional file is opened (00/05 for a NON line), if
      *> I-O/EXTEND does not create the file (35 after 05), or if
      *> OPTIONAL is wrongly applied to OUTPUT (05).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPT01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL SQO ASSIGN USING NSQO
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT SQN ASSIGN USING NSQN
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT OPTIONAL RLO ASSIGN USING NRLO
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT RLN ASSIGN USING NRLN
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT OPTIONAL IXO ASSIGN USING NIXO
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IXO-K FILE STATUS IS FS.
           SELECT IXN ASSIGN USING NIXN
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IXN-K FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SQO.
       01  SQO-R       PIC X(4).
       FD  SQN.
       01  SQN-R       PIC X(4).
       FD  RLO.
       01  RLO-R       PIC X(4).
       FD  RLN.
       01  RLN-R       PIC X(4).
       FD  IXO.
       01  IXO-R.
           05  IXO-K   PIC X(4).
       FD  IXN.
       01  IXN-R.
           05  IXN-K   PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS          PIC XX.
       01  FS1         PIC XX.
       01  NSQO        PIC X(20).
       01  NSQN        PIC X(20).
       01  NRLO        PIC X(20).
       01  NRLN        PIC X(20).
       01  NIXO        PIC X(20).
       01  NIXN        PIC X(20).
       01  SUF         PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
      *> INPUT mode
           MOVE "I" TO SUF. PERFORM SET-NAMES.
           OPEN INPUT SQO. MOVE FS TO FS1.
           READ SQO AT END CONTINUE END-READ.
           DISPLAY "SQ OPT INPUT " FS1 " " FS.
           CLOSE SQO.
           OPEN INPUT SQN. DISPLAY "SQ NON INPUT " FS.
           OPEN INPUT RLO. MOVE FS TO FS1.
           READ RLO AT END CONTINUE END-READ.
           DISPLAY "RL OPT INPUT " FS1 " " FS.
           CLOSE RLO.
           OPEN INPUT RLN. DISPLAY "RL NON INPUT " FS.
           OPEN INPUT IXO. MOVE FS TO FS1.
           READ IXO AT END CONTINUE END-READ.
           DISPLAY "IX OPT INPUT " FS1 " " FS.
           CLOSE IXO.
           OPEN INPUT IXN. DISPLAY "IX NON INPUT " FS.
      *> I-O mode: the OPT file is created, then opened INPUT through
      *> the non-optional entry
           MOVE "U" TO SUF. PERFORM SET-NAMES.
           OPEN I-O SQO. MOVE FS TO FS1. CLOSE SQO.
           OPEN I-O SQN. DISPLAY "SQ NON I-O " FS.
           MOVE NSQO TO NSQN.
           OPEN INPUT SQN. DISPLAY "SQ OPT I-O " FS1 " " FS.
           CLOSE SQN.
           OPEN I-O RLO. MOVE FS TO FS1. CLOSE RLO.
           OPEN I-O RLN. DISPLAY "RL NON I-O " FS.
           MOVE NRLO TO NRLN.
           OPEN INPUT RLN. DISPLAY "RL OPT I-O " FS1 " " FS.
           CLOSE RLN.
           OPEN I-O IXO. MOVE FS TO FS1. CLOSE IXO.
           OPEN I-O IXN. DISPLAY "IX NON I-O " FS.
           MOVE NIXO TO NIXN.
           OPEN INPUT IXN. DISPLAY "IX OPT I-O " FS1 " " FS.
           CLOSE IXN.
      *> EXTEND mode, likewise
           MOVE "E" TO SUF. PERFORM SET-NAMES.
           OPEN EXTEND SQO. MOVE FS TO FS1. CLOSE SQO.
           OPEN EXTEND SQN. DISPLAY "SQ NON EXTEND " FS.
           MOVE NSQO TO NSQN.
           OPEN INPUT SQN. DISPLAY "SQ OPT EXTEND " FS1 " " FS.
           CLOSE SQN.
           OPEN EXTEND RLO. MOVE FS TO FS1. CLOSE RLO.
           OPEN EXTEND RLN. DISPLAY "RL NON EXTEND " FS.
           MOVE NRLO TO NRLN.
           OPEN INPUT RLN. DISPLAY "RL OPT EXTEND " FS1 " " FS.
           CLOSE RLN.
           OPEN EXTEND IXO. MOVE FS TO FS1. CLOSE IXO.
           OPEN EXTEND IXN. DISPLAY "IX NON EXTEND " FS.
           MOVE NIXO TO NIXN.
           OPEN INPUT IXN. DISPLAY "IX OPT EXTEND " FS1 " " FS.
           CLOSE IXN.
      *> OUTPUT mode: OPTIONAL does not apply
           MOVE "O" TO SUF. PERFORM SET-NAMES.
           OPEN OUTPUT SQO. DISPLAY "SQ OPT OUTPUT " FS. CLOSE SQO.
           OPEN OUTPUT RLO. DISPLAY "RL OPT OUTPUT " FS. CLOSE RLO.
           OPEN OUTPUT IXO. DISPLAY "IX OPT OUTPUT " FS. CLOSE IXO.
           STOP RUN.
       SET-NAMES.
           MOVE SPACES TO NSQO NSQN NRLO NRLN NIXO NIXN.
           STRING "G2OSQO" SUF ".DAT" DELIMITED BY SIZE INTO NSQO.
           STRING "G2OSQN" SUF ".DAT" DELIMITED BY SIZE INTO NSQN.
           STRING "G2ORLO" SUF ".DAT" DELIMITED BY SIZE INTO NRLO.
           STRING "G2ORLN" SUF ".DAT" DELIMITED BY SIZE INTO NRLN.
           STRING "G2OIXO" SUF ".DAT" DELIMITED BY SIZE INTO NIXO.
           STRING "G2OIXN" SUF ".DAT" DELIMITED BY SIZE INTO NIXN.
