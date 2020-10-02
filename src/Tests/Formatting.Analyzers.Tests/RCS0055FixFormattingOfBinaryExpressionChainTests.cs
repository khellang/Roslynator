﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.Formatting.CodeFixes.CSharp;
using Xunit;

namespace Roslynator.Formatting.CSharp.Tests
{
    public class RCS0055FixFormattingOfBinaryExpressionChainTests : AbstractCSharpFixVerifier
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.FixFormattingOfBinaryExpressionChain;

        public override DiagnosticAnalyzer Analyzer { get; } = new FixFormattingOfBinaryExpressionChainAnalyzer();

        public override CodeFixProvider FixProvider { get; } = new FixFormattingOfBinaryExpressionChainCodeFixProvider();

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.FixFormattingOfBinaryExpressionChain)]
        public async Task Test_NotWrapped()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M() 
    {
        bool x = false, y = false, z = false;

        x = [|x && y
            && z|];
    }
}
", @"
class C
{
    void M() 
    {
        bool x = false, y = false, z = false;

        x = x
            && y
            && z;
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.FixFormattingOfBinaryExpressionChain)]
        public async Task Test_NotWrapped2()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M() 
    {
        bool x = false, y = false, z = false;

        x = [|x &&
            y && z|];
    }
}
", @"
class C
{
    void M() 
    {
        bool x = false, y = false, z = false;

        x = x &&
            y
            && z;
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.FixFormattingOfBinaryExpressionChain)]
        public async Task Test_NoIndentation()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M() 
    {
        bool x = false, y = false, z = false;

        x = [|x
&& y
&& z|];
    }
}
", @"
class C
{
    void M() 
    {
        bool x = false, y = false, z = false;

        x = x
            && y
            && z;
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.FixFormattingOfBinaryExpressionChain)]
        public async Task Test_WrongIndentation()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M() 
    {
        bool x = false, y = false, z = false;

        x = [|x
        && y
        && z|];
    }
}
", @"
class C
{
    void M() 
    {
        bool x = false, y = false, z = false;

        x = x
            && y
            && z;
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.FixFormattingOfBinaryExpressionChain)]
        public async Task TestNoDiagnostic_LeftIsMultiline()
        {
            await VerifyNoDiagnosticAsync(@"
class C
{
    void M() 
    {
        bool x = false, y = false, z = false;

        x = y
            .ToString()
            .Equals("""") && z;
    }
}
        ");
        }
    }
}