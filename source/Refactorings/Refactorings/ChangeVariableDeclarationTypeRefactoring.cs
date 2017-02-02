﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Analysis;
using Roslynator.CSharp.Extensions;
using Roslynator.Extensions;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ChangeVariableDeclarationTypeRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, VariableDeclarationSyntax variableDeclaration)
        {
            if (variableDeclaration.Type?.Span.Contains(context.Span) == true)
            {
                if (context.IsRefactoringEnabled(RefactoringIdentifiers.ChangeTypeAccordingToExpression))
                    await ChangeTypeAccordingToExpressionAsync(context, variableDeclaration).ConfigureAwait(false);

                if (context.IsAnyRefactoringEnabled(
                    RefactoringIdentifiers.ChangeExplicitTypeToVar,
                    RefactoringIdentifiers.ChangeVarToExplicitType))
                {
                    await ChangeTypeAsync(context, variableDeclaration).ConfigureAwait(false);
                }
            }
        }

        private static async Task ChangeTypeAccordingToExpressionAsync(
            RefactoringContext context,
            VariableDeclarationSyntax variableDeclaration)
        {
            TypeSyntax type = variableDeclaration.Type;

            if (type?.IsVar == false)
            {
                ExpressionSyntax initializerValue = variableDeclaration.SingleVariableOrDefault()?.Initializer?.Value;

                if (initializerValue != null)
                {
                    SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                    ITypeSymbol initializerTypeSymbol = semanticModel.GetTypeSymbol(initializerValue);

                    if (initializerTypeSymbol?.IsErrorType() == false)
                    {
                        ITypeSymbol typeSymbol = semanticModel.GetTypeInfo(type).ConvertedType;

                        if (!initializerTypeSymbol.Equals(typeSymbol))
                            ChangeType(context, variableDeclaration, initializerTypeSymbol, semanticModel, context.CancellationToken);
                    }
                }
            }
        }

        private static async Task ChangeTypeAsync(
            RefactoringContext context,
            VariableDeclarationSyntax variableDeclaration)
        {
            SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

            TypeAnalysisResult result = CSharpAnalysis.AnalyzeType(
                variableDeclaration,
                semanticModel,
                context.CancellationToken);

            if (result == TypeAnalysisResult.Explicit || result == TypeAnalysisResult.ExplicitButShouldBeImplicit)
            {
                if (context.IsRefactoringEnabled(RefactoringIdentifiers.ChangeExplicitTypeToVar))
                {
                    context.RegisterRefactoring(
                        "Change type to 'var'",
                        cancellationToken =>
                        {
                            return ChangeTypeRefactoring.ChangeTypeToVarAsync(
                                context.Document,
                                variableDeclaration.Type,
                                cancellationToken);
                        });
                }
            }
            else if (result == TypeAnalysisResult.Implicit || result == TypeAnalysisResult.ImplicitButShouldBeExplicit)
            {
                if (context.IsRefactoringEnabled(RefactoringIdentifiers.ChangeVarToExplicitType))
                {
                    ITypeSymbol typeSymbol = semanticModel.GetTypeSymbol(variableDeclaration.Type, context.CancellationToken);

                    ChangeType(context, variableDeclaration, typeSymbol, semanticModel, context.CancellationToken);
                }
            }
        }

        private static void ChangeType(
            RefactoringContext context,
            VariableDeclarationSyntax variableDeclaration,
            ITypeSymbol typeSymbol,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            TypeSyntax type = variableDeclaration.Type;

            if (variableDeclaration.SingleVariableOrDefault()?.Initializer?.Value != null
                && typeSymbol.IsConstructedFromTaskOfT(semanticModel))
            {
                ISymbol enclosingSymbol = semanticModel.GetEnclosingSymbol(variableDeclaration.SpanStart, cancellationToken);

                if (enclosingSymbol.IsAsyncMethod())
                {
                    ITypeSymbol typeArgument = ((INamedTypeSymbol)typeSymbol).TypeArguments[0];

                    context.RegisterRefactoring(
                        $"Change type to '{SymbolDisplay.GetMinimalDisplayString(typeArgument, type.SpanStart, semanticModel)}' and insert 'await'",
                        c => ChangeTypeAndAddAwaitAsync(context.Document, variableDeclaration, typeArgument, c));
                }
            }

            context.RegisterRefactoring(
                $"Change type to '{SymbolDisplay.GetMinimalDisplayString(typeSymbol, type.Span.Start, semanticModel)}'",
                c => ChangeTypeRefactoring.ChangeTypeAsync(context.Document, type, typeSymbol, c));
        }

        private static async Task<Document> ChangeTypeAndAddAwaitAsync(
            Document document,
            VariableDeclarationSyntax variableDeclaration,
            ITypeSymbol typeSymbol,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            TypeSyntax type = variableDeclaration.Type;

            ExpressionSyntax value = variableDeclaration.Variables[0].Initializer.Value;

            AwaitExpressionSyntax newInitializerValue = SyntaxFactory.AwaitExpression(value)
                .WithTriviaFrom(value);

            VariableDeclarationSyntax newNode = variableDeclaration.ReplaceNode(value, newInitializerValue);

            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            newNode = newNode.WithType(
                typeSymbol.ToMinimalSyntax(semanticModel, type.SpanStart).WithTriviaFrom(type));

            return await document.ReplaceNodeAsync(variableDeclaration, newNode, cancellationToken).ConfigureAwait(false);
        }
    }
}