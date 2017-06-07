using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using PT.PM.Common;
using PT.PM.PhpParseTreeUst;
using PT.PM.UstParsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PT.SourceStats
{
    public class PhpInfoCollectorVisitor : IPHPParserVisitor<string>, IFileStatisticsCollector
    {
        private string Delimeter = "";

        private Dictionary<string, int> classUsings = new Dictionary<string, int>();
        private Dictionary<string, int> methodInvocations = new Dictionary<string, int>();
        private Dictionary<string, int> includes = new Dictionary<string, int>();

        private HashSet<string> excludedMethods = new HashSet<string>();
        private HashSet<string> excludedClasses = new HashSet<string>();

        public ILogger Logger { get; set; }

        public PhpInfoCollectorVisitor()
        {
        }

        public FileStatistics CollectInfo(string fileName)
        {
            var parser = new PhpAntlrParser();
            parser.Logger = Logger;
            var sourceCode = File.ReadAllText(fileName);

            PhpAntlrParseTree ust = (PhpAntlrParseTree)parser.Parse(new SourceCodeFile(fileName) { Code = sourceCode });

            classUsings.Clear();
            methodInvocations.Clear();
            includes.Clear();

            var str = Visit(ust.SyntaxTree);

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
            catch (Exception ex)
            {
                var parserRuleContext = tree as ParserRuleContext;
                if (parserRuleContext != null)
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
            else if (text.StartsWith("$"))
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

        public string VisitUseDeclaration([NotNull] PHPParser.UseDeclarationContext context)
        {
            if (context.Function() != null)
            {
                var list = context.useDeclarationContentList();
                for (int i = 0; i < list.useDeclarationContent().Length; i++)
                {
                    var functionName = Visit(list.useDeclarationContent(i));
                    if (!excludedMethods.Contains(functionName))
                    {
                        int count = 0;
                        methodInvocations.TryGetValue(functionName, out count);
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

        public string VisitUseDeclarationContent([NotNull] PHPParser.UseDeclarationContentContext context)
        {
            return Visit(context.namespaceNameList());
        }

        public string VisitClassDeclaration([NotNull] PHPParser.ClassDeclarationContext context)
        {
            var id = Visit(context.identifier());
            excludedClasses.Add(id);
            classUsings.Remove(id);

            return VisitChildren(context);
        }

        public string VisitClassStatement([NotNull] PHPParser.ClassStatementContext context)
        {
            if (context.Function() != null)
            {
                var id = Visit(context.identifier());
                excludedMethods.Add(id);
            }

            return VisitChildren(context);
        }

        public string VisitParenthesis([NotNull] PHPParser.ParenthesisContext context)
        {
            return Visit(context.GetChild(1));
        }

        public string VisitNewExpr([NotNull] PHPParser.NewExprContext context)
        {
            var typeRef = Visit(context.typeRef());
            if (!excludedClasses.Contains(typeRef))
            {
                int count = 0;
                classUsings.TryGetValue(typeRef, out count);
                classUsings[typeRef] = count + 1;
            }

            return Visit(context.New()) + Delimeter + typeRef + Delimeter +
                (context.arguments() != null ? Visit(context.arguments()) : "");
        }

        public string VisitFunctionDeclaration([NotNull] PHPParser.FunctionDeclarationContext context)
        {
            var id = Visit(context.identifier());
            excludedMethods.Add(id);
            methodInvocations.Remove(id);

            return VisitChildren(context);
        }

        public string VisitMemberAccess([NotNull] PHPParser.MemberAccessContext context)
        {
            var functionName = Visit(context.keyedFieldName());

            if (!excludedMethods.Contains(functionName))
            {
                int count = 0;
                methodInvocations.TryGetValue(functionName, out count);
                methodInvocations[functionName] = count + 1;
            }
            else
            {
                methodInvocations.Remove(functionName);
            }

            return VisitChildren(context);
        }

        public string VisitFunctionCall([NotNull] PHPParser.FunctionCallContext context)
        {
            var functionName = Visit(context.functionCallName());
            var argsStr = Visit(context.actualArguments());

            if (functionName == "include" || functionName == "require_once" || functionName == "include" || functionName == "include_once")
            {
                int count = 0;
                includes.TryGetValue(argsStr, out count);
                includes[argsStr] = count + 1;
            }
            if (!excludedMethods.Contains(functionName))
            {
                int count = 0;
                methodInvocations.TryGetValue(functionName, out count);
                methodInvocations[functionName] = count + 1;
            }
            else
            {
                methodInvocations.Remove(functionName);
            }

            return functionName + Delimeter + argsStr;
        }

        public string VisitBaseCtorCall([NotNull] PHPParser.BaseCtorCallContext context)
        {
            var className = Visit(context.identifier());
            if (!excludedClasses.Contains(className))
            {
                int count = 0;
                classUsings.TryGetValue(className, out count);
                classUsings[className] = count + 1;
            }

            return ":" + Delimeter + className + Visit(context.arguments());
        }

        public string VisitSpecialWordExpression([NotNull] PHPParser.SpecialWordExpressionContext context)
        {
            string result;
            if (context.Require() != null || context.RequireOnce() != null || context.Include() != null || context.IncludeOnce() != null)
            {
                int count = 0;
                string includeName = Visit(context.expression());
                includes.TryGetValue(includeName, out count);
                includes[includeName] = count + 1;

                result = Visit(context.GetChild(0)) + Delimeter + includeName;
            }
            else
            {
                result = VisitChildren(context);
            }
            return result;
        }

        public string VisitChainExpression([NotNull] PHPParser.ChainExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitUnaryOperatorExpression([NotNull] PHPParser.UnaryOperatorExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitArrayCreationExpression([NotNull] PHPParser.ArrayCreationExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitNewExpression([NotNull] PHPParser.NewExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitParenthesisExpression([NotNull] PHPParser.ParenthesisExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitBackQuoteStringExpression([NotNull] PHPParser.BackQuoteStringExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitIndexerExpression([NotNull] PHPParser.IndexerExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitScalarExpression([NotNull] PHPParser.ScalarExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPrefixIncDecExpression([NotNull] PHPParser.PrefixIncDecExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPrintExpression([NotNull] PHPParser.PrintExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAssignmentExpression([NotNull] PHPParser.AssignmentExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPostfixIncDecExpression([NotNull] PHPParser.PostfixIncDecExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitCastExpression([NotNull] PHPParser.CastExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitLambdaFunctionExpression([NotNull] PHPParser.LambdaFunctionExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitCloneExpression([NotNull] PHPParser.CloneExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitHtmlDocument([NotNull] PHPParser.HtmlDocumentContext context)
        {
            return VisitChildren(context);
        }

        public string VisitHtmlElementOrPhpBlock([NotNull] PHPParser.HtmlElementOrPhpBlockContext context)
        {
            return VisitChildren(context);
        }

        public string VisitHtmlElements([NotNull] PHPParser.HtmlElementsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitHtmlElement([NotNull] PHPParser.HtmlElementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitScriptTextPart([NotNull] PHPParser.ScriptTextPartContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPhpBlock([NotNull] PHPParser.PhpBlockContext context)
        {
            return VisitChildren(context);
        }

        public string VisitImportStatement([NotNull] PHPParser.ImportStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTopStatement([NotNull] PHPParser.TopStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitUseDeclarationContentList([NotNull] PHPParser.UseDeclarationContentListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitNamespaceDeclaration([NotNull] PHPParser.NamespaceDeclarationContext context)
        {
            return VisitChildren(context);
        }

        public string VisitNamespaceStatement([NotNull] PHPParser.NamespaceStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitClassEntryType([NotNull] PHPParser.ClassEntryTypeContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInterfaceList([NotNull] PHPParser.InterfaceListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeParameterListInBrackets([NotNull] PHPParser.TypeParameterListInBracketsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeParameterList([NotNull] PHPParser.TypeParameterListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeParameterWithDefaultsList([NotNull] PHPParser.TypeParameterWithDefaultsListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeParameterDecl([NotNull] PHPParser.TypeParameterDeclContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeParameterWithDefaultDecl([NotNull] PHPParser.TypeParameterWithDefaultDeclContext context)
        {
            return VisitChildren(context);
        }

        public string VisitGenericDynamicArgs([NotNull] PHPParser.GenericDynamicArgsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttributes([NotNull] PHPParser.AttributesContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttributesGroup([NotNull] PHPParser.AttributesGroupContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttribute([NotNull] PHPParser.AttributeContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttributeArgList([NotNull] PHPParser.AttributeArgListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttributeNamedArgList([NotNull] PHPParser.AttributeNamedArgListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAttributeNamedArg([NotNull] PHPParser.AttributeNamedArgContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInnerStatementList([NotNull] PHPParser.InnerStatementListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInnerStatement([NotNull] PHPParser.InnerStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitStatement([NotNull] PHPParser.StatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitEmptyStatement([NotNull] PHPParser.EmptyStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitNonEmptyStatement([NotNull] PHPParser.NonEmptyStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitBlockStatement([NotNull] PHPParser.BlockStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitIfStatement([NotNull] PHPParser.IfStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitElseIfStatement([NotNull] PHPParser.ElseIfStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitElseIfColonStatement([NotNull] PHPParser.ElseIfColonStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitElseStatement([NotNull] PHPParser.ElseStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitElseColonStatement([NotNull] PHPParser.ElseColonStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitWhileStatement([NotNull] PHPParser.WhileStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitDoWhileStatement([NotNull] PHPParser.DoWhileStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitForStatement([NotNull] PHPParser.ForStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitForInit([NotNull] PHPParser.ForInitContext context)
        {
            return VisitChildren(context);
        }

        public string VisitForUpdate([NotNull] PHPParser.ForUpdateContext context)
        {
            return VisitChildren(context);
        }

        public string VisitSwitchStatement([NotNull] PHPParser.SwitchStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitSwitchBlock([NotNull] PHPParser.SwitchBlockContext context)
        {
            return VisitChildren(context);
        }

        public string VisitBreakStatement([NotNull] PHPParser.BreakStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitContinueStatement([NotNull] PHPParser.ContinueStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitReturnStatement([NotNull] PHPParser.ReturnStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitExpressionStatement([NotNull] PHPParser.ExpressionStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitUnsetStatement([NotNull] PHPParser.UnsetStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitForeachStatement([NotNull] PHPParser.ForeachStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTryCatchFinally([NotNull] PHPParser.TryCatchFinallyContext context)
        {
            return VisitChildren(context);
        }

        public string VisitCatchClause([NotNull] PHPParser.CatchClauseContext context)
        {
            return VisitChildren(context);
        }

        public string VisitFinallyStatement([NotNull] PHPParser.FinallyStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitThrowStatement([NotNull] PHPParser.ThrowStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitGotoStatement([NotNull] PHPParser.GotoStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitDeclareStatement([NotNull] PHPParser.DeclareStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInlineHtml([NotNull] PHPParser.InlineHtmlContext context)
        {
            return VisitChildren(context);
        }

        public string VisitDeclareList([NotNull] PHPParser.DeclareListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitFormalParameterList([NotNull] PHPParser.FormalParameterListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitFormalParameter([NotNull] PHPParser.FormalParameterContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeHint([NotNull] PHPParser.TypeHintContext context)
        {
            return VisitChildren(context);
        }

        public string VisitGlobalStatement([NotNull] PHPParser.GlobalStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitGlobalVar([NotNull] PHPParser.GlobalVarContext context)
        {
            return VisitChildren(context);
        }

        public string VisitEchoStatement([NotNull] PHPParser.EchoStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitStaticVariableStatement([NotNull] PHPParser.StaticVariableStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTraitAdaptations([NotNull] PHPParser.TraitAdaptationsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTraitAdaptationStatement([NotNull] PHPParser.TraitAdaptationStatementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTraitPrecedence([NotNull] PHPParser.TraitPrecedenceContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTraitAlias([NotNull] PHPParser.TraitAliasContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTraitMethodReference([NotNull] PHPParser.TraitMethodReferenceContext context)
        {
            return VisitChildren(context);
        }

        public string VisitMethodBody([NotNull] PHPParser.MethodBodyContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPropertyModifiers([NotNull] PHPParser.PropertyModifiersContext context)
        {
            return VisitChildren(context);
        }

        public string VisitMemberModifiers([NotNull] PHPParser.MemberModifiersContext context)
        {
            return VisitChildren(context);
        }

        public string VisitVariableInitializer([NotNull] PHPParser.VariableInitializerContext context)
        {
            return VisitChildren(context);
        }

        public string VisitIdentifierInititalizer([NotNull] PHPParser.IdentifierInititalizerContext context)
        {
            return VisitChildren(context);
        }

        public string VisitGlobalConstantDeclaration([NotNull] PHPParser.GlobalConstantDeclarationContext context)
        {
            return VisitChildren(context);
        }

        public string VisitExpressionList([NotNull] PHPParser.ExpressionListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitExpression([NotNull] PHPParser.ExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitComparisonExpression([NotNull] PHPParser.ComparisonExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAssignmentOperator([NotNull] PHPParser.AssignmentOperatorContext context)
        {
            return VisitChildren(context);
        }

        public string VisitYieldExpression([NotNull] PHPParser.YieldExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitArrayItemList([NotNull] PHPParser.ArrayItemListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitArrayItem([NotNull] PHPParser.ArrayItemContext context)
        {
            return VisitChildren(context);
        }

        public string VisitLambdaFunctionUseVars([NotNull] PHPParser.LambdaFunctionUseVarsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitLambdaFunctionUseVar([NotNull] PHPParser.LambdaFunctionUseVarContext context)
        {
            return VisitChildren(context);
        }

        public string VisitQualifiedStaticTypeRef([NotNull] PHPParser.QualifiedStaticTypeRefContext context)
        {
            return VisitChildren(context);
        }

        public string VisitTypeRef([NotNull] PHPParser.TypeRefContext context)
        {
            return VisitChildren(context);
        }

        public string VisitIndirectTypeRef([NotNull] PHPParser.IndirectTypeRefContext context)
        {
            return VisitChildren(context);
        }

        public string VisitQualifiedNamespaceName([NotNull] PHPParser.QualifiedNamespaceNameContext context)
        {
            return VisitChildren(context);
        }

        public string VisitNamespaceNameList([NotNull] PHPParser.NamespaceNameListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitQualifiedNamespaceNameList([NotNull] PHPParser.QualifiedNamespaceNameListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitArguments([NotNull] PHPParser.ArgumentsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitActualArgument([NotNull] PHPParser.ActualArgumentContext context)
        {
            return VisitChildren(context);
        }

        public string VisitConstantInititalizer([NotNull] PHPParser.ConstantInititalizerContext context)
        {
            return VisitChildren(context);
        }

        public string VisitConstantArrayItemList([NotNull] PHPParser.ConstantArrayItemListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitConstantArrayItem([NotNull] PHPParser.ConstantArrayItemContext context)
        {
            return VisitChildren(context);
        }

        public string VisitConstant([NotNull] PHPParser.ConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitLiteralConstant([NotNull] PHPParser.LiteralConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitNumericConstant([NotNull] PHPParser.NumericConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitClassConstant([NotNull] PHPParser.ClassConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitStringConstant([NotNull] PHPParser.StringConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitString([NotNull] PHPParser.StringContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInterpolatedStringPart([NotNull] PHPParser.InterpolatedStringPartContext context)
        {
            return VisitChildren(context);
        }

        public string VisitChainList([NotNull] PHPParser.ChainListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitChain([NotNull] PHPParser.ChainContext context)
        {
            return VisitChildren(context);
        }

        public string VisitFunctionCallName([NotNull] PHPParser.FunctionCallNameContext context)
        {
            return VisitChildren(context);
        }

        public string VisitActualArguments([NotNull] PHPParser.ActualArgumentsContext context)
        {
            return VisitChildren(context);
        }

        public string VisitChainBase([NotNull] PHPParser.ChainBaseContext context)
        {
            return VisitChildren(context);
        }

        public string VisitKeyedFieldName([NotNull] PHPParser.KeyedFieldNameContext context)
        {
            return VisitChildren(context);
        }

        public string VisitKeyedSimpleFieldName([NotNull] PHPParser.KeyedSimpleFieldNameContext context)
        {
            return VisitChildren(context);
        }

        public string VisitKeyedVariable([NotNull] PHPParser.KeyedVariableContext context)
        {
            return VisitChildren(context);
        }

        public string VisitSquareCurlyExpression([NotNull] PHPParser.SquareCurlyExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAssignmentList([NotNull] PHPParser.AssignmentListContext context)
        {
            return VisitChildren(context);
        }

        public string VisitAssignmentListElement([NotNull] PHPParser.AssignmentListElementContext context)
        {
            return VisitChildren(context);
        }

        public string VisitModifier([NotNull] PHPParser.ModifierContext context)
        {
            return VisitChildren(context);
        }

        public string VisitIdentifier([NotNull] PHPParser.IdentifierContext context)
        {
            return VisitChildren(context);
        }

        public string VisitMemberModifier([NotNull] PHPParser.MemberModifierContext context)
        {
            return VisitChildren(context);
        }

        public string VisitMagicConstant([NotNull] PHPParser.MagicConstantContext context)
        {
            return VisitChildren(context);
        }

        public string VisitMagicMethod([NotNull] PHPParser.MagicMethodContext context)
        {
            return VisitChildren(context);
        }

        public string VisitPrimitiveType([NotNull] PHPParser.PrimitiveTypeContext context)
        {
            return VisitChildren(context);
        }

        public string VisitCastOperation([NotNull] PHPParser.CastOperationContext context)
        {
            return VisitChildren(context);
        }

        public string VisitConditionalExpression([NotNull] PHPParser.ConditionalExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitArithmeticExpression([NotNull] PHPParser.ArithmeticExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitLogicalExpression([NotNull] PHPParser.LogicalExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitInstanceOfExpression([NotNull] PHPParser.InstanceOfExpressionContext context)
        {
            return VisitChildren(context);
        }

        public string VisitBitwiseExpression([NotNull] PHPParser.BitwiseExpressionContext context)
        {
            return VisitChildren(context);
        }
    }
}
