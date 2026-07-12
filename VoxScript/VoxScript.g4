grammar VoxScript;

// PARSER
program: actionSet;
actionSet: (action ';'*)*;

// Actions
action
    : cont_if
    | cont_while
    | cont_for
    | cont_return
    | cont_break
    | cont_continue
    | func_call
    | func_define
    | var_define
    | var_set
    | var_arith
    | var_incre
    ;
    
cont_if: CONT_IF '(' expression ')' (('{' actionSet? '}') | action) cont_else?;
cont_else: CONT_ELSE (('{' actionSet? '}') | action);
cont_while: CONT_WHILE '(' expression ')' (('{' actionSet? '}') | action);
cont_for: CONT_FOR '(' (for_object | for_repeat | for_range) ')' (('{' actionSet? '}') | action);
cont_return: CONT_RETURN expression?;
cont_break: CONT_BREAK;
cont_continue: CONT_CONTINUE;

func_call: identifier '(' (expression (',' expression)*)? ')';
func_define: OBJ_FUNCTION identifier '(' (var_inst (',' var_inst)*)? ')' type_reference? (('{' actionSet? '}') | action);

var_define: (OBJ_VAR | OBJ_CONST) var_inst '=' expression;
var_set: identifier '=' expression;
var_arith: identifier ARITH_ASSIGN expression;
var_incre: identifier (INCREMENT | DECREMENT);
var_inst: identifier type_reference?;

type_define: OBJ_TYPE identifier type_inherit? '{' type_member* '}';
type_inherit: '<<' identifier;
type_member: (TYPE_PRIVATE | TYPE_PUBLIC)? TYPE_STATIC? (type_field | type_function) ';'*;
type_field: identifier type_reference? ('=' expression)?;
type_function: TYPE_VIRTUAL? identifier '(' (var_inst (',' var_inst)*)? ')' type_reference? (('{' actionSet? '}') | action)?;

type_reference: (':' identifier);

for_object: identifier ',' identifier 'in' expression;
for_repeat: identifier CONT_FOR_AT expression CONT_FOR_REP expression CONT_FOR_ADD expression;
for_range: identifier CONT_FOR_FROM expression CONT_FOR_TO expression CONT_FOR_PER expression;

// Evaluation
expression
    : '(' expression ')'
    | '!' expression
    | '-' expression
    | left=expression op='^' right=expression
    | left=expression op=MUL_DIV right=expression
    | left=expression op=ADD_SUB right=expression
    | left=expression op=COND_AND right=expression
    | left=expression op=COND_OR right=expression
    | left=expression op=COMPARE right=expression
    | identifier
    | NUMBER
    | STRING
    | BOOLEAN
    | NULL
    | object
    | array
    | expression ('.' identifier)
    | condition=expression '?' primary=expression ':' secondary=expression
    | func_expr
    | func_call
    ;
object: '{' (objItem (',' objItem)*) '}';
objItem: expression '=' expression;
array: '[' (expression (',' expression)*)? ']';
identifier: ID iden_seg*;
iden_seg: iden_seg_text | iden_seg_expr;
iden_seg_text: '.' ID;
iden_seg_expr: '[' expression ']';
func_expr: OBJ_FUNCTION '(' (var_inst (',' var_inst)*)? ')' '{' actionSet? '}';


// LEXER

// Keywords
// >Control
CONT_IF: 'if';
CONT_ELSE: 'else';
CONT_FOR: 'for';
CONT_FOR_AT: 'at';
CONT_FOR_ADD: 'add';
CONT_FOR_REP: 'do';
CONT_FOR_FROM: 'from';
CONT_FOR_TO: 'to';
CONT_FOR_PER: 'per';
CONT_WHILE: 'while';
CONT_RETURN: 'return';
CONT_BREAK: 'break';
CONT_CONTINUE: 'continue';
// >Objects
OBJ_FUNCTION: 'function';
OBJ_TYPE: 'type';
OBJ_VAR: 'var';
OBJ_CONST: 'const';
// >Types
TYPE_STATIC: 'static';
TYPE_VIRTUAL: 'virtual';
TYPE_PUBLIC: 'public';
TYPE_PRIVATE: 'private';

// Primitives
NUMBER: ([1-9] [0-9]* | [0-9]) ('.' [0-9]+)?;
STRING: '"' ( ESC | ~["\\\r\n] )* '"';
BOOLEAN: TRUE | FALSE;
NULL: 'null';
fragment TRUE: 'true';
fragment FALSE: 'false';
ID: [a-zA-Z_][a-zA-Z0-9_]+;

fragment ESC
    : '\\' (
          ["\\/bfnrt]
        | 'u' HEX HEX HEX HEX
      )
    ;
fragment HEX
    : [0-9a-fA-F]
    ;

// Basic tokens
fragment SEMICOLON: ';';
fragment COLON: ':';
fragment EXCLAMATION: '!';
fragment QUOTATION: '"';
fragment LEFT_PAREN: '(';
fragment RIGHT_PAREN: ')';
fragment LEFT_BRACE: '[';
fragment RIGHT_BRACE: ']';
fragment LEFT_CURLY: '{';
fragment RIGHT_CURLY: '}';

// Operators
MUL_DIV: MULTIPLY | DIVIDE | MODULO;
ADD_SUB: PLUS | MINUS;
COMPARE: COND_EQUAL | COND_NOTEQUAL | COND_GREATERTHAN | COND_LESSTHAN | COND_GREATEROREQUAL | COND_LESSOREQUAL;
ARITH_ASSIGN: ADD_DIRECT | SUB_DIRECT | MULT_DIRECT | DIV_DIRECT | EXPO_DIRECT | MOD_DIRECT;

// >Eval
PLUS: '+';
MINUS: '-';
MULTIPLY: '*';
DIVIDE: '/';
EXPONENT: '^';
MODULO: '%';
EQUALS: '=';
// >Direct
INCREMENT: '++';
DECREMENT: '--';
ADD_DIRECT: '+=';
SUB_DIRECT: '-=';
MULT_DIRECT: '*=';
DIV_DIRECT: '/=';
EXPO_DIRECT: '^=';
MOD_DIRECT: '%=';
// >Conditions
COND_EQUAL: '==';
COND_NOTEQUAL: '!=';
COND_GREATERTHAN: '>';
COND_LESSTHAN: '<';
COND_GREATEROREQUAL: '>=';
COND_LESSOREQUAL: '<=';
COND_AND: '&&';
COND_OR: '||';


// Channels
WS: [ \n\r\t]+ -> skip;
LINE_COMMENT: '//' .*? '\n' -> skip;
MULTILINE_COMMENT: '/*' .*? '*/' -> skip;