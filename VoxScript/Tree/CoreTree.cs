using Antlr4.Runtime;
using VoxScript.Integration;
using VoxScript.Runtime;
using static VoxScriptParser;

namespace VoxScript.Tree;

public abstract class AstNode
{
    public int Line = -1;
    public int Column = -1;
}

public class RootNode(StatementSet statements) : AstNode
{
    public StatementSet Statements { get; } = statements;
    public Scope GlobalScope { get; } = new();

    public VoxValue Run()
    {
        var currentScope = GlobalScope;
        return Statements.Execute(currentScope);
    }
}

public class AstBuilder : VoxScriptBaseVisitor<AstNode>
{
    private RootNode? _currentRoot;
    private Scope? _currentScope;
    public RootNode Build(ProgramContext tree)
    {
        _currentRoot = new RootNode((StatementSet)Visit(tree.actionSet()));
        
        return _currentRoot;
    }
    
    public static string GetLineString(ParserRuleContext tree)
    {
        return $"({tree.Start.Line}:{tree.Start.Column})";
    }
    
    public override AstNode VisitActionSet(ActionSetContext context)
    {
        List<Statement> statements = [];
        foreach (var action in context.action())
        {
            var statement = (Statement)Visit(action);
            statement.LineNumber = action.Start.Line;
            statements.Add(statement);
        }
        var set = new StatementSet(statements);

        return set;
    }

    public override AstNode VisitAction(ActionContext context)
    {
        if (context.var_define() != null)
        {
            return (VariableDeclaration)Visit(context.var_define());
        }

        if (context.var_set() != null)
        {
            return (VariableRedefinition)Visit(context.var_set());
        }

        if (context.func_call() != null)
        {
            return (FunctionCall)Visit(context.func_call());
        }

        if (context.func_define() != null)
        {
            return (FunctionDeclaration)Visit(context.func_define());
        }

        if (context.cont_if() != null)
        {
            return (IfStatement)Visit(context.cont_if());
        }

        if (context.cont_while() != null)
        {
            return (WhileStatement)Visit(context.cont_while());
        }

        if (context.cont_for() != null)
        {
            return (ForStatement)Visit(context.cont_for());
        }

        if (context.cont_return() != null)
        {
            return (ReturnStatement)Visit(context.cont_return());
        }

        if (context.cont_break() != null) return new BreakStatement();
        if (context.cont_continue() != null) return new ContinueStatement();
        
        if (context.var_arith() != null) return (ArithmeticAssignment)Visit(context.var_arith());
        if (context.var_incre() != null) return (IncrementAssignment)Visit(context.var_incre());

        throw new NotImplementedException($"{GetLineString(context)} Unknown statement");
    }

    public override AstNode VisitVar_define(Var_defineContext context)
    {
        var inst = context.var_inst();
        var nameIdent = inst.identifier();
        var name = nameIdent.ID()?.GetText();
        var type = inst.type_reference() != null ? (IdentifierExpression)Visit(inst.type_reference()) : null;

        if (name == null || nameIdent.iden_seg().Length > 0)
        {
            throw new Exception($"{GetLineString(nameIdent)} Invalid variable name");
        }
        
        return new VariableDeclaration(name, type, context.OBJ_CONST() != null, (Expression)Visit(context.expression()));
    }

    public override AstNode VisitVar_set(Var_setContext context)
    {
        var identifier = (IdentifierExpression)Visit(context.identifier());
        var value = (Expression)Visit(context.expression());
        
        return new VariableRedefinition(identifier, value);
    }

    private static Expression SimplifyExpr(Expression expression)
    {
        return ExpressionMath.CanSimplify(expression) ? ExpressionMath.Simplify(expression) : expression;
    }

    public override AstNode VisitExpression(
        ExpressionContext context)
    {
        if (context.NUMBER() != null)
        {
            return new LiteralExpression(
                double.Parse(context.NUMBER().GetText()));
        }
        if (context.STRING() != null)
        {
            return new LiteralExpression(
                context.STRING().GetText().Trim('"'));
        }
        if (context.BOOLEAN() != null)
        {
            return new LiteralExpression(
                context.BOOLEAN().GetText() == "true");
        }
        if (context.NULL() != null)
        {
            return new LiteralExpression(VoxValue.Null);
        }
        
        if (context.func_call() != null)
        {
            List<Expression> arguments = [];
            foreach (var child in context.func_call().expression())
            {
                arguments.Add((Expression)Visit(child));
            }
            
            return new CallExpression(
                (Expression)Visit(context.func_call().identifier()), arguments);
        }

        if (context.func_expr() != null)
        {
            return (FunctionExpression)Visit(context.func_expr());
        }

        if (context is { left: not null, right: not null })
        {
            var left =
                (Expression)Visit(context.left);

            var right =
                (Expression)Visit(context.right);

            var op = context.op.Text;

            return SimplifyExpr(new BinaryExpression(left, op, right));
        }

        if (context is { condition: not null, primary: not null, secondary: not null })
        {
            var condition = (Expression)Visit(context.condition);
            var primary = (Expression)Visit(context.primary);
            var secondary = (Expression)Visit(context.secondary);
            
            return new ConditionalExpression(condition, primary, secondary);
        }

        if (context.identifier() != null)
        {
            return Visit(context.identifier());
        }

        if (context.@object() != null)
        {
            return Visit(context.@object());
        }

        if (context.array() != null)
        {
            return Visit(context.array());
        }

        throw new Exception($"{GetLineString(context)} Unknown expression");
    }

    public override AstNode VisitIdentifier(IdentifierContext context)
    {
        List<Expression> path = [];
        path.Add(new LiteralExpression(context.ID().GetText()));
        foreach (var child in context.iden_seg())
        {
            var text = child.iden_seg_text();
            if (text != null)
            {
                path.Add(new LiteralExpression(text.GetText()[1..]));
            }

            var expr = child.iden_seg_expr();
            if (expr != null)
            {
                path.Add((Expression)Visit(expr.expression()));
            }
        }
        return new IdentifierExpression(path);
    }

    public override AstNode VisitFunc_call(Func_callContext context)
    {
        List<Expression> arguments = [];
        foreach (ExpressionContext expressionContext in context.expression())
        {
            arguments.Add((Expression)Visit(expressionContext));
        }
        FunctionCall call = new FunctionCall((IdentifierExpression)Visit(context.identifier()), arguments);
        return call;
    }

    public override AstNode VisitFunc_define(Func_defineContext context)
    {
        var id = context.identifier();
        var parameters = context.var_inst();
        var body = (StatementSet)Visit(context.actionSet());

        List<IdentifierExpression> paramList = [];
        foreach (var param in parameters)
        {
            var name = param.identifier();
            
            paramList.Add((IdentifierExpression)Visit(name));
        }

        FunctionDeclaration declaration = new FunctionDeclaration((IdentifierExpression)Visit(id), paramList, body);
        return declaration;
    }

    public override AstNode VisitCont_if(Cont_ifContext context)
    {
        var cond = (Expression)Visit(context.expression());
        StatementSet body;
        if (context.actionSet() != null) 
        {
            body = (StatementSet)Visit(context.actionSet());
        }
        else
        {
            body = new StatementSet([(Statement)Visit(context.action())]);
        }

        var elseBody = context.cont_else() != null ? (StatementSet)Visit(context.cont_else()) : null;
        
        return new IfStatement(cond, body, elseBody);
    }

    public override AstNode VisitCont_else(Cont_elseContext context)
    {
        if (context.actionSet() != null) return Visit(context.actionSet());
        return new StatementSet([(Statement)Visit(context.action())]);
    }

    public override AstNode VisitCont_while(Cont_whileContext context)
    {
        var cond = (Expression)Visit(context.expression());
        
        StatementSet body;
        if (context.actionSet() != null) 
        {
            body = (StatementSet)Visit(context.actionSet());
        }
        else
        {
            body = new StatementSet([(Statement)Visit(context.action())]);
        }
        
        return new WhileStatement(cond, body);
    }

    public override AstNode VisitCont_for(Cont_forContext context)
    {
        ForModeImpl impl = null!;
        
        if (context.for_object() != null)
        {
            var mode = context.for_object();
            impl = new ForEach(
                (IdentifierExpression)Visit(mode.identifier(0)),
                (IdentifierExpression)Visit(mode.identifier(1)),
                (Expression)Visit(mode.expression())
            );
        }
        if (context.for_repeat() != null)
        {
            var mode = context.for_repeat();
            impl = new ForRepeat(
                (IdentifierExpression)Visit(mode.identifier()),
                (Expression)Visit(mode.expression(0)),
                (Expression)Visit(mode.expression(1)),
                (Expression)Visit(mode.expression(2))
            );
        }
        if (context.for_range() != null)
        {
            var mode = context.for_range();
            impl = new ForRange(
                (IdentifierExpression)Visit(mode.identifier()),
                (Expression)Visit(mode.expression(0)),
                (Expression)Visit(mode.expression(1)),
                (Expression)Visit(mode.expression(2))
            );
        }
        
        StatementSet body;
        if (context.actionSet() != null) 
        {
            body = (StatementSet)Visit(context.actionSet());
        }
        else
        {
            body = new StatementSet([(Statement)Visit(context.action())]);
        }

        return new ForStatement(impl, body);
    }

    public override AstNode VisitObject(ObjectContext context)
    {
        var items = context.objItem();

        List<Expression> keys = [];
        List<Expression> values = [];
        foreach (var item in items)
        {
            keys.Add((Expression)Visit(item.expression(0)));
            values.Add((Expression)Visit(item.expression(1)));
        }
        
        return new ObjectExpression(keys, values);
    }

    public override AstNode VisitArray(ArrayContext context)
    {
        var items = context.expression();

        List<Expression> values = [];
        values.AddRange(items.Select(item => (Expression)Visit(item)));

        return new ArrayExpression(values);
    }

    public override AstNode VisitFunc_expr(Func_exprContext context)
    {
        List<IdentifierExpression> parameters = [];
        foreach (var iden in context.var_inst())
        {
            parameters.Add((IdentifierExpression)Visit(iden.identifier()));
        }
        
        return new FunctionExpression(parameters, (StatementSet)Visit(context.actionSet()));
    }

    public override AstNode VisitCont_return(Cont_returnContext context)
    {
        return new ReturnStatement((Expression)Visit(context.expression()));
    }

    public override AstNode VisitVar_arith(Var_arithContext context)
    {
        var identifier = (IdentifierExpression)Visit(context.identifier());
        var operation = context.ARITH_ASSIGN().GetText();
        var value = (Expression)Visit(context.expression());

        return new ArithmeticAssignment(identifier, operation, value);
    }

    public override AstNode VisitVar_incre(Var_increContext context)
    {
        var identifier = (IdentifierExpression)Visit(context.identifier());
        var negative = context.INCREMENT() == null;
        
        return new IncrementAssignment(identifier, negative);
    }
}