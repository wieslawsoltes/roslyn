// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.ThrowsClause;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Features.CodeFixes.RemoveThrowsType;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ThrowsClause;

using VerifyCS = CSharpCodeFixVerifier<
    RedundantThrowsTypeAnalyzer,
    RemoveThrowsTypeCodeFixProvider>;

public sealed class RedundantThrowsTypeAnalyzerTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestRedundantDerivedType_SimpleCase()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.IO;
            
            class C
            {
                void M() throws IOException, [|FileNotFoundException|]
                {
                }
            }
            """, """
            using System;
            using System.IO;
            
            class C
            {
                void M() throws IOException
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestRedundantDerivedType_ArgumentException()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            
            class C
            {
                void M() throws Exception, [|ArgumentException|]
                {
                }
            }
            """, """
            using System;
            
            class C
            {
                void M() throws Exception
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestNonRedundantTypes_NoDiagnostic()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using System.IO;
            
            class C
            {
                void M() throws ArgumentException, IOException
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestRedundantDerivedType_MultipleBaseTypes()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.IO;
            
            class C
            {
                void M() throws IOException, ArgumentException, [|FileNotFoundException|]
                {
                }
            }
            """, """
            using System;
            using System.IO;
            
            class C
            {
                void M() throws IOException, ArgumentException
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestRedundantDerivedType_OrderDoesNotMatter()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.IO;
            
            class C
            {
                void M() throws [|FileNotFoundException|], IOException
                {
                }
            }
            """, """
            using System;
            using System.IO;
            
            class C
            {
                void M() throws IOException
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestRedundantDerivedType_MultipleRedundant()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.IO;
            
            class C
            {
                void M() throws Exception, [|ArgumentException|], [|IOException|]
                {
                }
            }
            """, """
            using System;
            using System.IO;
            
            class C
            {
                void M() throws Exception
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestRedundantDerivedType_CustomExceptions()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            
            class CustomBaseException : Exception { }
            class CustomDerivedException : CustomBaseException { }
            
            class C
            {
                void M() throws CustomBaseException, [|CustomDerivedException|]
                {
                }
            }
            """, """
            using System;
            
            class CustomBaseException : Exception { }
            class CustomDerivedException : CustomBaseException { }
            
            class C
            {
                void M() throws CustomBaseException
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestRedundantDerivedType_ThreeLevelHierarchy()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            
            class Level1 : Exception { }
            class Level2 : Level1 { }
            class Level3 : Level2 { }
            
            class C
            {
                void M() throws Level1, [|Level3|]
                {
                }
            }
            """, """
            using System;
            
            class Level1 : Exception { }
            class Level2 : Level1 { }
            class Level3 : Level2 { }
            
            class C
            {
                void M() throws Level1
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestNonRedundant_SiblingExceptions_NoDiagnostic()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            
            class Base : Exception { }
            class Derived1 : Base { }
            class Derived2 : Base { }
            
            class C
            {
                void M() throws Derived1, Derived2
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestRedundantDerivedType_ArgumentNullException()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            
            class C
            {
                void M() throws ArgumentException, [|ArgumentNullException|]
                {
                }
            }
            """, """
            using System;
            
            class C
            {
                void M() throws ArgumentException
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestSingleException_NoDiagnostic()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            
            class C
            {
                void M() throws ArgumentException
                {
                }
            }
            """);
}
