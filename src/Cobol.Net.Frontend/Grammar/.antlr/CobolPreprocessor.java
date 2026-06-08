// Generated from e:/CobolSharp/src/CobolSharp.Compiler/Grammar/CobolPreprocessor.g4 by ANTLR 4.13.1
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast", "CheckReturnValue"})
public class CobolPreprocessor extends Parser {
	static { RuntimeMetaData.checkVersion("4.13.1", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		WS=1, COMMENT_START=2, END_IF=3, END_PERFORM=4, END_EVALUATE=5, END_READ=6, 
		END_SEARCH=7, END_CALL=8, END_SORT=9, END_MERGE=10, END_RETURN=11, END_REWRITE=12, 
		END_DELETE=13, END_WRITE=14, END_START=15, END_INVOKE=16, END_JSON=17, 
		END_XML=18, END_METHOD=19, END_ADD=20, END_SUBTRACT=21, END_MULTIPLY=22, 
		END_DIVIDE=23, END_COMPUTE=24, END_STRING=25, END_UNSTRING=26, END_ACCEPT=27, 
		END_DISPLAY=28, PROGRAM_ID=29, METHOD_ID=30, CLASS_ID=31, INTERFACE_ID=32, 
		WORKING_STORAGE=33, LOCAL_STORAGE=34, SENTENCE=35, DATE_WRITTEN=36, DATE_COMPILED=37, 
		SOURCE_COMPUTER=38, OBJECT_COMPUTER=39, SPECIAL_NAMES=40, FILE_CONTROL=41, 
		I_O_CONTROL=42, I_O=43, PACKED_DECIMAL=44, DAY_OF_WEEK=45, IDENTIFICATION=46, 
		DIVISION=47, ENVIRONMENT=48, DATA=49, PROCEDURE=50, REPORT=51, SECTION=52, 
		LINKAGE=53, FD=54, RD=55, SD=56, ACCEPT=57, ADD=58, ALTER=59, CALL=60, 
		CANCEL=61, CLOSE=62, COMPUTE=63, CONTINUE=64, DELETE=65, DISPLAY=66, DIVIDE=67, 
		EVALUATE=68, EXIT=69, GOBACK=70, GO=71, IF=72, INITIALIZE=73, INSPECT=74, 
		INVOKE=75, JSON=76, MERGE=77, MOVE=78, MULTIPLY=79, OPEN=80, PERFORM=81, 
		READ=82, RELEASE=83, RETURN=84, REWRITE=85, SEARCH=86, SET=87, SORT=88, 
		START=89, STOP=90, STRING=91, SUBTRACT=92, UNSTRING=93, WRITE=94, XML=95, 
		ACCESS=96, ADDRESS=97, ALPHABETIC=98, ALPHABETIC_LOWER=99, ALPHABETIC_UPPER=100, 
		ADVANCING=101, AFTER=102, ALL=103, ALSO=104, ALPHANUMERIC_EDITED=105, 
		NUMERIC_EDITED=106, ALPHANUMERIC=107, ALTERNATE=108, AND=109, ANY=110, 
		ASCENDING=111, ASSIGN=112, ARE=113, AT=114, AUTHOR=115, BEFORE=116, BINARY=117, 
		BLANK=118, BY=119, CHARACTER=120, CHARACTERS=121, CLASS=122, COLLATING=123, 
		COMMON=124, COMP=125, COMP_1=126, COMP_2=127, COMP_3=128, COMP_5=129, 
		COMPUTATIONAL=130, COMPUTATIONAL_1=131, COMPUTATIONAL_2=132, COMPUTATIONAL_3=133, 
		COMPUTATIONAL_5=134, CONTENT=135, CONVERTING=136, CURRENCY=137, DECIMAL_POINT=138, 
		CORRESPONDING=139, COUNT=140, DATE=141, DAY=142, DECLARATIVES=143, DELIMITED=144, 
		DELIMITER=145, DEPENDING=146, DESCENDING=147, DOWN=148, DUPLICATES=149, 
		DYNAMIC=150, EDITED=151, ELSE=152, END=153, ENTRY=154, EQUAL=155, ERROR=156, 
		EXCEPTION=157, EXTEND=158, EXTERNAL=159, FIRST=160, FOR=161, FALSE_=162, 
		FILE=163, FILLER=164, POSITIVE=165, NEGATIVE=166, RESERVE=167, FROM=168, 
		FUNCTION=169, LABEL=170, GENERIC=171, GIVING=172, GLOBAL=173, GREATER=174, 
		SYMBOLIC=175, ALPHABET=176, CRT=177, CURSOR=178, CHANNEL=179, PROCEED=180, 
		USE=181, STANDARD=182, REPORTING=183, SUM=184, IN=185, INDEX=186, INDEXED=187, 
		INITIAL_=188, INPUT=189, INSTALLATION=190, INTO=191, INVALID=192, IS=193, 
		JUST=194, JUSTIFIED=195, KEY=196, LEADING=197, LEFT=198, LESS=199, LINE=200, 
		LINES=201, METHOD=202, MODE=203, NEXT=204, NOT=205, NUMERIC=206, NULL_=207, 
		OCCURS=208, OF=209, OFF=210, ON=211, OR=212, OMITTED=213, ORGANIZATION=214, 
		OTHER=215, OUTPUT=216, OVERFLOW=217, PACKED=218, PARAGRAPH=219, PIC=220, 
		POINTER=221, PREVIOUS=222, PROGRAM=223, RANDOM=224, RECORD=225, RECORDS=226, 
		RECURSIVE=227, REDEFINES=228, REPLACING=229, REFERENCE=230, RELATIVE=231, 
		REMAINDER=232, REMARKS=233, RENAMES=234, RETURNING=235, ROUNDED=236, RIGHT=237, 
		RUN=238, SECURITY=239, SELECT=240, SELF=241, SEPARATE=242, SEQUENCE=243, 
		SEQUENTIAL=244, SIGN=245, SIZE=246, STATUS=247, SUPER=248, SYNC=249, SYNCHRONIZED=250, 
		TALLYING=251, TEST=252, THAN=253, THEN=254, THROUGH=255, THRU=256, TIME=257, 
		TIMES=258, TO=259, TRAILING=260, TRUE_=261, TYPE=262, TYPEDEF=263, UNTIL=264, 
		UP=265, USAGE=266, USING=267, VALUE=268, VALUES=269, VARYING=270, WHEN=271, 
		WITH=272, ZERO=273, SPACE=274, HIGH_VALUE=275, LOW_VALUE=276, QUOTE_=277, 
		DECIMALLIT=278, IDENTIFIER=279, INTEGERLIT=280, STRINGLIT=281, HEXLIT=282, 
		POWER=283, LTEQUAL=284, GTEQUAL=285, NOTEQUAL=286, DOT=287, COMMA_SEP=288, 
		COMMA=289, LPAREN=290, RPAREN=291, LT=292, GT=293, EQUALS=294, PLUS=295, 
		MINUS=296, STAR=297, SLASH=298, COLON=299, SEMICOLON=300, ANY_CHAR=301, 
		PIC_IS=302, PIC_WS=303, PIC_STRING=304, SUB_WS=305, SUB_OF=306, SUB_IN=307, 
		SUB_ALL=308, SIGNED_INTEGERLIT=309, SUB_INTEGERLIT=310, SUB_DECIMALLIT=311, 
		SUB_IDENTIFIER=312, SUB_PLUS=313, SUB_MINUS=314, SUB_COMMA=315, SUB_COLON=316, 
		SUB_LPAREN=317, SUB_RPAREN=318, SUB_ANY=319, COMMENT_TEXT=320, COMMENT_END=321, 
		COPY_DIRECTIVE=322, COPY_DOT=323, COPY_NAME=324, COPY_STRINGLIT=325, COPY_REPLACING=326, 
		COPY_BY=327, REPLACE_DIRECTIVE=328, REPLACE_DOT=329, REPLACE_OFF=330, 
		REPLACE_BY=331, COPY_PSEUDO_OPEN=332, PSEUDO_TEXT_CLOSE=333, REPLACE_PSEUDO_OPEN=334, 
		PSEUDO_TEXT_BODY=335, COPY_TOKEN=336, REPLACE_TOKEN=337;
	public static final int
		RULE_copyDirective = 0, RULE_copyName = 1, RULE_copyReplacingPhrase = 2, 
		RULE_replaceClause = 3, RULE_replaceDirective = 4, RULE_replaceOffDirective = 5, 
		RULE_replaceDirectiveClause = 6, RULE_replaceableText = 7, RULE_replacementText = 8, 
		RULE_pseudoText = 9, RULE_pseudoTextBody = 10, RULE_tokenSequence = 11, 
		RULE_replaceToken = 12;
	private static String[] makeRuleNames() {
		return new String[] {
			"copyDirective", "copyName", "copyReplacingPhrase", "replaceClause", 
			"replaceDirective", "replaceOffDirective", "replaceDirectiveClause", 
			"replaceableText", "replacementText", "pseudoText", "pseudoTextBody", 
			"tokenSequence", "replaceToken"
		};
	}
	public static final String[] ruleNames = makeRuleNames();

	private static String[] makeLiteralNames() {
		return new String[] {
			null, null, "'*>'", "'END-IF'", "'END-PERFORM'", "'END-EVALUATE'", "'END-READ'", 
			"'END-SEARCH'", "'END-CALL'", "'END-SORT'", "'END-MERGE'", "'END-RETURN'", 
			"'END-REWRITE'", "'END-DELETE'", "'END-WRITE'", "'END-START'", "'END-INVOKE'", 
			"'END-JSON'", "'END-XML'", "'END-METHOD'", "'END-ADD'", "'END-SUBTRACT'", 
			"'END-MULTIPLY'", "'END-DIVIDE'", "'END-COMPUTE'", "'END-STRING'", "'END-UNSTRING'", 
			"'END-ACCEPT'", "'END-DISPLAY'", "'PROGRAM-ID'", "'METHOD-ID'", "'CLASS-ID'", 
			"'INTERFACE-ID'", "'WORKING-STORAGE'", "'LOCAL-STORAGE'", "'SENTENCE'", 
			"'DATE-WRITTEN'", "'DATE-COMPILED'", "'SOURCE-COMPUTER'", "'OBJECT-COMPUTER'", 
			"'SPECIAL-NAMES'", "'FILE-CONTROL'", "'I-O-CONTROL'", "'I-O'", "'PACKED-DECIMAL'", 
			"'DAY-OF-WEEK'", "'IDENTIFICATION'", "'DIVISION'", "'ENVIRONMENT'", "'DATA'", 
			"'PROCEDURE'", "'REPORT'", "'SECTION'", "'LINKAGE'", "'FD'", "'RD'", 
			"'SD'", "'ACCEPT'", "'ADD'", "'ALTER'", "'CALL'", "'CANCEL'", "'CLOSE'", 
			"'COMPUTE'", "'CONTINUE'", "'DELETE'", "'DISPLAY'", "'DIVIDE'", "'EVALUATE'", 
			"'EXIT'", "'GOBACK'", "'GO'", "'IF'", "'INITIALIZE'", "'INSPECT'", "'INVOKE'", 
			"'JSON'", "'MERGE'", "'MOVE'", "'MULTIPLY'", "'OPEN'", "'PERFORM'", "'READ'", 
			"'RELEASE'", "'RETURN'", "'REWRITE'", "'SEARCH'", "'SET'", "'SORT'", 
			"'START'", "'STOP'", "'STRING'", "'SUBTRACT'", "'UNSTRING'", "'WRITE'", 
			"'XML'", "'ACCESS'", "'ADDRESS'", "'ALPHABETIC'", "'ALPHABETIC-LOWER'", 
			"'ALPHABETIC-UPPER'", "'ADVANCING'", "'AFTER'", null, "'ALSO'", "'ALPHANUMERIC-EDITED'", 
			"'NUMERIC-EDITED'", "'ALPHANUMERIC'", "'ALTERNATE'", "'AND'", "'ANY'", 
			"'ASCENDING'", "'ASSIGN'", "'ARE'", "'AT'", "'AUTHOR'", "'BEFORE'", "'BINARY'", 
			"'BLANK'", "'BY'", "'CHARACTER'", "'CHARACTERS'", "'CLASS'", "'COLLATING'", 
			"'COMMON'", "'COMP'", "'COMP-1'", "'COMP-2'", "'COMP-3'", "'COMP-5'", 
			"'COMPUTATIONAL'", "'COMPUTATIONAL-1'", "'COMPUTATIONAL-2'", "'COMPUTATIONAL-3'", 
			"'COMPUTATIONAL-5'", "'CONTENT'", "'CONVERTING'", "'CURRENCY'", "'DECIMAL-POINT'", 
			"'CORRESPONDING'", "'COUNT'", "'DATE'", "'DAY'", "'DECLARATIVES'", "'DELIMITED'", 
			"'DELIMITER'", "'DEPENDING'", "'DESCENDING'", "'DOWN'", "'DUPLICATES'", 
			"'DYNAMIC'", "'EDITED'", "'ELSE'", "'END'", "'ENTRY'", "'EQUAL'", "'ERROR'", 
			"'EXCEPTION'", "'EXTEND'", "'EXTERNAL'", "'FIRST'", "'FOR'", "'FALSE'", 
			"'FILE'", "'FILLER'", "'POSITIVE'", "'NEGATIVE'", "'RESERVE'", "'FROM'", 
			"'FUNCTION'", "'LABEL'", "'GENERIC'", "'GIVING'", "'GLOBAL'", "'GREATER'", 
			"'SYMBOLIC'", "'ALPHABET'", "'CRT'", "'CURSOR'", "'CHANNEL'", "'PROCEED'", 
			"'USE'", "'STANDARD'", "'REPORTING'", "'SUM'", null, "'INDEX'", "'INDEXED'", 
			"'INITIAL'", "'INPUT'", "'INSTALLATION'", "'INTO'", "'INVALID'", null, 
			"'JUST'", "'JUSTIFIED'", "'KEY'", "'LEADING'", "'LEFT'", "'LESS'", "'LINE'", 
			"'LINES'", "'METHOD'", "'MODE'", "'NEXT'", "'NOT'", "'NUMERIC'", "'NULL'", 
			"'OCCURS'", null, "'OFF'", "'ON'", "'OR'", "'OMITTED'", "'ORGANIZATION'", 
			"'OTHER'", "'OUTPUT'", "'OVERFLOW'", "'PACKED'", "'PARAGRAPH'", null, 
			"'POINTER'", "'PREVIOUS'", "'PROGRAM'", "'RANDOM'", "'RECORD'", "'RECORDS'", 
			"'RECURSIVE'", "'REDEFINES'", "'REPLACING'", "'REFERENCE'", "'RELATIVE'", 
			"'REMAINDER'", "'REMARKS'", "'RENAMES'", "'RETURNING'", "'ROUNDED'", 
			"'RIGHT'", "'RUN'", "'SECURITY'", "'SELECT'", "'SELF'", "'SEPARATE'", 
			"'SEQUENCE'", "'SEQUENTIAL'", "'SIGN'", "'SIZE'", "'STATUS'", "'SUPER'", 
			"'SYNC'", "'SYNCHRONIZED'", "'TALLYING'", "'TEST'", "'THAN'", "'THEN'", 
			"'THROUGH'", "'THRU'", "'TIME'", "'TIMES'", "'TO'", "'TRAILING'", "'TRUE'", 
			"'TYPE'", "'TYPEDEF'", "'UNTIL'", "'UP'", "'USAGE'", "'USING'", "'VALUE'", 
			"'VALUES'", "'VARYING'", "'WHEN'", "'WITH'", null, null, null, null, 
			null, null, null, null, null, null, "'**'", "'<='", "'>='", "'<>'", "'.'", 
			null, null, null, null, "'<'", "'>'", "'='", null, null, "'*'", "'/'", 
			null, "';'"
		};
	}
	private static final String[] _LITERAL_NAMES = makeLiteralNames();
	private static String[] makeSymbolicNames() {
		return new String[] {
			null, "WS", "COMMENT_START", "END_IF", "END_PERFORM", "END_EVALUATE", 
			"END_READ", "END_SEARCH", "END_CALL", "END_SORT", "END_MERGE", "END_RETURN", 
			"END_REWRITE", "END_DELETE", "END_WRITE", "END_START", "END_INVOKE", 
			"END_JSON", "END_XML", "END_METHOD", "END_ADD", "END_SUBTRACT", "END_MULTIPLY", 
			"END_DIVIDE", "END_COMPUTE", "END_STRING", "END_UNSTRING", "END_ACCEPT", 
			"END_DISPLAY", "PROGRAM_ID", "METHOD_ID", "CLASS_ID", "INTERFACE_ID", 
			"WORKING_STORAGE", "LOCAL_STORAGE", "SENTENCE", "DATE_WRITTEN", "DATE_COMPILED", 
			"SOURCE_COMPUTER", "OBJECT_COMPUTER", "SPECIAL_NAMES", "FILE_CONTROL", 
			"I_O_CONTROL", "I_O", "PACKED_DECIMAL", "DAY_OF_WEEK", "IDENTIFICATION", 
			"DIVISION", "ENVIRONMENT", "DATA", "PROCEDURE", "REPORT", "SECTION", 
			"LINKAGE", "FD", "RD", "SD", "ACCEPT", "ADD", "ALTER", "CALL", "CANCEL", 
			"CLOSE", "COMPUTE", "CONTINUE", "DELETE", "DISPLAY", "DIVIDE", "EVALUATE", 
			"EXIT", "GOBACK", "GO", "IF", "INITIALIZE", "INSPECT", "INVOKE", "JSON", 
			"MERGE", "MOVE", "MULTIPLY", "OPEN", "PERFORM", "READ", "RELEASE", "RETURN", 
			"REWRITE", "SEARCH", "SET", "SORT", "START", "STOP", "STRING", "SUBTRACT", 
			"UNSTRING", "WRITE", "XML", "ACCESS", "ADDRESS", "ALPHABETIC", "ALPHABETIC_LOWER", 
			"ALPHABETIC_UPPER", "ADVANCING", "AFTER", "ALL", "ALSO", "ALPHANUMERIC_EDITED", 
			"NUMERIC_EDITED", "ALPHANUMERIC", "ALTERNATE", "AND", "ANY", "ASCENDING", 
			"ASSIGN", "ARE", "AT", "AUTHOR", "BEFORE", "BINARY", "BLANK", "BY", "CHARACTER", 
			"CHARACTERS", "CLASS", "COLLATING", "COMMON", "COMP", "COMP_1", "COMP_2", 
			"COMP_3", "COMP_5", "COMPUTATIONAL", "COMPUTATIONAL_1", "COMPUTATIONAL_2", 
			"COMPUTATIONAL_3", "COMPUTATIONAL_5", "CONTENT", "CONVERTING", "CURRENCY", 
			"DECIMAL_POINT", "CORRESPONDING", "COUNT", "DATE", "DAY", "DECLARATIVES", 
			"DELIMITED", "DELIMITER", "DEPENDING", "DESCENDING", "DOWN", "DUPLICATES", 
			"DYNAMIC", "EDITED", "ELSE", "END", "ENTRY", "EQUAL", "ERROR", "EXCEPTION", 
			"EXTEND", "EXTERNAL", "FIRST", "FOR", "FALSE_", "FILE", "FILLER", "POSITIVE", 
			"NEGATIVE", "RESERVE", "FROM", "FUNCTION", "LABEL", "GENERIC", "GIVING", 
			"GLOBAL", "GREATER", "SYMBOLIC", "ALPHABET", "CRT", "CURSOR", "CHANNEL", 
			"PROCEED", "USE", "STANDARD", "REPORTING", "SUM", "IN", "INDEX", "INDEXED", 
			"INITIAL_", "INPUT", "INSTALLATION", "INTO", "INVALID", "IS", "JUST", 
			"JUSTIFIED", "KEY", "LEADING", "LEFT", "LESS", "LINE", "LINES", "METHOD", 
			"MODE", "NEXT", "NOT", "NUMERIC", "NULL_", "OCCURS", "OF", "OFF", "ON", 
			"OR", "OMITTED", "ORGANIZATION", "OTHER", "OUTPUT", "OVERFLOW", "PACKED", 
			"PARAGRAPH", "PIC", "POINTER", "PREVIOUS", "PROGRAM", "RANDOM", "RECORD", 
			"RECORDS", "RECURSIVE", "REDEFINES", "REPLACING", "REFERENCE", "RELATIVE", 
			"REMAINDER", "REMARKS", "RENAMES", "RETURNING", "ROUNDED", "RIGHT", "RUN", 
			"SECURITY", "SELECT", "SELF", "SEPARATE", "SEQUENCE", "SEQUENTIAL", "SIGN", 
			"SIZE", "STATUS", "SUPER", "SYNC", "SYNCHRONIZED", "TALLYING", "TEST", 
			"THAN", "THEN", "THROUGH", "THRU", "TIME", "TIMES", "TO", "TRAILING", 
			"TRUE_", "TYPE", "TYPEDEF", "UNTIL", "UP", "USAGE", "USING", "VALUE", 
			"VALUES", "VARYING", "WHEN", "WITH", "ZERO", "SPACE", "HIGH_VALUE", "LOW_VALUE", 
			"QUOTE_", "DECIMALLIT", "IDENTIFIER", "INTEGERLIT", "STRINGLIT", "HEXLIT", 
			"POWER", "LTEQUAL", "GTEQUAL", "NOTEQUAL", "DOT", "COMMA_SEP", "COMMA", 
			"LPAREN", "RPAREN", "LT", "GT", "EQUALS", "PLUS", "MINUS", "STAR", "SLASH", 
			"COLON", "SEMICOLON", "ANY_CHAR", "PIC_IS", "PIC_WS", "PIC_STRING", "SUB_WS", 
			"SUB_OF", "SUB_IN", "SUB_ALL", "SIGNED_INTEGERLIT", "SUB_INTEGERLIT", 
			"SUB_DECIMALLIT", "SUB_IDENTIFIER", "SUB_PLUS", "SUB_MINUS", "SUB_COMMA", 
			"SUB_COLON", "SUB_LPAREN", "SUB_RPAREN", "SUB_ANY", "COMMENT_TEXT", "COMMENT_END", 
			"COPY_DIRECTIVE", "COPY_DOT", "COPY_NAME", "COPY_STRINGLIT", "COPY_REPLACING", 
			"COPY_BY", "REPLACE_DIRECTIVE", "REPLACE_DOT", "REPLACE_OFF", "REPLACE_BY", 
			"COPY_PSEUDO_OPEN", "PSEUDO_TEXT_CLOSE", "REPLACE_PSEUDO_OPEN", "PSEUDO_TEXT_BODY", 
			"COPY_TOKEN", "REPLACE_TOKEN"
		};
	}
	private static final String[] _SYMBOLIC_NAMES = makeSymbolicNames();
	public static final Vocabulary VOCABULARY = new VocabularyImpl(_LITERAL_NAMES, _SYMBOLIC_NAMES);

	/**
	 * @deprecated Use {@link #VOCABULARY} instead.
	 */
	@Deprecated
	public static final String[] tokenNames;
	static {
		tokenNames = new String[_SYMBOLIC_NAMES.length];
		for (int i = 0; i < tokenNames.length; i++) {
			tokenNames[i] = VOCABULARY.getLiteralName(i);
			if (tokenNames[i] == null) {
				tokenNames[i] = VOCABULARY.getSymbolicName(i);
			}

			if (tokenNames[i] == null) {
				tokenNames[i] = "<INVALID>";
			}
		}
	}

	@Override
	@Deprecated
	public String[] getTokenNames() {
		return tokenNames;
	}

	@Override

	public Vocabulary getVocabulary() {
		return VOCABULARY;
	}

	@Override
	public String getGrammarFileName() { return "CobolPreprocessor.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public CobolPreprocessor(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}

	@SuppressWarnings("CheckReturnValue")
	public static class CopyDirectiveContext extends ParserRuleContext {
		public TerminalNode COPY_DIRECTIVE() { return getToken(CobolPreprocessor.COPY_DIRECTIVE, 0); }
		public CopyNameContext copyName() {
			return getRuleContext(CopyNameContext.class,0);
		}
		public TerminalNode COPY_DOT() { return getToken(CobolPreprocessor.COPY_DOT, 0); }
		public CopyReplacingPhraseContext copyReplacingPhrase() {
			return getRuleContext(CopyReplacingPhraseContext.class,0);
		}
		public CopyDirectiveContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_copyDirective; }
	}

	public final CopyDirectiveContext copyDirective() throws RecognitionException {
		CopyDirectiveContext _localctx = new CopyDirectiveContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_copyDirective);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(26);
			match(COPY_DIRECTIVE);
			setState(27);
			copyName();
			setState(29);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==COPY_REPLACING) {
				{
				setState(28);
				copyReplacingPhrase();
				}
			}

			setState(31);
			match(COPY_DOT);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class CopyNameContext extends ParserRuleContext {
		public TerminalNode COPY_NAME() { return getToken(CobolPreprocessor.COPY_NAME, 0); }
		public TerminalNode COPY_STRINGLIT() { return getToken(CobolPreprocessor.COPY_STRINGLIT, 0); }
		public CopyNameContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_copyName; }
	}

	public final CopyNameContext copyName() throws RecognitionException {
		CopyNameContext _localctx = new CopyNameContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_copyName);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(33);
			_la = _input.LA(1);
			if ( !(_la==COPY_NAME || _la==COPY_STRINGLIT) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class CopyReplacingPhraseContext extends ParserRuleContext {
		public TerminalNode COPY_REPLACING() { return getToken(CobolPreprocessor.COPY_REPLACING, 0); }
		public List<ReplaceClauseContext> replaceClause() {
			return getRuleContexts(ReplaceClauseContext.class);
		}
		public ReplaceClauseContext replaceClause(int i) {
			return getRuleContext(ReplaceClauseContext.class,i);
		}
		public CopyReplacingPhraseContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_copyReplacingPhrase; }
	}

	public final CopyReplacingPhraseContext copyReplacingPhrase() throws RecognitionException {
		CopyReplacingPhraseContext _localctx = new CopyReplacingPhraseContext(_ctx, getState());
		enterRule(_localctx, 4, RULE_copyReplacingPhrase);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(35);
			match(COPY_REPLACING);
			setState(37); 
			_errHandler.sync(this);
			_la = _input.LA(1);
			do {
				{
				{
				setState(36);
				replaceClause();
				}
				}
				setState(39); 
				_errHandler.sync(this);
				_la = _input.LA(1);
			} while ( ((((_la - 324)) & ~0x3f) == 0 && ((1L << (_la - 324)) & 13571L) != 0) );
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ReplaceClauseContext extends ParserRuleContext {
		public ReplaceableTextContext replaceableText() {
			return getRuleContext(ReplaceableTextContext.class,0);
		}
		public TerminalNode COPY_BY() { return getToken(CobolPreprocessor.COPY_BY, 0); }
		public ReplacementTextContext replacementText() {
			return getRuleContext(ReplacementTextContext.class,0);
		}
		public ReplaceClauseContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_replaceClause; }
	}

	public final ReplaceClauseContext replaceClause() throws RecognitionException {
		ReplaceClauseContext _localctx = new ReplaceClauseContext(_ctx, getState());
		enterRule(_localctx, 6, RULE_replaceClause);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(41);
			replaceableText();
			setState(42);
			match(COPY_BY);
			setState(43);
			replacementText();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ReplaceDirectiveContext extends ParserRuleContext {
		public TerminalNode REPLACE_DIRECTIVE() { return getToken(CobolPreprocessor.REPLACE_DIRECTIVE, 0); }
		public TerminalNode REPLACE_DOT() { return getToken(CobolPreprocessor.REPLACE_DOT, 0); }
		public List<ReplaceDirectiveClauseContext> replaceDirectiveClause() {
			return getRuleContexts(ReplaceDirectiveClauseContext.class);
		}
		public ReplaceDirectiveClauseContext replaceDirectiveClause(int i) {
			return getRuleContext(ReplaceDirectiveClauseContext.class,i);
		}
		public ReplaceDirectiveContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_replaceDirective; }
	}

	public final ReplaceDirectiveContext replaceDirective() throws RecognitionException {
		ReplaceDirectiveContext _localctx = new ReplaceDirectiveContext(_ctx, getState());
		enterRule(_localctx, 8, RULE_replaceDirective);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(45);
			match(REPLACE_DIRECTIVE);
			setState(47); 
			_errHandler.sync(this);
			_la = _input.LA(1);
			do {
				{
				{
				setState(46);
				replaceDirectiveClause();
				}
				}
				setState(49); 
				_errHandler.sync(this);
				_la = _input.LA(1);
			} while ( ((((_la - 324)) & ~0x3f) == 0 && ((1L << (_la - 324)) & 13571L) != 0) );
			setState(51);
			match(REPLACE_DOT);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ReplaceOffDirectiveContext extends ParserRuleContext {
		public TerminalNode REPLACE_DIRECTIVE() { return getToken(CobolPreprocessor.REPLACE_DIRECTIVE, 0); }
		public TerminalNode REPLACE_OFF() { return getToken(CobolPreprocessor.REPLACE_OFF, 0); }
		public TerminalNode REPLACE_DOT() { return getToken(CobolPreprocessor.REPLACE_DOT, 0); }
		public ReplaceOffDirectiveContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_replaceOffDirective; }
	}

	public final ReplaceOffDirectiveContext replaceOffDirective() throws RecognitionException {
		ReplaceOffDirectiveContext _localctx = new ReplaceOffDirectiveContext(_ctx, getState());
		enterRule(_localctx, 10, RULE_replaceOffDirective);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(53);
			match(REPLACE_DIRECTIVE);
			setState(54);
			match(REPLACE_OFF);
			setState(55);
			match(REPLACE_DOT);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ReplaceDirectiveClauseContext extends ParserRuleContext {
		public ReplaceableTextContext replaceableText() {
			return getRuleContext(ReplaceableTextContext.class,0);
		}
		public TerminalNode REPLACE_BY() { return getToken(CobolPreprocessor.REPLACE_BY, 0); }
		public ReplacementTextContext replacementText() {
			return getRuleContext(ReplacementTextContext.class,0);
		}
		public ReplaceDirectiveClauseContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_replaceDirectiveClause; }
	}

	public final ReplaceDirectiveClauseContext replaceDirectiveClause() throws RecognitionException {
		ReplaceDirectiveClauseContext _localctx = new ReplaceDirectiveClauseContext(_ctx, getState());
		enterRule(_localctx, 12, RULE_replaceDirectiveClause);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(57);
			replaceableText();
			setState(58);
			match(REPLACE_BY);
			setState(59);
			replacementText();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ReplaceableTextContext extends ParserRuleContext {
		public PseudoTextContext pseudoText() {
			return getRuleContext(PseudoTextContext.class,0);
		}
		public TokenSequenceContext tokenSequence() {
			return getRuleContext(TokenSequenceContext.class,0);
		}
		public ReplaceableTextContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_replaceableText; }
	}

	public final ReplaceableTextContext replaceableText() throws RecognitionException {
		ReplaceableTextContext _localctx = new ReplaceableTextContext(_ctx, getState());
		enterRule(_localctx, 14, RULE_replaceableText);
		try {
			setState(63);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case COPY_PSEUDO_OPEN:
			case REPLACE_PSEUDO_OPEN:
				enterOuterAlt(_localctx, 1);
				{
				setState(61);
				pseudoText();
				}
				break;
			case COPY_NAME:
			case COPY_STRINGLIT:
			case COPY_TOKEN:
			case REPLACE_TOKEN:
				enterOuterAlt(_localctx, 2);
				{
				setState(62);
				tokenSequence();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ReplacementTextContext extends ParserRuleContext {
		public PseudoTextContext pseudoText() {
			return getRuleContext(PseudoTextContext.class,0);
		}
		public TokenSequenceContext tokenSequence() {
			return getRuleContext(TokenSequenceContext.class,0);
		}
		public ReplacementTextContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_replacementText; }
	}

	public final ReplacementTextContext replacementText() throws RecognitionException {
		ReplacementTextContext _localctx = new ReplacementTextContext(_ctx, getState());
		enterRule(_localctx, 16, RULE_replacementText);
		try {
			setState(67);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case COPY_PSEUDO_OPEN:
			case REPLACE_PSEUDO_OPEN:
				enterOuterAlt(_localctx, 1);
				{
				setState(65);
				pseudoText();
				}
				break;
			case COPY_NAME:
			case COPY_STRINGLIT:
			case COPY_TOKEN:
			case REPLACE_TOKEN:
				enterOuterAlt(_localctx, 2);
				{
				setState(66);
				tokenSequence();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class PseudoTextContext extends ParserRuleContext {
		public TerminalNode COPY_PSEUDO_OPEN() { return getToken(CobolPreprocessor.COPY_PSEUDO_OPEN, 0); }
		public PseudoTextBodyContext pseudoTextBody() {
			return getRuleContext(PseudoTextBodyContext.class,0);
		}
		public TerminalNode PSEUDO_TEXT_CLOSE() { return getToken(CobolPreprocessor.PSEUDO_TEXT_CLOSE, 0); }
		public TerminalNode REPLACE_PSEUDO_OPEN() { return getToken(CobolPreprocessor.REPLACE_PSEUDO_OPEN, 0); }
		public PseudoTextContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_pseudoText; }
	}

	public final PseudoTextContext pseudoText() throws RecognitionException {
		PseudoTextContext _localctx = new PseudoTextContext(_ctx, getState());
		enterRule(_localctx, 18, RULE_pseudoText);
		try {
			setState(77);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case COPY_PSEUDO_OPEN:
				enterOuterAlt(_localctx, 1);
				{
				setState(69);
				match(COPY_PSEUDO_OPEN);
				setState(70);
				pseudoTextBody();
				setState(71);
				match(PSEUDO_TEXT_CLOSE);
				}
				break;
			case REPLACE_PSEUDO_OPEN:
				enterOuterAlt(_localctx, 2);
				{
				setState(73);
				match(REPLACE_PSEUDO_OPEN);
				setState(74);
				pseudoTextBody();
				setState(75);
				match(PSEUDO_TEXT_CLOSE);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class PseudoTextBodyContext extends ParserRuleContext {
		public List<TerminalNode> PSEUDO_TEXT_BODY() { return getTokens(CobolPreprocessor.PSEUDO_TEXT_BODY); }
		public TerminalNode PSEUDO_TEXT_BODY(int i) {
			return getToken(CobolPreprocessor.PSEUDO_TEXT_BODY, i);
		}
		public PseudoTextBodyContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_pseudoTextBody; }
	}

	public final PseudoTextBodyContext pseudoTextBody() throws RecognitionException {
		PseudoTextBodyContext _localctx = new PseudoTextBodyContext(_ctx, getState());
		enterRule(_localctx, 20, RULE_pseudoTextBody);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(82);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==PSEUDO_TEXT_BODY) {
				{
				{
				setState(79);
				match(PSEUDO_TEXT_BODY);
				}
				}
				setState(84);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class TokenSequenceContext extends ParserRuleContext {
		public List<ReplaceTokenContext> replaceToken() {
			return getRuleContexts(ReplaceTokenContext.class);
		}
		public ReplaceTokenContext replaceToken(int i) {
			return getRuleContext(ReplaceTokenContext.class,i);
		}
		public TokenSequenceContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_tokenSequence; }
	}

	public final TokenSequenceContext tokenSequence() throws RecognitionException {
		TokenSequenceContext _localctx = new TokenSequenceContext(_ctx, getState());
		enterRule(_localctx, 22, RULE_tokenSequence);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(86); 
			_errHandler.sync(this);
			_alt = 1;
			do {
				switch (_alt) {
				case 1:
					{
					{
					setState(85);
					replaceToken();
					}
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				setState(88); 
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,7,_ctx);
			} while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER );
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ReplaceTokenContext extends ParserRuleContext {
		public TerminalNode COPY_NAME() { return getToken(CobolPreprocessor.COPY_NAME, 0); }
		public TerminalNode COPY_STRINGLIT() { return getToken(CobolPreprocessor.COPY_STRINGLIT, 0); }
		public TerminalNode COPY_TOKEN() { return getToken(CobolPreprocessor.COPY_TOKEN, 0); }
		public TerminalNode REPLACE_TOKEN() { return getToken(CobolPreprocessor.REPLACE_TOKEN, 0); }
		public ReplaceTokenContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_replaceToken; }
	}

	public final ReplaceTokenContext replaceToken() throws RecognitionException {
		ReplaceTokenContext _localctx = new ReplaceTokenContext(_ctx, getState());
		enterRule(_localctx, 24, RULE_replaceToken);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(90);
			_la = _input.LA(1);
			if ( !(((((_la - 324)) & ~0x3f) == 0 && ((1L << (_la - 324)) & 12291L) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static final String _serializedATN =
		"\u0004\u0001\u0151]\u0002\u0000\u0007\u0000\u0002\u0001\u0007\u0001\u0002"+
		"\u0002\u0007\u0002\u0002\u0003\u0007\u0003\u0002\u0004\u0007\u0004\u0002"+
		"\u0005\u0007\u0005\u0002\u0006\u0007\u0006\u0002\u0007\u0007\u0007\u0002"+
		"\b\u0007\b\u0002\t\u0007\t\u0002\n\u0007\n\u0002\u000b\u0007\u000b\u0002"+
		"\f\u0007\f\u0001\u0000\u0001\u0000\u0001\u0000\u0003\u0000\u001e\b\u0000"+
		"\u0001\u0000\u0001\u0000\u0001\u0001\u0001\u0001\u0001\u0002\u0001\u0002"+
		"\u0004\u0002&\b\u0002\u000b\u0002\f\u0002\'\u0001\u0003\u0001\u0003\u0001"+
		"\u0003\u0001\u0003\u0001\u0004\u0001\u0004\u0004\u00040\b\u0004\u000b"+
		"\u0004\f\u00041\u0001\u0004\u0001\u0004\u0001\u0005\u0001\u0005\u0001"+
		"\u0005\u0001\u0005\u0001\u0006\u0001\u0006\u0001\u0006\u0001\u0006\u0001"+
		"\u0007\u0001\u0007\u0003\u0007@\b\u0007\u0001\b\u0001\b\u0003\bD\b\b\u0001"+
		"\t\u0001\t\u0001\t\u0001\t\u0001\t\u0001\t\u0001\t\u0001\t\u0003\tN\b"+
		"\t\u0001\n\u0005\nQ\b\n\n\n\f\nT\t\n\u0001\u000b\u0004\u000bW\b\u000b"+
		"\u000b\u000b\f\u000bX\u0001\f\u0001\f\u0001\f\u0000\u0000\r\u0000\u0002"+
		"\u0004\u0006\b\n\f\u000e\u0010\u0012\u0014\u0016\u0018\u0000\u0002\u0001"+
		"\u0000\u0144\u0145\u0002\u0000\u0144\u0145\u0150\u0151W\u0000\u001a\u0001"+
		"\u0000\u0000\u0000\u0002!\u0001\u0000\u0000\u0000\u0004#\u0001\u0000\u0000"+
		"\u0000\u0006)\u0001\u0000\u0000\u0000\b-\u0001\u0000\u0000\u0000\n5\u0001"+
		"\u0000\u0000\u0000\f9\u0001\u0000\u0000\u0000\u000e?\u0001\u0000\u0000"+
		"\u0000\u0010C\u0001\u0000\u0000\u0000\u0012M\u0001\u0000\u0000\u0000\u0014"+
		"R\u0001\u0000\u0000\u0000\u0016V\u0001\u0000\u0000\u0000\u0018Z\u0001"+
		"\u0000\u0000\u0000\u001a\u001b\u0005\u0142\u0000\u0000\u001b\u001d\u0003"+
		"\u0002\u0001\u0000\u001c\u001e\u0003\u0004\u0002\u0000\u001d\u001c\u0001"+
		"\u0000\u0000\u0000\u001d\u001e\u0001\u0000\u0000\u0000\u001e\u001f\u0001"+
		"\u0000\u0000\u0000\u001f \u0005\u0143\u0000\u0000 \u0001\u0001\u0000\u0000"+
		"\u0000!\"\u0007\u0000\u0000\u0000\"\u0003\u0001\u0000\u0000\u0000#%\u0005"+
		"\u0146\u0000\u0000$&\u0003\u0006\u0003\u0000%$\u0001\u0000\u0000\u0000"+
		"&\'\u0001\u0000\u0000\u0000\'%\u0001\u0000\u0000\u0000\'(\u0001\u0000"+
		"\u0000\u0000(\u0005\u0001\u0000\u0000\u0000)*\u0003\u000e\u0007\u0000"+
		"*+\u0005\u0147\u0000\u0000+,\u0003\u0010\b\u0000,\u0007\u0001\u0000\u0000"+
		"\u0000-/\u0005\u0148\u0000\u0000.0\u0003\f\u0006\u0000/.\u0001\u0000\u0000"+
		"\u000001\u0001\u0000\u0000\u00001/\u0001\u0000\u0000\u000012\u0001\u0000"+
		"\u0000\u000023\u0001\u0000\u0000\u000034\u0005\u0149\u0000\u00004\t\u0001"+
		"\u0000\u0000\u000056\u0005\u0148\u0000\u000067\u0005\u014a\u0000\u0000"+
		"78\u0005\u0149\u0000\u00008\u000b\u0001\u0000\u0000\u00009:\u0003\u000e"+
		"\u0007\u0000:;\u0005\u014b\u0000\u0000;<\u0003\u0010\b\u0000<\r\u0001"+
		"\u0000\u0000\u0000=@\u0003\u0012\t\u0000>@\u0003\u0016\u000b\u0000?=\u0001"+
		"\u0000\u0000\u0000?>\u0001\u0000\u0000\u0000@\u000f\u0001\u0000\u0000"+
		"\u0000AD\u0003\u0012\t\u0000BD\u0003\u0016\u000b\u0000CA\u0001\u0000\u0000"+
		"\u0000CB\u0001\u0000\u0000\u0000D\u0011\u0001\u0000\u0000\u0000EF\u0005"+
		"\u014c\u0000\u0000FG\u0003\u0014\n\u0000GH\u0005\u014d\u0000\u0000HN\u0001"+
		"\u0000\u0000\u0000IJ\u0005\u014e\u0000\u0000JK\u0003\u0014\n\u0000KL\u0005"+
		"\u014d\u0000\u0000LN\u0001\u0000\u0000\u0000ME\u0001\u0000\u0000\u0000"+
		"MI\u0001\u0000\u0000\u0000N\u0013\u0001\u0000\u0000\u0000OQ\u0005\u014f"+
		"\u0000\u0000PO\u0001\u0000\u0000\u0000QT\u0001\u0000\u0000\u0000RP\u0001"+
		"\u0000\u0000\u0000RS\u0001\u0000\u0000\u0000S\u0015\u0001\u0000\u0000"+
		"\u0000TR\u0001\u0000\u0000\u0000UW\u0003\u0018\f\u0000VU\u0001\u0000\u0000"+
		"\u0000WX\u0001\u0000\u0000\u0000XV\u0001\u0000\u0000\u0000XY\u0001\u0000"+
		"\u0000\u0000Y\u0017\u0001\u0000\u0000\u0000Z[\u0007\u0001\u0000\u0000"+
		"[\u0019\u0001\u0000\u0000\u0000\b\u001d\'1?CMRX";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}