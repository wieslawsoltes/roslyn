// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.ThrowsClause;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Features.CodeFixes.AddThrowsClause;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ThrowsClause;

using VerifyCS = CSharpCodeFixVerifier<
    MissingThrowsTypeAnalyzer,
    AddThrowsClauseCodeFixProvider>;

public sealed class MissingThrowsTypeAnalyzerTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestSimpleThrow_NotInThrowsClause()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            
            class C
            {
                void M()
                {
                    throw new [|ArgumentException|]();
                }
            }
            """, """
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
    public Task TestSimpleThrow_AlreadyInThrowsClause_NoDiagnostic()
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
    public Task TestThrow_BaseTypeInThrowsClause_NoDiagnostic()
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
    public Task TestThrow_CaughtByTryCatch_NoDiagnostic()
        => VerifyCS.VerifyAnalyzerAsync("""
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
    public Task TestThrow_CaughtByBaseCatch_NoDiagnostic()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            
            class C
            {
                void M()
                {
                    try
                    {
                        throw new ArgumentException();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestThrow_CaughtByCatchAll_NoDiagnostic()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            
            class C
            {
                void M()
                {
                    try
                    {
                        throw new ArgumentException();
                    }
                    catch
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestRethrow_NoDiagnostic()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            
            class C
            {
                void M()
                {
                    try
                    {
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestMultipleThrows_DifferentTypes()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.IO;
            
            class C
            {
                void M()
                {
                    throw new [|ArgumentException|]();
                    throw new [|IOException|]();
                }
            }
            """, """
            using System;
            using System.IO;
            
            class C
            {
                void M() throws ArgumentException, IOException
                {
                    throw new ArgumentException();
                    throw new IOException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestThrow_PartiallyInThrowsClause()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.IO;
            
            class C
            {
                void M() throws ArgumentException
                {
                    throw new ArgumentException();
                    throw new [|IOException|]();
                }
            }
            """, """
            using System;
            using System.IO;
            
            class C
            {
                void M() throws ArgumentException, IOException
                {
                    throw new ArgumentException();
                    throw new IOException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestThrow_InNestedTry_NotCaught()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.IO;
            
            class C
            {
                void M()
                {
                    try
                    {
                        throw new [|ArgumentException|]();
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            """, """
            using System;
            using System.IO;
            
            class C
            {
                void M() throws ArgumentException
                {
                    try
                    {
                        throw new ArgumentException();
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestThrow_CustomException()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            
            class CustomException : Exception { }
            
            class C
            {
                void M()
                {
                    throw new [|CustomException|]();
                }
            }
            """, """
            using System;
            
            class CustomException : Exception { }
            
            class C
            {
                void M() throws CustomException
                {
                    throw new CustomException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public Task TestThrow_InExpressionBody()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            
            class C
            {
                void M() => throw new [|ArgumentException|]();
            }
            """, """
            using System;
            
            class C
            {
                void M() throws ArgumentException => throw new ArgumentException();
            }
            """);
}
