      *>--- conformance-case
      *> id: g85_bf10_11
      *> rules: [1985-SR-IX.4.7.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.7.3 SR2 - "Data-name-1 may be qualified." START IF1 KEY IS
      *> ... IK OF IREC, the RECORD KEY (itself qualified); the working-storage
      *> group WK holds another IK ("K000") that START must not use: a START
      *> bound to WK's IK would give GT K001 -> K001 V1 (GT K000), not K002.
      *> Derivation: §1985:IX.4.7.4 GR4 - the file position indicator is set to
      *> the first record whose key satisfies the comparison; READ NEXT returns
      *> it. Records K001, K002, K003.
      *>   GT K001 -> K002 : START from IK OF IREC = "K001" (GREATER THAN).
      *>   NLT K003 -> K003
      *>   GT K003 -> INVALID : no record has a greater key.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1011.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IF1 ASSIGN TO "bf1011.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IK OF IREC
               FILE STATUS IS IF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IF1
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05  IK                  PIC X(4).
           05  IV                  PIC X(2).
       WORKING-STORAGE SECTION.
       01  WK.
           05  IK                  PIC X(4) VALUE "K000".
       01  IF-ST                   PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT IF1.
           MOVE "K001" TO IK OF IREC.
           MOVE "V1" TO IV.
           WRITE IREC INVALID KEY DISPLAY "W-ERR".
           MOVE "K002" TO IK OF IREC.
           MOVE "V2" TO IV.
           WRITE IREC INVALID KEY DISPLAY "W-ERR".
           MOVE "K003" TO IK OF IREC.
           MOVE "V3" TO IV.
           WRITE IREC INVALID KEY DISPLAY "W-ERR".
           CLOSE IF1.
           OPEN INPUT IF1.
           MOVE "K001" TO IK OF IREC.
           START IF1 KEY IS GREATER THAN IK OF IREC
               INVALID KEY DISPLAY "INVALID".
           READ IF1 NEXT RECORD AT END DISPLAY "EOF".
           DISPLAY "GT K001 -> " IK OF IREC " " IV.
           MOVE "K003" TO IK OF IREC.
           START IF1 KEY IS NOT LESS THAN IK OF IREC
               INVALID KEY DISPLAY "INVALID".
           READ IF1 NEXT RECORD AT END DISPLAY "EOF".
           DISPLAY "NLT K003 -> " IK OF IREC " " IV.
           MOVE "K003" TO IK OF IREC.
           START IF1 KEY IS GREATER THAN IK OF IREC
               INVALID KEY DISPLAY "GT K003 -> INVALID".
           DISPLAY "WK=" IK OF WK.
           CLOSE IF1.
           STOP RUN.
