parser grammar CobolDialect;

options { tokenVocab = CobolLexer; }

import CobolParserCore;

// Dialect-aware imperativeStatement (pseudo – you'll gate via predicates or mode)

imperativeStatement
    : ( dialect85Imperative
      | statement+
      )
    ;

dialect85Imperative
    : END    // only enabled when dialect == Cobol85
    ;
