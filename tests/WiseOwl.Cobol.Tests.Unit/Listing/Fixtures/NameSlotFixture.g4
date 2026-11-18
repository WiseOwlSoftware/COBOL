// The planted failures of GrammarNameSlotDriftTests (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Guards"):
// six rules the name-slot counter must judge, one clean and one for each way a name slot can hide from it. The
// self-test generates this grammar with the vendored ANTLR tool and the product generator's flags, compiles it, runs
// the same counter over its ATN and context classes, and expects exactly the last five rules, each at its slot.
grammar NameSlotFixture;

// 1. Every slot labelled, each label unique in the rule: clean.
clean
    : MOVE refClean1=dataRef TO refClean2=dataRef
    ;

// 2. One label REUSED across two alternatives: ANTLR generates one field for two slots.
reused
    : GO refReused1=cobolWord
    | PERFORM refReused1=cobolWord
    ;

// 3. A cobolWord with no label.
unlabelled
    : READ refUnlabelled1=cobolWord INTO cobolWord
    ;

// 4. A name rule whose INTEGERLIT alternative (an alternative of a slot) has no label.
numbered
    : nameWord1=cobolWord
    | INTEGERLIT
    ;

// 5. A header whose transition into a reference rule carries no def label (R5-2).
header
    : procName DOT
    ;

// 6. A label with no ordinal: not of the name-slot label's form (GrammarSource.LabelPattern), so it classifies nothing.
malformed
    : DOT refMalformed=cobolWord
    ;

// A reference rule: every path begins with its head slot.
procName
    : (refHead1=cobolWord | refHead2=INTEGERLIT) ((OF | IN) (qualProc1=cobolWord | qualProc2=INTEGERLIT))?
    ;

dataRef
    : refHead1=cobolWord ((OF | IN) qualData1=cobolWord)*
    ;

cobolWord
    : IDENTIFIER
    ;

reservedGatedWord
    : TEXT
    ;

MOVE : 'MOVE' ;
TO : 'TO' ;
GO : 'GO' ;
PERFORM : 'PERFORM' ;
READ : 'READ' ;
INTO : 'INTO' ;
OF : 'OF' ;
IN : 'IN' ;
TEXT : 'TEXT' ;
DOT : '.' ;
INTEGERLIT : [0-9]+ ;
IDENTIFIER : [A-Z] [A-Z0-9-]* ;
WS : [ \t\r\n]+ -> skip ;
