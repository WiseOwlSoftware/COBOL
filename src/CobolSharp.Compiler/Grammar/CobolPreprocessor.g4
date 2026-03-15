parser grammar CobolPreprocessor;

options { tokenVocab = CobolLexer; }

// ==========================================
// COPY/REPLACE SUBSYSTEM
// ==========================================
//
// COPY/REPLACE runs BEFORE the main parser.
// These rules define the preprocessor AST structure.
// The preprocessor consumes COPY_DIRECTIVE and REPLACE_DIRECTIVE
// tokens from the lexer's COPYMODE/REPLACEMODE, expands them,
// and feeds the expanded text back to the main lexer.

// ==========================================
// COPY Directive (§14.3)
// ==========================================

copyDirective
    : COPY_DIRECTIVE copyName copyReplacingPhrase? COPY_DOT
    ;

copyName
    : COPY_NAME
    | COPY_STRINGLIT
    ;

// ==========================================
// COPY ... REPLACING Phrase
// ==========================================

copyReplacingPhrase
    : COPY_REPLACING replaceClause+
    ;

replaceClause
    : replaceableText COPY_BY replacementText
    ;

// ==========================================
// REPLACE Directive (§14.4)
// ==========================================

replaceDirective
    : REPLACE_DIRECTIVE replaceDirectiveClause+ REPLACE_DOT
    ;

replaceOffDirective
    : REPLACE_DIRECTIVE REPLACE_OFF REPLACE_DOT
    ;

replaceDirectiveClause
    : replaceableText REPLACE_BY replacementText
    ;

// ==========================================
// Replaceable / Replacement Text
// ==========================================

replaceableText
    : pseudoText
    | tokenSequence
    ;

replacementText
    : pseudoText
    | tokenSequence
    ;

// ==========================================
// Pseudo-Text (== ... ==)
// ==========================================

pseudoText
    : COPY_PSEUDO_OPEN pseudoTextBody PSEUDO_TEXT_CLOSE
    | REPLACE_PSEUDO_OPEN pseudoTextBody PSEUDO_TEXT_CLOSE
    ;

pseudoTextBody
    : PSEUDO_TEXT_BODY*
    ;

// ==========================================
// Token Sequence (for REPLACING)
// ==========================================

tokenSequence
    : replaceToken+
    ;

replaceToken
    : COPY_NAME
    | COPY_STRINGLIT
    | COPY_TOKEN
    | REPLACE_TOKEN
    ;
