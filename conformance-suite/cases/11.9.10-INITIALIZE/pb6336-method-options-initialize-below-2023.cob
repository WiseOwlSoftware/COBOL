*>--- conformance-case
*> id: pb6336-method-options-initialize-below-2023
*> rules: [GR-11.9.10.4-6]
*> source-format: free
*> portable: unverified
*>---
*> The OPTIONS INITIALIZE clause is a COBOL-2023 addition (Annex E.3.3 item 33); a METHOD's own OPTIONS paragraph
*> carries it through the same introduction gate (kb/Work PB6336 moved the method's options scope earlier in the
*> method emitter, and that must not let the clause past the gate). The fill literal is the conforming X"41" spelling
*> so the only thing this fixture can be refused for is the edition gate.
IDENTIFICATION DIVISION.
CLASS-ID. NEGPB6336K INHERITS FROM BASE.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    CLASS BASE.
IDENTIFICATION DIVISION.
OBJECT.
PROCEDURE DIVISION.
METHOD-ID. M1.
OPTIONS.
    INITIALIZE LOCAL-STORAGE TO X"41".
DATA DIVISION.
LOCAL-STORAGE SECTION.
01 F PIC X(3).
PROCEDURE DIVISION.
    DISPLAY F.
END METHOD M1.
END OBJECT.
END CLASS NEGPB6336K.
