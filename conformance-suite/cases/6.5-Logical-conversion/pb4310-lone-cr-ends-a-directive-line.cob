*>--- conformance-case
*> id: pb4310-lone-cr-ends-a-directive-line
*> rules: [GR-6.5-9, DOC-A.1-156]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4310, DOC-A.1-156: a CR immediately before a LINE FEED is part of the terminator, and a CR NOT followed by one is a
*> character of the line. The directive line below ends in two CRs and a LINE FEED, so its operand is FIXED followed by one
*> CR - not a closed-set operand of SOURCE FORMAT (COBOL1911), as the same CR on a source line is refused. Fails if the
*> directive is accepted (the CR trimmed and the format switched).
>>SOURCE FORMAT FIXED
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4310CR.
       PROCEDURE DIVISION.
           STOP RUN.
