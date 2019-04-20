using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using PT.PM.Common;
using PT.PM.PhpParseTreeUst;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PT.PM.Common.Files;

namespace PT.SourceStats
{
    public class PhpInfoCollectorVisitor : IPhpParserVisitor<string>, IFileStatisticsCollector
    {
        private string Delimeter = "";

        private readonly Dictionary<string, int> classUsings = new Dictionary<string, int>();
        private readonly Dictionary<string, int> methodInvocations = new Dictionary<string, int>();
        private readonly Dictionary<string, int> includes = new Dictionary<string, int>();

        private readonly HashSet<string> excludedMethods = new HashSet<string>();
        private readonly HashSet<string> excludedClasses = new HashSet<string>();

        public ILogger Logger { get; set; }

        public PhpInfoCollectorVisitor()
        {
        }

        public FileStatistics CollectInfo(string fileName)
        {
            var lexer = new PhpAntlrLexer();
            var sourceFile = new TextFile(File.ReadAllText(fileName)) {Name = fileName};
            var tokens = lexer.GetTokens(sourceFile, out _);

            var parser = new PhpAntlrParser();
            parser.Logger = Logger;
            parser.SourceFile = sourceFile;
            PhpAntlrParseTree ust = (PhpAntlrParseTree)parser.Parse(tokens, out _);

            classUsings.Clear();
            methodInvocations.Clear();
            includes.Clear();

            Visit(ust.SyntaxTree);

            var result = new FileStatistics
            {
                FileName = fileName,
                ClassUsings = new Dictionary<string, int>(classUsings),
                MethodInvocations = new Dictionary<string, int>(methodInvocations),
                Includes = new Dictionary<string, int>(includes)
            };
            return result;
        }

        protected string DefaultResult => "";

        public string Visit(IParseTree tree)
        {
            try
            {
                return tree.Accept(this);
            }
            catch
            {
                if (tree is ParserRuleContext)
                {
                    //AntlrHelper.LogConversionError(ex, parserRuleContext, FileNode.FileName.Text, FileNode.FileData, Logger);
                }
                return DefaultResult;
            }
        }

        public string VisitChildren(IRuleNode node)
        {
            var result = new StringBuilder();
            for (int i = 0; i < node.ChildCount; i++)
            {
                result.Append(Visit(node.GetChild(i)));
                if (i != node.ChildCount - 1)
                {
                    result.Append(Delimeter);
                }
            }
            return result.ToString();
        }

        public string VisitTerminal(ITerminalNode node)
        {
            string text = node.GetText();
            string result;
            if (text == null)
            {
                return "";
            }

            if (text.StartsWith("$"))
            {
                result = text.Substring(1);
            }
            else if ((text.StartsWith("'") || text.StartsWith("\"")) && text.Length > 1)
            {
                result = text.Substring(1, text.Length - 2).ToLowerInvariant();
            }
            else
            {
                result = text.ToLowerInvariant();
            }

            return result;
        }

        public string VisitErrorNode(IErrorNode node)
        {
            return "";
        }

        public string VisitUseDeclaration([NotNull] PhpParser.UseDeclarationContext context)
        {
            if (context.Function() != null)
            {
                var list = context.useDeclarationContentList();
                for (int i = 0; i < list.useDeclarationContent().Length; i++)
                {
                    var functionName = Visit(list.useDeclarationContent(i));
                    if (!excludedMethods.Contains(functionName))
                    {
                        methodInvocations.TryGetValue(functionName, out int count);
                        methodInvocations[functionName] = count + 1;
                    }
                    else
                    {
                        methodInvocations.Remove(functionName);
                    }
                }
            }

            return VisitChildren(context);
        }

        public string VisitUseDeclarationContent([NotNull] PhpParser.UseDeclarationContentContext context)
        {
            return Visit(context.namespaceNameList());
        }

        public string VisitClassDeclaration([NotNull] PhpParser.ClassDeclarationContext context)
        {
            var id = Visit(context.identifier());
            excludedClasses.Add(id);
            classUsings.Remove(id);

            return VisitChildren(context);
        }

        public string VisitClassStatement([NotNull] PhpParser.ClassStatementContext context)
        {
            if (context.Function() != null)
            {
                var id = Visit(context.identifier());
                excludedMethods.Add(id);
            }

            return VisitChildren(context);
        }

        public string VisitParenthesis([NotNull] PhpParser.ParenthesisContext context)
        {
            return Visit(context.GetChild(1));
        }

        public string VisitNewExpr([NotNull] PhpParser.NewExprContext context)
        {
            var typeRef = Visit(context.typeRef());
            if (!excludedClasses.Contains(typeRef))
            {
                classUsings.TryGetValue(typeRef, out int count);
                classUsings[typeRef] = count + 1;
            }

            return Visit(context.New()) + Delimeter + typeRef + Delimeter +
                (context.arguments() != null ? Visit(context.arguments()) : "");
        }

        public string VisitFunctionDeclaration([NotNull] PhpParser.FunctionDeclarationContext context)
        {
            var id = Visit(context.identifier());
            excludedMethods.Add(id);
            methodInvocations.Remove(id);

            return VisitChildren(context);
        }

        public string VisitMemberAccess([NotNull] PhpParser.MemberAccessContext context)
        {
            var functionName = Visit(context.keyedFieldName());

            if (!excludedMethods.Contains(functionName))
            {
                methodInvocations.TryGetValue(functionName, out int count);
                methodInvocations[functionName] = count + 1;
            }
            else
            {
                methodInvocations.Remove(functionName);
            }

            return VisitChildren(context);
        }

        public string VisitFunctionCall([NotNull] PhpParser.FunctionCallContext context)
        {
            var functionName = Visit(context.functionCallName());
            var argsStr = Visit(context.actualArguments());

            if (functionName == "include" || functionName == "require_once" || functionName == "include" || functionName == "include_once")
            {
                includes.TryGetValue(argsStr, out int count);
                includes[argsStr] = count + 1;
            }
            if (!excludedMethods.Contains(functionName))
            {
                methodInvocations.TryGetValue(functionName, out int count);
                methodInvocations[functionName] = count + 1;
            }
            else
            {
                methodInvocations.Remove(functionName);
            }

            return functionName + Delimeter + argsStr;
        }

        public string VisitBaseCtorCall([NotNull] PhpParser.BaseCtorCallContext context)
        {
            var className = Visit(context.identifier());
            if (!excludedClasses.Contains(className))
            {
                classUsings.TryGetValue(className, out int count);
                classUsings[className] = count + 1;
            }

            return ":" + Delimeter + className + Visit(context.arguments());
        }

        public string VisitSpecialWordExpression([NotNull] PhpParser.SpecialWordExpressionContext context)
        {
            string result;
            if (context.Require() != null || context.RequireOnce() != null || context.Include() != null || context.IncludeOnce() != null)
            {
                string includeName = Visit(context.expression());
                includes.TryGetValue(includeName, out int count);
                includes[includeName] = count + 1;

                result = Visit(context.GetChild(0)) + Delimeter + includeName;
            }
            else
            {
                result = VisitChildren(context);
            }
            return result;
        }

        public string VisitChainExpression([NotNull] PhpParser.ChainExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitUnaryOperatorExpression([NotNull] PhpParser.UnaryOperatorExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitArrayCreationExpression([NotNull] PhpParser.ArrayCreationExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitNewExpression([NotNull] PhpParser.NewExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitParenthesisExpression([NotNull] PhpParser.ParenthesisExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitBackQuoteStringExpression([NotNull] PhpParser.BackQuoteStringExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitIndexerExpression([NotNull] PhpParser.IndexerExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitScalarExpression([NotNull] PhpParser.ScalarExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPrefixIncDecExpression([NotNull] PhpParser.PrefixIncDecExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPrintExpression([NotNull] PhpParser.PrintExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAssignmentExpression([NotNull] PhpParser.AssignmentExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPostfixIncDecExpression([NotNull] PhpParser.PostfixIncDecExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitCastExpression([NotNull] PhpParser.CastExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitLambdaFunctionExpression([NotNull] PhpParser.LambdaFunctionExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitCloneExpression([NotNull] PhpParser.CloneExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitHtmlDocument([NotNull] PhpParser.HtmlDocumentContext context)
        {
            return VisitChildren(context);
        }

        public string VisitHtmlElementOrPhpBlock([NotNull] PhpParser.HtmlElementOrPhpBlockContext context)
        {
            return VisitChildren(context);
        }

        public string VisitHtmlElements([NotNull] PhpParser.HtmlElementsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitHtmlElement([NotNull] PhpParser.HtmlElementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitScriptTextPart([NotNull] PhpParser.ScriptTextPartContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPhpBlock([NotNull] PhpParser.PhpBlockContext context)
        {
            return VisitChildren(context);
        }

        public string VisitImportStatement([NotNull] PhpParser.ImportStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTopStatement([NotNull] PhpParser.TopStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitUseDeclarationContentList([NotNull] PhpParser.UseDeclarationContentListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitNamespaceDeclaration([NotNull] PhpParser.NamespaceDeclarationContext context)
        {
            return VisitChildren(context);
        }

        public string VisitNamespaceStatement([NotNull] PhpParser.NamespaceStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitClassEntryType([NotNull] PhpParser.ClassEntryTypeContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInterfaceList([NotNull] PhpParser.InterfaceListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeParameterListInBrackets([NotNull] PhpParser.TypeParameterListInBracketsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeParameterList([NotNull] PhpParser.TypeParameterListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeParameterWithDefaultsList([NotNull] PhpParser.TypeParameterWithDefaultsListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeParameterDecl([NotNull] PhpParser.TypeParameterDeclContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeParameterWithDefaultDecl([NotNull] PhpParser.TypeParameterWithDefaultDeclContext context)
        {
            return VisitChildren(context);
        }

        public string VisitGenericDynamicArgs([NotNull] PhpParser.GenericDynamicArgsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttributes([NotNull] PhpParser.AttributesContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttributesGroup([NotNull] PhpParser.AttributesGroupContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttribute([NotNull] PhpParser.AttributeContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttributeArgList([NotNull] PhpParser.AttributeArgListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttributeNamedArgList([NotNull] PhpParser.AttributeNamedArgListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttributeNamedArg([NotNull] PhpParser.AttributeNamedArgContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInnerStatementList([NotNull] PhpParser.InnerStatementListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInnerStatement([NotNull] PhpParser.InnerStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitStatement([NotNull] PhpParser.StatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitEmptyStatement([NotNull] PhpParser.EmptyStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitBlockStatement([NotNull] PhpParser.BlockStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitIfStatement([NotNull] PhpParser.IfStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitElseIfStatement([NotNull] PhpParser.ElseIfStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitElseIfColonStatement([NotNull] PhpParser.ElseIfColonStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitElseStatement([NotNull] PhpParser.ElseStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitElseColonStatement([NotNull] PhpParser.ElseColonStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitWhileStatement([NotNull] PhpParser.WhileStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitDoWhileStatement([NotNull] PhpParser.DoWhileStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitForStatement([NotNull] PhpParser.ForStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitForInit([NotNull] PhpParser.ForInitContext context)
        {
            return VisitChildren(context);
        }

        public string VisitForUpdate([NotNull] PhpParser.ForUpdateContext context)
        {
            return VisitChildren(context);
        }

        public string VisitSwitchStatement([NotNull] PhpParser.SwitchStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitSwitchBlock([NotNull] PhpParser.SwitchBlockContext context)
        {
            return VisitChildren(context);
        }

        public string VisitBreakStatement([NotNull] PhpParser.BreakStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitContinueStatement([NotNull] PhpParser.ContinueStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitReturnStatement([NotNull] PhpParser.ReturnStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitExpressionStatement([NotNull] PhpParser.ExpressionStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitUnsetStatement([NotNull] PhpParser.UnsetStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitForeachStatement([NotNull] PhpParser.ForeachStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTryCatchFinally([NotNull] PhpParser.TryCatchFinallyContext context)
        {
            return VisitChildren(context);
        }

        public string VisitCatchClause([NotNull] PhpParser.CatchClauseContext context)
        {
            return VisitChildren(context);
        }

        public string VisitFinallyStatement([NotNull] PhpParser.FinallyStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitThrowStatement([NotNull] PhpParser.ThrowStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitGotoStatement([NotNull] PhpParser.GotoStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitDeclareStatement([NotNull] PhpParser.DeclareStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInlineHtml([NotNull] PhpParser.InlineHtmlContext context)
        {
            return VisitChildren(context);
        }

        public string VisitDeclareList([NotNull] PhpParser.DeclareListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitFormalParameterList([NotNull] PhpParser.FormalParameterListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitFormalParameter([NotNull] PhpParser.FormalParameterContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeHint([NotNull] PhpParser.TypeHintContext context)
        {
            return VisitChildren(context);
        }

        public string VisitGlobalStatement([NotNull] PhpParser.GlobalStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitGlobalVar([NotNull] PhpParser.GlobalVarContext context)
        {
            return VisitChildren(context);
        }

        public string VisitEchoStatement([NotNull] PhpParser.EchoStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitStaticVariableStatement([NotNull] PhpParser.StaticVariableStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTraitAdaptations([NotNull] PhpParser.TraitAdaptationsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTraitAdaptationStatement([NotNull] PhpParser.TraitAdaptationStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTraitPrecedence([NotNull] PhpParser.TraitPrecedenceContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTraitAlias([NotNull] PhpParser.TraitAliasContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTraitMethodReference([NotNull] PhpParser.TraitMethodReferenceContext context)
        {
            return VisitChildren(context);
        }

        public string VisitMethodBody([NotNull] PhpParser.MethodBodyContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPropertyModifiers([NotNull] PhpParser.PropertyModifiersContext context)
        {
            return VisitChildren(context);
        }

        public string VisitMemberModifiers([NotNull] PhpParser.MemberModifiersContext context)
        {
            return VisitChildren(context);
        }

        public string VisitVariableInitializer([NotNull] PhpParser.VariableInitializerContext context)
        {
            return VisitChildren(context);
        }

        public string VisitIdentifierInititalizer([NotNull] PhpParser.IdentifierInititalizerContext context)
        {
            return VisitChildren(context);
        }

        public string VisitGlobalConstantDeclaration([NotNull] PhpParser.GlobalConstantDeclarationContext context)
        {
            return VisitChildren(context);
        }

        public string VisitExpressionList([NotNull] PhpParser.ExpressionListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitExpression([NotNull] PhpParser.ExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitComparisonExpression([NotNull] PhpParser.ComparisonExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAssignmentOperator([NotNull] PhpParser.AssignmentOperatorContext context)
        {
            return VisitChildren(context);
        }

        public string VisitYieldExpression([NotNull] PhpParser.YieldExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitArrayItemList([NotNull] PhpParser.ArrayItemListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitArrayItem([NotNull] PhpParser.ArrayItemContext context)
        {
            return VisitChildren(context);
        }

        public string VisitLambdaFunctionUseVars([NotNull] PhpParser.LambdaFunctionUseVarsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitLambdaFunctionUseVar([NotNull] PhpParser.LambdaFunctionUseVarContext context)
        {
            return VisitChildren(context);
        }

        public string VisitQualifiedStaticTypeRef([NotNull] PhpParser.QualifiedStaticTypeRefContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeRef([NotNull] PhpParser.TypeRefContext context)
        {
            return VisitChildren(context);
        }

        public string VisitIndirectTypeRef([NotNull] PhpParser.IndirectTypeRefContext context)
        {
            return VisitChildren(context);
        }

        public string VisitQualifiedNamespaceName([NotNull] PhpParser.QualifiedNamespaceNameContext context)
        {
            return VisitChildren(context);
        }

        public string VisitNamespaceNameList([NotNull] PhpParser.NamespaceNameListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitQualifiedNamespaceNameList([NotNull] PhpParser.QualifiedNamespaceNameListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitArguments([NotNull] PhpParser.ArgumentsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitActualArgument([NotNull] PhpParser.ActualArgumentContext context)
        {
            return VisitChildren(context);
        }

        public string VisitConstantInititalizer([NotNull] PhpParser.ConstantInititalizerContext context)
        {
            return VisitChildren(context);
        }

        public string VisitConstantArrayItemList([NotNull] PhpParser.ConstantArrayItemListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitConstantArrayItem([NotNull] PhpParser.ConstantArrayItemContext context)
        {
            return VisitChildren(context);
        }

        public string VisitConstant([NotNull] PhpParser.ConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitLiteralConstant([NotNull] PhpParser.LiteralConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitNumericConstant([NotNull] PhpParser.NumericConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitClassConstant([NotNull] PhpParser.ClassConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitStringConstant([NotNull] PhpParser.StringConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitString([NotNull] PhpParser.StringContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInterpolatedStringPart([NotNull] PhpParser.InterpolatedStringPartContext context)
        {
            return VisitChildren(context);
        }

        public string VisitChainList([NotNull] PhpParser.ChainListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitChain([NotNull] PhpParser.ChainContext context)
        {
            return VisitChildren(context);
        }

        public string VisitFunctionCallName([NotNull] PhpParser.FunctionCallNameContext context)
        {
            return VisitChildren(context);
        }

        public string VisitActualArguments([NotNull] PhpParser.ActualArgumentsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitChainBase([NotNull] PhpParser.ChainBaseContext context)
        {
            return VisitChildren(context);
        }

        public string VisitKeyedFieldName([NotNull] PhpParser.KeyedFieldNameContext context)
        {
            return VisitChildren(context);
        }

        public string VisitKeyedSimpleFieldName([NotNull] PhpParser.KeyedSimpleFieldNameContext context)
        {
            return VisitChildren(context);
        }

        public string VisitKeyedVariable([NotNull] PhpParser.KeyedVariableContext context)
        {
            return VisitChildren(context);
        }

        public string VisitSquareCurlyExpression([NotNull] PhpParser.SquareCurlyExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAssignmentList([NotNull] PhpParser.AssignmentListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAssignmentListElement([NotNull] PhpParser.AssignmentListElementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitModifier([NotNull] PhpParser.ModifierContext context)
        {
            return VisitChildren(context);
        }

        public string VisitIdentifier([NotNull] PhpParser.IdentifierContext context)
        {
            return VisitChildren(context);
        }

        public string VisitMemberModifier([NotNull] PhpParser.MemberModifierContext context)
        {
            return VisitChildren(context);
        }

        public string VisitMagicConstant([NotNull] PhpParser.MagicConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitMagicMethod([NotNull] PhpParser.MagicMethodContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPrimitiveType([NotNull] PhpParser.PrimitiveTypeContext context)
        {
            return VisitChildren(context);
        }

        public string VisitCastOperation([NotNull] PhpParser.CastOperationContext context)
        {
            return VisitChildren(context);
        }

        public string VisitConditionalExpression([NotNull] PhpParser.ConditionalExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitArithmeticExpression([NotNull] PhpParser.ArithmeticExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitLogicalExpression([NotNull] PhpParser.LogicalExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInstanceOfExpression([NotNull] PhpParser.InstanceOfExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitBitwiseExpression([NotNull] PhpParser.BitwiseExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInlineHtmlStatement([NotNull] PhpParser.InlineHtmlStatementContext context)
        {
            return VisitChildren(context);
        }
    }
}
