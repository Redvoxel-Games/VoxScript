// Generated from c:/Users/Timothy/source/repos/ObjectScript/ObjectScript/ObjectScript.g4 by ANTLR 4.13.1
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast", "CheckReturnValue"})
public class ObjectScriptParser extends Parser {
	static { RuntimeMetaData.checkVersion("4.13.1", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		T__0=1, T__1=2, CONT_IF=3, CONT_FOR=4, CONT_FOREACH=5, CONT_WHILE=6, CONT_REPEAT=7, 
		CONT_UNTIL=8, OBJ_FUNCTION=9, OBJ_TYPE=10, NUMBER=11, STRING=12, BOOLEAN=13, 
		CHARACTER=14, IDENTIFIER=15, SEMICOLON=16, COLON=17, EXCLAMATION=18, QUOTATION=19, 
		LEFT_PAREN=20, RIGHT_PAREN=21, LEFT_BRACE=22, RIGHT_BRACE=23, LEFT_CURLY=24, 
		RIGHT_CURLY=25, OPERATOR=26, PLUS=27, MINUS=28, MULTIPLY=29, DIVIDE=30, 
		EXPONENT=31, EQUALS=32, INCREMENT=33, DECREMENT=34, ADD_DIRECT=35, SUB_DIRECT=36, 
		MULT_DIRECT=37, DIV_DIRECT=38, EXPO_DIRECT=39, COND_EQUAL=40, COND_NOTEQUAL=41, 
		COND_GREATERTHAN=42, COND_LESSTHAN=43, COND_GREATEROREQUAL=44, COND_LESSOREQUAL=45, 
		WS=46;
	public static final int
		RULE_actionSet = 0, RULE_action = 1, RULE_cont_if = 2, RULE_expression = 3;
	private static String[] makeRuleNames() {
		return new String[] {
			"actionSet", "action", "cont_if", "expression"
		};
	}
	public static final String[] ruleNames = makeRuleNames();

	private static String[] makeLiteralNames() {
		return new String[] {
			null, "'\\n'", "'null'", "'if'", "'for'", "'foreach'", "'while'", "'repeat'", 
			"'until'", "'func'", "'type'", null, null, null, null, null, "';'", "':'", 
			"'!'", null, "'('", "')'", "'['", "']'", "'{'", "'}'", null, "'+'", "'-'", 
			"'*'", "'/'", "'^'", "'='", "'++'", "'--'", "'+='", "'-='", "'*='", "'/='", 
			"'^='", "'=='", "'!='", "'>'", "'<'", "'>='", "'<='"
		};
	}
	private static final String[] _LITERAL_NAMES = makeLiteralNames();
	private static String[] makeSymbolicNames() {
		return new String[] {
			null, null, null, "CONT_IF", "CONT_FOR", "CONT_FOREACH", "CONT_WHILE", 
			"CONT_REPEAT", "CONT_UNTIL", "OBJ_FUNCTION", "OBJ_TYPE", "NUMBER", "STRING", 
			"BOOLEAN", "CHARACTER", "IDENTIFIER", "SEMICOLON", "COLON", "EXCLAMATION", 
			"QUOTATION", "LEFT_PAREN", "RIGHT_PAREN", "LEFT_BRACE", "RIGHT_BRACE", 
			"LEFT_CURLY", "RIGHT_CURLY", "OPERATOR", "PLUS", "MINUS", "MULTIPLY", 
			"DIVIDE", "EXPONENT", "EQUALS", "INCREMENT", "DECREMENT", "ADD_DIRECT", 
			"SUB_DIRECT", "MULT_DIRECT", "DIV_DIRECT", "EXPO_DIRECT", "COND_EQUAL", 
			"COND_NOTEQUAL", "COND_GREATERTHAN", "COND_LESSTHAN", "COND_GREATEROREQUAL", 
			"COND_LESSOREQUAL", "WS"
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
	public String getGrammarFileName() { return "ObjectScript.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public ObjectScriptParser(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ActionSetContext extends ParserRuleContext {
		public List<ActionContext> action() {
			return getRuleContexts(ActionContext.class);
		}
		public ActionContext action(int i) {
			return getRuleContext(ActionContext.class,i);
		}
		public List<TerminalNode> SEMICOLON() { return getTokens(ObjectScriptParser.SEMICOLON); }
		public TerminalNode SEMICOLON(int i) {
			return getToken(ObjectScriptParser.SEMICOLON, i);
		}
		public ActionSetContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_actionSet; }
	}

	public final ActionSetContext actionSet() throws RecognitionException {
		ActionSetContext _localctx = new ActionSetContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_actionSet);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(8);
			action();
			setState(13);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__0 || _la==SEMICOLON) {
				{
				{
				setState(9);
				_la = _input.LA(1);
				if ( !(_la==T__0 || _la==SEMICOLON) ) {
				_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				setState(10);
				action();
				}
				}
				setState(15);
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
	public static class ActionContext extends ParserRuleContext {
		public Cont_ifContext cont_if() {
			return getRuleContext(Cont_ifContext.class,0);
		}
		public ActionContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_action; }
	}

	public final ActionContext action() throws RecognitionException {
		ActionContext _localctx = new ActionContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_action);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(16);
			cont_if();
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
	public static class Cont_ifContext extends ParserRuleContext {
		public TerminalNode CONT_IF() { return getToken(ObjectScriptParser.CONT_IF, 0); }
		public TerminalNode LEFT_PAREN() { return getToken(ObjectScriptParser.LEFT_PAREN, 0); }
		public ExpressionContext expression() {
			return getRuleContext(ExpressionContext.class,0);
		}
		public TerminalNode RIGHT_PAREN() { return getToken(ObjectScriptParser.RIGHT_PAREN, 0); }
		public TerminalNode LEFT_CURLY() { return getToken(ObjectScriptParser.LEFT_CURLY, 0); }
		public ActionSetContext actionSet() {
			return getRuleContext(ActionSetContext.class,0);
		}
		public TerminalNode RIGHT_CURLY() { return getToken(ObjectScriptParser.RIGHT_CURLY, 0); }
		public Cont_ifContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_cont_if; }
	}

	public final Cont_ifContext cont_if() throws RecognitionException {
		Cont_ifContext _localctx = new Cont_ifContext(_ctx, getState());
		enterRule(_localctx, 4, RULE_cont_if);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(18);
			match(CONT_IF);
			setState(19);
			match(LEFT_PAREN);
			setState(20);
			expression(0);
			setState(21);
			match(RIGHT_PAREN);
			setState(22);
			match(LEFT_CURLY);
			setState(23);
			actionSet();
			setState(24);
			match(RIGHT_CURLY);
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
	public static class ExpressionContext extends ParserRuleContext {
		public TerminalNode IDENTIFIER() { return getToken(ObjectScriptParser.IDENTIFIER, 0); }
		public TerminalNode NUMBER() { return getToken(ObjectScriptParser.NUMBER, 0); }
		public TerminalNode STRING() { return getToken(ObjectScriptParser.STRING, 0); }
		public TerminalNode BOOLEAN() { return getToken(ObjectScriptParser.BOOLEAN, 0); }
		public TerminalNode EXCLAMATION() { return getToken(ObjectScriptParser.EXCLAMATION, 0); }
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode LEFT_PAREN() { return getToken(ObjectScriptParser.LEFT_PAREN, 0); }
		public TerminalNode RIGHT_PAREN() { return getToken(ObjectScriptParser.RIGHT_PAREN, 0); }
		public TerminalNode OPERATOR() { return getToken(ObjectScriptParser.OPERATOR, 0); }
		public ExpressionContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_expression; }
	}

	public final ExpressionContext expression() throws RecognitionException {
		return expression(0);
	}

	private ExpressionContext expression(int _p) throws RecognitionException {
		ParserRuleContext _parentctx = _ctx;
		int _parentState = getState();
		ExpressionContext _localctx = new ExpressionContext(_ctx, _parentState);
		ExpressionContext _prevctx = _localctx;
		int _startState = 6;
		enterRecursionRule(_localctx, 6, RULE_expression, _p);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(38);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case IDENTIFIER:
				{
				setState(27);
				match(IDENTIFIER);
				}
				break;
			case NUMBER:
				{
				setState(28);
				match(NUMBER);
				}
				break;
			case STRING:
				{
				setState(29);
				match(STRING);
				}
				break;
			case BOOLEAN:
				{
				setState(30);
				match(BOOLEAN);
				}
				break;
			case T__1:
				{
				setState(31);
				match(T__1);
				}
				break;
			case EXCLAMATION:
				{
				setState(32);
				match(EXCLAMATION);
				setState(33);
				expression(3);
				}
				break;
			case LEFT_PAREN:
				{
				setState(34);
				match(LEFT_PAREN);
				setState(35);
				expression(0);
				setState(36);
				match(RIGHT_PAREN);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
			_ctx.stop = _input.LT(-1);
			setState(45);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,2,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					{
					_localctx = new ExpressionContext(_parentctx, _parentState);
					pushNewRecursionContext(_localctx, _startState, RULE_expression);
					setState(40);
					if (!(precpred(_ctx, 1))) throw new FailedPredicateException(this, "precpred(_ctx, 1)");
					setState(41);
					match(OPERATOR);
					setState(42);
					expression(2);
					}
					} 
				}
				setState(47);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,2,_ctx);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			unrollRecursionContexts(_parentctx);
		}
		return _localctx;
	}

	public boolean sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 3:
			return expression_sempred((ExpressionContext)_localctx, predIndex);
		}
		return true;
	}
	private boolean expression_sempred(ExpressionContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0:
			return precpred(_ctx, 1);
		}
		return true;
	}

	public static final String _serializedATN =
		"\u0004\u0001.1\u0002\u0000\u0007\u0000\u0002\u0001\u0007\u0001\u0002\u0002"+
		"\u0007\u0002\u0002\u0003\u0007\u0003\u0001\u0000\u0001\u0000\u0001\u0000"+
		"\u0005\u0000\f\b\u0000\n\u0000\f\u0000\u000f\t\u0000\u0001\u0001\u0001"+
		"\u0001\u0001\u0002\u0001\u0002\u0001\u0002\u0001\u0002\u0001\u0002\u0001"+
		"\u0002\u0001\u0002\u0001\u0002\u0001\u0003\u0001\u0003\u0001\u0003\u0001"+
		"\u0003\u0001\u0003\u0001\u0003\u0001\u0003\u0001\u0003\u0001\u0003\u0001"+
		"\u0003\u0001\u0003\u0001\u0003\u0003\u0003\'\b\u0003\u0001\u0003\u0001"+
		"\u0003\u0001\u0003\u0005\u0003,\b\u0003\n\u0003\f\u0003/\t\u0003\u0001"+
		"\u0003\u0000\u0001\u0006\u0004\u0000\u0002\u0004\u0006\u0000\u0001\u0002"+
		"\u0000\u0001\u0001\u0010\u00104\u0000\b\u0001\u0000\u0000\u0000\u0002"+
		"\u0010\u0001\u0000\u0000\u0000\u0004\u0012\u0001\u0000\u0000\u0000\u0006"+
		"&\u0001\u0000\u0000\u0000\b\r\u0003\u0002\u0001\u0000\t\n\u0007\u0000"+
		"\u0000\u0000\n\f\u0003\u0002\u0001\u0000\u000b\t\u0001\u0000\u0000\u0000"+
		"\f\u000f\u0001\u0000\u0000\u0000\r\u000b\u0001\u0000\u0000\u0000\r\u000e"+
		"\u0001\u0000\u0000\u0000\u000e\u0001\u0001\u0000\u0000\u0000\u000f\r\u0001"+
		"\u0000\u0000\u0000\u0010\u0011\u0003\u0004\u0002\u0000\u0011\u0003\u0001"+
		"\u0000\u0000\u0000\u0012\u0013\u0005\u0003\u0000\u0000\u0013\u0014\u0005"+
		"\u0014\u0000\u0000\u0014\u0015\u0003\u0006\u0003\u0000\u0015\u0016\u0005"+
		"\u0015\u0000\u0000\u0016\u0017\u0005\u0018\u0000\u0000\u0017\u0018\u0003"+
		"\u0000\u0000\u0000\u0018\u0019\u0005\u0019\u0000\u0000\u0019\u0005\u0001"+
		"\u0000\u0000\u0000\u001a\u001b\u0006\u0003\uffff\uffff\u0000\u001b\'\u0005"+
		"\u000f\u0000\u0000\u001c\'\u0005\u000b\u0000\u0000\u001d\'\u0005\f\u0000"+
		"\u0000\u001e\'\u0005\r\u0000\u0000\u001f\'\u0005\u0002\u0000\u0000 !\u0005"+
		"\u0012\u0000\u0000!\'\u0003\u0006\u0003\u0003\"#\u0005\u0014\u0000\u0000"+
		"#$\u0003\u0006\u0003\u0000$%\u0005\u0015\u0000\u0000%\'\u0001\u0000\u0000"+
		"\u0000&\u001a\u0001\u0000\u0000\u0000&\u001c\u0001\u0000\u0000\u0000&"+
		"\u001d\u0001\u0000\u0000\u0000&\u001e\u0001\u0000\u0000\u0000&\u001f\u0001"+
		"\u0000\u0000\u0000& \u0001\u0000\u0000\u0000&\"\u0001\u0000\u0000\u0000"+
		"\'-\u0001\u0000\u0000\u0000()\n\u0001\u0000\u0000)*\u0005\u001a\u0000"+
		"\u0000*,\u0003\u0006\u0003\u0002+(\u0001\u0000\u0000\u0000,/\u0001\u0000"+
		"\u0000\u0000-+\u0001\u0000\u0000\u0000-.\u0001\u0000\u0000\u0000.\u0007"+
		"\u0001\u0000\u0000\u0000/-\u0001\u0000\u0000\u0000\u0003\r&-";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}