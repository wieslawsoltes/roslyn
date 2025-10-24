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
    UnnecessaryThrowsTypeAnalyzer,
    RemoveThrowsTypeCodeFixProvider>;

public sealed class UnnecessaryThrowsTypeAnalyzerTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestUnnecessaryThrowsType_NeverThrown()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            
            class C
            {
                void M() throws [|ArgumentException|]
                {
                }
            }
            """, """
            using System;
            
            class C
            {
                void M()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestNecessaryThrowsType_IsThrown_NoDiagnostic()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            
            class C
            {
                void M() throws ArgumentException
                {
                    throw new ArgumentException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestUnnecessaryThrowsType_OnlyPartiallyThrown()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.IO;
            
            class C
            {
                void M() throws ArgumentException, [|IOException|]
                {
                    throw new ArgumentException();
                }
            }
            """, """
            using System;
            using System.IO;
            
            class C
            {
                void M() throws ArgumentException
                {
                    throw new ArgumentException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestUnnecessaryThrowsType_ThrownButCaught()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            
            class C
            {
                void M() throws [|ArgumentException|]
                {
                    try
                    {
                        throw new ArgumentException();
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
            """, """
            using System;
            
            class C
            {
                void M()
                {
                    try
                    {
                        throw new ArgumentException();
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestNecessaryThrowsType_ThrownByCalledMethod_NoDiagnostic()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            
            class C
            {
                void M() throws ArgumentException
                {
                    Helper();
                }
                
                void Helper() throws ArgumentException
                {
                    throw new ArgumentException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestUnnecessaryThrowsType_CalledMethodCatchesException()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            
            class C
            {
                void M() throws [|ArgumentException|]
                {
                    Helper();
                }
                
                void Helper()
                {
                    try
                    {
                        throw new ArgumentException();
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
            """, """
            using System;
            
            class C
            {
                void M()
                {
                    Helper();
                }
                
                void Helper()
                {
                    try
                    {
                        throw new ArgumentException();
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestNecessaryThrowsType_DerivedTypeThrown_NoDiagnostic()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            
            class C
            {
                void M() throws Exception
                {
                    throw new ArgumentException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestUnnecessaryThrowsType_MultipleTypes_AllUnnecessary()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.IO;
            
            class C
            {
                void M() throws [|ArgumentException|], [|IOException|]
                {
                }
            }
            """, """
            using System;
            using System.IO;
            
            class C
            {
                void M()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestNecessaryThrowsType_InExpressionBody_NoDiagnostic()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            
            class C
            {
                void M() throws ArgumentException => throw new ArgumentException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestUnnecessaryThrowsType_EmptyExpressionBody()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            
            class C
            {
                int M() throws [|ArgumentException|] => 42;
            }
            """, """
            using System;
            
            class C
            {
                int M() => 42;
            }
            """);
}
