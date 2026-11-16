      *>--- conformance-case
      *> id: g2fc01_four_formats_every_clause
      *> rules: [2002-FMT-12.3.4.1, 2002-FMT-12.3.4.10.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.1 - the four general formats of the file control
      *> entry (indexed, relative, sequential, sort-merge), ending "where
      *> collating-sequence-clause is described in 12.3.4.6"
      *>   cite.py --check -> OK. Every clause of every 2002 format is
      *> written once: Format 1 IX: OPTIONAL, ASSIGN TO literal, ACCESS
      *> DYNAMIC, ALTERNATE RECORD KEY data-name WITH DUPLICATES and
      *> record-key-name SOURCE IS, file-level and key-level COLLATING
      *> SEQUENCE, FILE STATUS, LOCK MODE AUTOMATIC WITH LOCK ON MULTIPLE
      *> RECORDS, ORGANIZATION INDEXED, RECORD KEY, RESERVE 2 AREAS,
      *> SHARING WITH READ ONLY. Format 2 RL: ACCESS RANDOM, LOCK MODE
      *> MANUAL ... RECORD, ORGANIZATION RELATIVE, RELATIVE KEY, RESERVE 1
      *> AREA, SHARING WITH NO OTHER. Format 3 SQ: ACCESS SEQUENTIAL, LOCK
      *> ON RECORD, ORGANIZATION IS SEQUENTIAL, PADDING CHARACTER IS
      *> literal-2, RESERVE 3, SHARING WITH ALL OTHER; SQ2: ASSIGN TO literal-1 USING data-name-1 and
      *> PADDING CHARACTER IS data-name-8; SQ3: ASSIGN USING data-name-1
      *> and the bare SEQUENTIAL (ORGANIZATION IS omitted). Format 4 SF:
      *> ASSIGN TO literal, ORGANIZATION IS SEQUENTIAL.
      *> Not written: Format 3's optional RECORD DELIMITER clause, left
      *> to its own row (2002-FMT-12.3.4.11.1). It "may be specified
      *> only for variable-length records" (§2002:12.3.4.11.2 SR1 ->
      *> cite.py OK), and every file here is fixed-length (implicit
      *> RECORD clause, §2002:13.16.41.3 GR6 -> cite.py OK).
      *> §2002:12.3.4.10.1 - PADDING CHARACTER IS {data-name-1 | literal-1};
      *> both forms (SQ literal "*", SQ2 data-name PAD-CH). The clause is
      *> "an obsolete element" (§2002:12.3.4.10 -> cite.py OK): warning
      *> COBOL0903 does not fail the case. §2002:12.3.4.10.2 SR1/SR3 hold
      *> (one-character literal; PAD-CH PIC X in working-storage) ->
      *> cite.py OK.
      *> Each leg fails if its entry is refused at 2002 or its connector
      *> does not behave as its organization requires.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>  IX: the third WRITE duplicates alternate key ALT2, allowed WITH
      *>   DUPLICATES -> "02" (§2002:9.1.12.1: "the record just written
      *>   created a duplicate key value for at least one alternate record
      *>   key for which duplicates are allowed" -> cite.py OK). READ NEXT
      *>   follows the prime key ascending under NATIVE: K001, K002, K003.
      *>   KEY IS IX-ALT "ALT1" -> K001. IX-SPL is "the concatenation of
      *>   all occurrences of data-name-2 in the order specified"
      *>   (§2002:12.3.4.5.3 GR2 -> cite.py OK): "CC"&"DD" -> K003.
      *>  RL: writes 00; random READ 5 -> 00 FIVE; READ 3 has no record ->
      *>   23 (§2002:9.1.12.4: "an attempt is made to randomly access a
      *>   record that does not exist in the physical file" -> cite.py OK).
      *>  SQ: ALPHA, BETA in write order, then 10 (§2002:9.1.12.3: "the end
      *>   of the physical file has been reached" -> cite.py OK).
      *>  SQ2: 00 GAMMA; SQ3: DELTA; SF ascending: APPLE, FIG, PEAR.
      *>  Every record field is PIC X(8) (IX-REC 12), space-filled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FC01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A1 IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL IX ASSIGN TO "G2FC1IX.DAT"
               ACCESS MODE IS DYNAMIC
               ALTERNATE RECORD KEY IS IX-ALT WITH DUPLICATES
               ALTERNATE RECORD KEY IS IX-SPL SOURCE IS IX-A IX-B
               COLLATING SEQUENCE IS A1
               COLLATING SEQUENCE OF IX-ALT IS A1
               FILE STATUS IS FS-IX
               LOCK MODE IS AUTOMATIC WITH LOCK ON MULTIPLE RECORDS
               ORGANIZATION IS INDEXED
               RECORD KEY IS IX-KEY
               RESERVE 2 AREAS
               SHARING WITH READ ONLY.
           SELECT RL ASSIGN TO "G2FC1RL.DAT"
               ACCESS MODE IS RANDOM
               FILE STATUS IS FS-RL
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORD
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RL-K
               RESERVE 1 AREA
               SHARING WITH NO OTHER.
           SELECT SQ ASSIGN TO "G2FC1SQ.DAT"
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS-SQ
               LOCK MODE IS MANUAL WITH LOCK ON RECORD
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS "*"
               RESERVE 3
               SHARING WITH ALL OTHER.
           SELECT SQ2 ASSIGN TO "G2FC1XX.DAT" USING SQ2-NAME
               PADDING CHARACTER IS PAD-CH
               FILE STATUS IS FS-SQ2.
           SELECT SQ3 ASSIGN USING SQ3-NAME
               SEQUENTIAL.
           SELECT SF ASSIGN TO "G2FC1SF.TMP"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  IX.
       01  IX-REC.
           05 IX-KEY   PIC X(4).
           05 IX-ALT   PIC X(4).
           05 IX-A     PIC X(2).
           05 IX-B     PIC X(2).
       FD  RL.
       01  RL-REC      PIC X(8).
       FD  SQ.
       01  SQ-REC      PIC X(8).
       FD  SQ2.
       01  SQ2-REC     PIC X(8).
       FD  SQ3.
       01  SQ3-REC     PIC X(8).
       SD  SF.
       01  SF-REC      PIC X(8).
       WORKING-STORAGE SECTION.
       01  FS-IX       PIC XX.
       01  FS-RL       PIC XX.
       01  FS-SQ       PIC XX.
       01  FS-SQ2      PIC XX.
       01  RL-K        PIC 9(4).
       01  SQ2-NAME    PIC X(20) VALUE "G2FC1S2.DAT".
       01  SQ3-NAME    PIC X(20) VALUE "G2FC1S3.DAT".
       01  PAD-CH      PIC X VALUE "#".
       01  EOF         PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
      *> Format 1 (indexed)
           OPEN OUTPUT IX.
           MOVE "K003ALT2CCDD" TO IX-REC. WRITE IX-REC.
           MOVE "K001ALT1AABB" TO IX-REC. WRITE IX-REC.
           MOVE "K002ALT2EEFF" TO IX-REC. WRITE IX-REC.
           DISPLAY "IX WRITE " FS-IX.
           CLOSE IX.
           OPEN INPUT IX.
           MOVE "N" TO EOF.
           PERFORM UNTIL EOF = "Y"
               READ IX NEXT RECORD
                   AT END MOVE "Y" TO EOF
                   NOT AT END DISPLAY "IX " IX-REC
               END-READ
           END-PERFORM.
           MOVE "ALT1" TO IX-ALT.
           READ IX KEY IS IX-ALT
               INVALID KEY DISPLAY "IX ALT INVALID"
               NOT INVALID KEY DISPLAY "IX ALT " IX-KEY
           END-READ.
           MOVE "CC" TO IX-A. MOVE "DD" TO IX-B.
           READ IX KEY IS IX-SPL
               INVALID KEY DISPLAY "IX SPL INVALID"
               NOT INVALID KEY DISPLAY "IX SPL " IX-KEY
           END-READ.
           CLOSE IX.
      *> Format 2 (relative)
           OPEN OUTPUT RL.
           MOVE 2 TO RL-K. MOVE "TWO" TO RL-REC. WRITE RL-REC.
           MOVE 5 TO RL-K. MOVE "FIVE" TO RL-REC. WRITE RL-REC.
           DISPLAY "RL WRITE " FS-RL.
           CLOSE RL.
           OPEN INPUT RL.
           MOVE 5 TO RL-K.
           READ RL INVALID KEY DISPLAY "RL 5 INVALID".
           DISPLAY "RL 5 " FS-RL " " RL-REC.
           MOVE 3 TO RL-K.
           READ RL INVALID KEY DISPLAY "RL 3 INVALID " FS-RL.
           CLOSE RL.
      *> Format 3 (sequential)
           OPEN OUTPUT SQ.
           MOVE "ALPHA" TO SQ-REC. WRITE SQ-REC.
           MOVE "BETA" TO SQ-REC. WRITE SQ-REC.
           CLOSE SQ.
           OPEN INPUT SQ.
           MOVE "N" TO EOF.
           PERFORM UNTIL EOF = "Y"
               READ SQ
                   AT END MOVE "Y" TO EOF DISPLAY "SQ END " FS-SQ
                   NOT AT END DISPLAY "SQ " SQ-REC
               END-READ
           END-PERFORM.
           CLOSE SQ.
           OPEN OUTPUT SQ2.
           MOVE "GAMMA" TO SQ2-REC. WRITE SQ2-REC.
           CLOSE SQ2.
           OPEN INPUT SQ2.
           READ SQ2 AT END DISPLAY "SQ2 EMPTY".
           DISPLAY "SQ2 " FS-SQ2 " " SQ2-REC.
           CLOSE SQ2.
           OPEN OUTPUT SQ3.
           MOVE "DELTA" TO SQ3-REC. WRITE SQ3-REC.
           CLOSE SQ3.
           OPEN INPUT SQ3.
           READ SQ3 AT END DISPLAY "SQ3 EMPTY".
           DISPLAY "SQ3 " SQ3-REC.
           CLOSE SQ3.
      *> Format 4 (sort-merge)
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS REL-P
               OUTPUT PROCEDURE IS RET-P.
           STOP RUN.
       REL-P.
           MOVE "PEAR" TO SF-REC. RELEASE SF-REC.
           MOVE "APPLE" TO SF-REC. RELEASE SF-REC.
           MOVE "FIG" TO SF-REC. RELEASE SF-REC.
       RET-P.
           MOVE "N" TO EOF.
           PERFORM UNTIL EOF = "Y"
               RETURN SF
                   AT END MOVE "Y" TO EOF
                   NOT AT END DISPLAY "SF " SF-REC
               END-RETURN
           END-PERFORM.
