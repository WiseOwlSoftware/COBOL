*>--- conformance-case
*> id: pb4298-floating-debugging-at-2014
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 2023 6.2.3.1 lists no debugging indicator, and the 2002 text calls
*> the debugging indicators "obsolete and ... to be removed in the next
*> revision": the floating >>D is obsolete at 2002 and REMOVED at 2014
*> (COBOL0902, an error unless --permissive; kb/Work R61, PB4298). Free
*> form, so the 85 run is not refused first by the introduction gate of
*> a fixed-form source.
>>SOURCE FORMAT FREE
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4298B14.
PROCEDURE DIVISION.
    DISPLAY "A".
>>D DISPLAY "DEBUG".
    STOP RUN.
