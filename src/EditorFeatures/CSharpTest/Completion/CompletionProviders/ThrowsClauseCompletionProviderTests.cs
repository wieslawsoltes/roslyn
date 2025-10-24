// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Completion)]
public class ThrowsClauseCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(ThrowsClauseCompletionProvider);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public async Task AfterThrowsKeyword()
    {
        var markup = """
            using System;
            
            class C
            {
                void M() throws $$
                {
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "Exception");
        await VerifyItemExistsAsync(markup, "ArgumentException");
        await VerifyItemExistsAsync(markup, "IOException");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public async Task AfterThrowsKeywordWithComma()
    {
        var markup = """
            using System;
            
            class C
            {
                void M() throws ArgumentException, $$
                {
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "Exception");
        await VerifyItemExistsAsync(markup, "IOException");
        await VerifyItemIsAbsentAsync(markup, "ArgumentException"); // Already declared
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public async Task OnlyExceptionTypes()
    {
        var markup = """
            using System;
            
            class C
            {
                void M() throws $$
                {
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "Exception");
        await VerifyItemIsAbsentAsync(markup, "String");
        await VerifyItemIsAbsentAsync(markup, "Int32");
        await VerifyItemIsAbsentAsync(markup, "Object");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public async Task NotBeforeThrowsKeyword()
    {
        var markup = """
            using System;
            
            class C
            {
                void M() $$throws
                {
                }
            }
            """;

        await VerifyNoItemsExistAsync(markup);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public async Task NotInMethodBody()
    {
        var markup = """
            using System;
            
            class C
            {
                void M() throws Exception
                {
                    $$
                }
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "ArgumentException");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public async Task CommonExceptionsPrioritized()
    {
        var markup = """
            using System;
            
            class C
            {
                void M() throws $$
                {
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "ArgumentException");
        await VerifyItemExistsAsync(markup, "ArgumentNullException");
        await VerifyItemExistsAsync(markup, "InvalidOperationException");
        await VerifyItemExistsAsync(markup, "IOException");
        await VerifyItemExistsAsync(markup, "NotSupportedException");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public async Task WithSystemImport()
    {
        var markup = """
            using System;
            using System.IO;
            
            class C
            {
                void M() throws $$
                {
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "IOException");
        await VerifyItemExistsAsync(markup, "FileNotFoundException");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public async Task CustomExceptionInProject()
    {
        var markup = """
            using System;
            
            class CustomException : Exception { }
            
            class C
            {
                void M() throws $$
                {
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "CustomException");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/")]
    public async Task MultipleThrowsTypes()
    {
        var markup = """
            using System;
            
            class C
            {
                void M() throws ArgumentException, IOException, $$
                {
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "InvalidOperationException");
        await VerifyItemIsAbsentAsync(markup, "ArgumentException"); // Already declared
        await VerifyItemIsAbsentAsync(markup, "IOException"); // Already declared
    }
}
