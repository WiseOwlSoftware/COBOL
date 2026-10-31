*>--- conformance-case
*> id: pb4134-fault-before-a-missing-period-then-a-malformed-end-marker
*> rules: [FMT-10.7.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4134 - ISO 10.7.2 prints the end marker "END PROGRAM program-name-1 ." and 10.7.3 SR2 makes
      *> program-name-1 identical to the program-name declared in a preceding PROGRAM-ID paragraph; the numeric
      *> literal 123 is no program-name. This program has two faults: the stray END-ADD (14.5.3.2), and the
      *> end marker's name, and the sentence the first is in lacks its period. The recovery of the first fault
      *> discards the rest of its sentence, and ran through the end marker to the marker's period, so the
      *> second fault was lost. The discard now stops where the structure around the sentence resumes, at
      *> the end marker, and the second fault is reported: this case expects the second diagnostic, COBOL5506
      *> naming the 123 (the first, COBOL0306, is pinned with it by ParseRecoveryUnitTailTests).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4134EM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF W-N = 1 MOVE 1 TO W-N END-ADD
       END PROGRAM 123.
