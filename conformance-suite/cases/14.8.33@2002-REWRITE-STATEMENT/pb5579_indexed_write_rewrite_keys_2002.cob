*>--- conformance-case
*> id: pb5579_indexed_write_rewrite_keys_2002
*> rules: [2002-GR-14.8.33.3-2, 2002-GR-14.8.33.3-21, 2002-GR-14.8.33.3-22, 2002-GR-14.8.33.3-24,
*>     2002-GR-14.8.29.3-23, 2002-SR-14.8.47.2-1, 2002-GR-14.8.47.3-9, 2002-GR-14.8.47.3-33,
*>     2002-GR-14.8.47.3-34, 2002-GR-14.8.47.3-35]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5579 - the plain WRITE and REWRITE rules of an INDEXED file at COBOL-2002, each read from the 2002 text
*> (cite.py --check, each OK):
*>   §2002:14.8.47.2 SR1 "The write file is the file referenced by file-name-1 or by the file-name associated with
*>     record-name-1": each WRITE below names a record of one of two files, and each record lands in its own file.
*>   §2002:14.8.47.3 GR33 "The data item specified as the prime record key shall be set by the runtime element to the
*>     desired value prior to the execution of the WRITE statement": the key moved before each WRITE is the record's key.
*>   §2002:14.8.47.3 GR34 "records shall be released to the operating environment through that file connector in
*>     ascending order of prime record key values": on the SEQUENTIAL-access file a key below the last one written is
*>     '21' (W3), and in extend mode a key not above the highest key in the file when it was opened (X1) or written
*>     through the connector (X3) is '21'; the INVALID KEY phrase runs each time.
*>   §2002:14.8.47.3 GR35 "WRITE statements may release records to the operating environment through that connector in
*>     any order": the DYNAMIC-access file takes 05, 03, 09, 01 with status 00 each, and a prime-key walk reads them
*>     back in key order.
*>   §2002:14.8.29.3 GR23 "records having the same duplicate value in an alternate record key that is the key of
*>     reference are made available in the same order, or, in the case of PREVIOUS, in the reverse order, in which they
*>     are released by execution of WRITE statements": the X duplicates were written 05, 09, 01, so READ NEXT makes them
*>     available 05, 09, 01, and READ PREVIOUS from the Y record makes them available 01, 09, 05.
*>   §2002:14.8.47.3 GR9 "The file position indicator is not affected by the execution of a WRITE statement": after
*>     READ of 01, a WRITE of 00 (a key BEFORE the current record) leaves the indicator on 01, so READ NEXT gives 03.
*>   §2002:14.8.33.3 GR22 "If there is no existing record in the physical file with that prime record key, the execution
*>     of the REWRITE statement is unsuccessful and the I-O status in the rewrite file connector is set to the invalid
*>     key condition, '23'": REWRITE of key 07 (R1).
*>   §2002:14.8.33.3 GR24c "When an alternate record key of the record to be replaced does not allow duplicates and the
*>     value of that alternate record key is equal to the value of the corresponding alternate record key of a record in
*>     that physical file, the I-O status associated with the rewrite file connector is set to '22'": record 03 given
*>     the B key of record 05 (R2); the record area keeps what the program moved to it ("the content of the record area is
*>     unaffected") and the file still holds 03's own B key (R2-FILE).
*>   §2002:14.8.33.3 GR21 "the value of the prime record key of the record to be replaced shall be equal to the value of
*>     the prime record key of the last record read using this file connector. If it is not, the execution of the REWRITE
*>     statement is unsuccessful and the I-O status in the rewrite file connector is set to the invalid key condition,
*>     '21'": on the sequential-access file the key changed after READ is '21' (S1). The REWRITE that follows at once is
*>     '43' (S2), neither INVALID KEY phrase running, because §2002:14.8.33.3 GR4 asks that "the immediately previous
*>     input-output statement executed that referenced this file connector shall have been a successfully executed READ
*>     statement"; after a fresh READ the REWRITE with the key unchanged is '00' (S3) and replaces the record (S4).
*>   §2002:14.8.33.3 GR2 "The rewrite file connector shall have an open mode of I-O. If the open mode is some other value
*>     or the file is not open, the I-O status in the rewrite file connector is set to '49'": OPEN INPUT then REWRITE (M1),
*>     and REWRITE of the closed file (M2).
*> Each leg can fail: a status other than the one the rule names, or a record in the wrong order or file.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5579IX.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQ ASSIGN TO "pb5579sq.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS SEQUENTIAL
        RECORD KEY IS SQ-K
        FILE STATUS IS FS-SQ.
    SELECT DY ASSIGN TO "pb5579dy.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS DY-K
        ALTERNATE RECORD KEY IS DY-A WITH DUPLICATES
        ALTERNATE RECORD KEY IS DY-B
        FILE STATUS IS FS-DY.
DATA DIVISION.
FILE SECTION.
FD SQ.
01 SQ-REC.
   05 SQ-K PIC XX.
   05 SQ-D PIC XXX.
FD DY.
01 DY-REC.
   05 DY-K PIC XX.
   05 DY-A PIC X.
   05 DY-B PIC X.
   05 DY-D PIC XXX.
WORKING-STORAGE SECTION.
01 FS-SQ PIC XX.
01 FS-DY PIC XX.
01 TAG PIC X(8).
PROCEDURE DIVISION.
MAIN.
*> GR34 and GR33 on the sequential-access file.
    OPEN OUTPUT SQ
    MOVE "10" TO SQ-K  MOVE "AAA" TO SQ-D  MOVE "W1" TO TAG  PERFORM WRITE-SQ
    MOVE "30" TO SQ-K  MOVE "CCC" TO SQ-D  MOVE "W2" TO TAG  PERFORM WRITE-SQ
    MOVE "20" TO SQ-K  MOVE "BBB" TO SQ-D  MOVE "W3" TO TAG  PERFORM WRITE-SQ
    CLOSE SQ
    OPEN EXTEND SQ
    MOVE "25" TO SQ-K  MOVE "XXX" TO SQ-D  MOVE "X1" TO TAG  PERFORM WRITE-SQ
    MOVE "40" TO SQ-K  MOVE "DDD" TO SQ-D  MOVE "X2" TO TAG  PERFORM WRITE-SQ
    MOVE "35" TO SQ-K  MOVE "YYY" TO SQ-D  MOVE "X3" TO TAG  PERFORM WRITE-SQ
    CLOSE SQ
    OPEN INPUT SQ
    PERFORM UNTIL FS-SQ NOT = "00"
        READ SQ
            AT END CONTINUE
            NOT AT END DISPLAY "SQ " SQ-K " " SQ-D
        END-READ
    END-PERFORM
    CLOSE SQ
*> GR35 and GR33 on the dynamic-access file; SR1: these records land in DY, the ones above in SQ.
    OPEN OUTPUT DY
    MOVE "05" TO DY-K  MOVE "X" TO DY-A  MOVE "1" TO DY-B  MOVE "R05" TO DY-D  MOVE "D1" TO TAG  PERFORM WRITE-DY
    MOVE "03" TO DY-K  MOVE "Y" TO DY-A  MOVE "2" TO DY-B  MOVE "R03" TO DY-D  MOVE "D2" TO TAG  PERFORM WRITE-DY
    MOVE "09" TO DY-K  MOVE "X" TO DY-A  MOVE "3" TO DY-B  MOVE "R09" TO DY-D  MOVE "D3" TO TAG  PERFORM WRITE-DY
    MOVE "01" TO DY-K  MOVE "X" TO DY-A  MOVE "4" TO DY-B  MOVE "R01" TO DY-D  MOVE "D4" TO TAG  PERFORM WRITE-DY
    CLOSE DY
    OPEN I-O DY
*> The prime-key walk: key order whatever the order written.
    MOVE LOW-VALUES TO DY-K
    START DY KEY IS NOT LESS THAN DY-K
    PERFORM 4 TIMES
        READ DY NEXT RECORD
        DISPLAY "PK " DY-K " " FS-DY
    END-PERFORM
*> READ GR23: the duplicates of X in the order written, then backward from Y in the reverse order.
    MOVE "X" TO DY-A
    START DY KEY IS EQUAL TO DY-A
    PERFORM 4 TIMES
        READ DY NEXT RECORD
        DISPLAY "AK " DY-A " " DY-K " " FS-DY
    END-PERFORM
    PERFORM 3 TIMES
        READ DY PREVIOUS RECORD
        DISPLAY "AP " DY-A " " DY-K " " FS-DY
    END-PERFORM
*> WRITE GR9: the indicator stays on 01 across a WRITE of 00.
    MOVE "01" TO DY-K
    START DY KEY IS EQUAL TO DY-K
    READ DY NEXT RECORD
    DISPLAY "F0 " DY-K " " FS-DY
    MOVE "00" TO DY-K  MOVE "Z" TO DY-A  MOVE "5" TO DY-B  MOVE "R00" TO DY-D  MOVE "F1" TO TAG  PERFORM WRITE-DY
    READ DY NEXT RECORD
    DISPLAY "F2 " DY-K " " FS-DY
*> REWRITE GR22: no record 07.
    MOVE "07" TO DY-K  MOVE "Q" TO DY-A  MOVE "7" TO DY-B  MOVE "R07" TO DY-D
    REWRITE DY-REC
        INVALID KEY DISPLAY "R1 INVALID " FS-DY
        NOT INVALID KEY DISPLAY "R1 NOT INVALID " FS-DY
    END-REWRITE
*> REWRITE GR24 c): record 03 given record 05's B key.
    MOVE "03" TO DY-K
    READ DY KEY IS DY-K
    MOVE "1" TO DY-B  MOVE "NEW" TO DY-D
    REWRITE DY-REC
        INVALID KEY DISPLAY "R2 INVALID " FS-DY " " DY-K DY-A DY-B DY-D
        NOT INVALID KEY DISPLAY "R2 NOT INVALID " FS-DY
    END-REWRITE
    MOVE "03" TO DY-K
    READ DY KEY IS DY-K
    DISPLAY "R2-FILE " DY-K DY-A DY-B DY-D " " FS-DY
    CLOSE DY
*> REWRITE GR2: open mode INPUT, then not open.
    OPEN INPUT DY
    MOVE "05" TO DY-K
    READ DY KEY IS DY-K
    REWRITE DY-REC
    DISPLAY "M1 " FS-DY
    CLOSE DY
    REWRITE DY-REC
    DISPLAY "M2 " FS-DY
*> REWRITE GR21 on the sequential-access file.
    OPEN I-O SQ
    READ SQ
    MOVE "11" TO SQ-K
    REWRITE SQ-REC
        INVALID KEY DISPLAY "S1 INVALID " FS-SQ
        NOT INVALID KEY DISPLAY "S1 NOT INVALID " FS-SQ
    END-REWRITE
    MOVE "10" TO SQ-K  MOVE "NEW" TO SQ-D
    REWRITE SQ-REC
        INVALID KEY DISPLAY "S2 INVALID " FS-SQ
        NOT INVALID KEY DISPLAY "S2 NOT INVALID " FS-SQ
    END-REWRITE
    DISPLAY "S2 " FS-SQ
    CLOSE SQ
    OPEN I-O SQ
    READ SQ
    MOVE "NEW" TO SQ-D
    REWRITE SQ-REC
        INVALID KEY DISPLAY "S3 INVALID " FS-SQ
        NOT INVALID KEY DISPLAY "S3 NOT INVALID " FS-SQ
    END-REWRITE
    CLOSE SQ
    OPEN INPUT SQ
    READ SQ
    DISPLAY "S4 " SQ-K " " SQ-D " " FS-SQ
    CLOSE SQ
    STOP RUN.
WRITE-SQ.
    WRITE SQ-REC
        INVALID KEY DISPLAY TAG " INVALID " FS-SQ
        NOT INVALID KEY DISPLAY TAG " " FS-SQ
    END-WRITE.
WRITE-DY.
    WRITE DY-REC
        INVALID KEY DISPLAY TAG " INVALID " FS-DY
        NOT INVALID KEY DISPLAY TAG " " FS-DY
    END-WRITE.
