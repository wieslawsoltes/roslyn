// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [Trait("Feature", "ThrowsClause")]
    public class ThrowsClauseSemanticTests : CSharpTestBase
    {
        #region CS9340: Type must derive from System.Exception

        [Fact]
        public void ERR_ThrowsClauseTypeMustDeriveFromException_PrimitiveType()
        {
            var source = """
                class C
                {
                    void M() throws int { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics(
                // (3,25): error CS9340: Exception type 'int' in throws clause must derive from 'System.Exception'
                //     void M() throws int { }
                Diagnostic(ErrorCode.ERR_ThrowsClauseTypeMustDeriveFromException, "int").WithArguments("int").WithLocation(3, 25)
            );
        }

        [Fact]
        public void ERR_ThrowsClauseTypeMustDeriveFromException_NonExceptionType()
        {
            var source = """
                class MyClass { }

                class C
                {
                    void M() throws MyClass { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics(
                // (5,25): error CS9340: Exception type 'MyClass' in throws clause must derive from 'System.Exception'
                //     void M() throws MyClass { }
                Diagnostic(ErrorCode.ERR_ThrowsClauseTypeMustDeriveFromException, "MyClass").WithArguments("MyClass").WithLocation(5, 25)
            );
        }

        [Fact]
        public void ValidExceptionType_SystemException()
        {
            var source = """
                using System;

                class C
                {
                    void M() throws Exception { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ValidExceptionType_DerivedExceptions()
        {
            var source = """
                using System;

                class C
                {
                    void M1() throws ArgumentException { }
                    void M2() throws InvalidOperationException { }
                    void M3() throws NotImplementedException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ValidExceptionType_CustomException()
        {
            var source = """
                using System;

                class MyException : Exception { }

                class C
                {
                    void M() throws MyException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        #endregion

        #region CS9341: Duplicate exception types

        [Fact]
        public void ERR_DuplicateExceptionTypeInThrowsClause_Simple()
        {
            var source = """
                using System;

                class C
                {
                    void M() throws ArgumentException, ArgumentException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics(
                // (5,48): error CS9341: Duplicate exception type 'ArgumentException' in throws clause
                //     void M() throws ArgumentException, ArgumentException { }
                Diagnostic(ErrorCode.ERR_DuplicateExceptionTypeInThrowsClause, "ArgumentException").WithArguments("ArgumentException").WithLocation(5, 48)
            );
        }

        [Fact]
        public void ERR_DuplicateExceptionTypeInThrowsClause_Multiple()
        {
            var source = """
                using System;

                class C
                {
                    void M() throws Exception, ArgumentException, Exception, InvalidOperationException, ArgumentException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics(
                // (5,55): error CS9341: Duplicate exception type 'Exception' in throws clause
                //     void M() throws Exception, ArgumentException, Exception, InvalidOperationException, ArgumentException { }
                Diagnostic(ErrorCode.ERR_DuplicateExceptionTypeInThrowsClause, "Exception").WithArguments("Exception").WithLocation(5, 55),
                // (5,99): error CS9341: Duplicate exception type 'ArgumentException' in throws clause
                //     void M() throws Exception, ArgumentException, Exception, InvalidOperationException, ArgumentException { }
                Diagnostic(ErrorCode.ERR_DuplicateExceptionTypeInThrowsClause, "ArgumentException").WithArguments("ArgumentException").WithLocation(5, 99)
            );
        }

        [Fact]
        public void ValidThrowsClause_NoDuplicates()
        {
            var source = """
                using System;
                using System.IO;

                class C
                {
                    void M() throws ArgumentException, InvalidOperationException, IOException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        #endregion

        #region CS9342: Override throws exception not declared by base

        [Fact]
        public void ERR_OverrideThrowsExceptionNotDeclaredByBase_Simple()
        {
            var source = """
                using System;

                class Base
                {
                    public virtual void M() throws ArgumentException { }
                }

                class Derived : Base
                {
                    public override void M() throws IOException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics(
                // (10,45): error CS9342: 'Derived.M()': override method throws exception type 'IOException' not declared in base method
                //     public override void M() throws IOException { }
                Diagnostic(ErrorCode.ERR_OverrideThrowsExceptionNotDeclaredByBase, "IOException").WithArguments("Derived.M()", "IOException").WithLocation(10, 45)
            );
        }

        [Fact]
        public void ValidOverride_SameExceptionType()
        {
            var source = """
                using System;

                class Base
                {
                    public virtual void M() throws ArgumentException { }
                }

                class Derived : Base
                {
                    public override void M() throws ArgumentException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ValidOverride_SubsetOfExceptions()
        {
            var source = """
                using System;
                using System.IO;

                class Base
                {
                    public virtual void M() throws ArgumentException, IOException, InvalidOperationException { }
                }

                class Derived : Base
                {
                    public override void M() throws ArgumentException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ValidOverride_DerivedExceptionType()
        {
            var source = """
                using System;

                class Base
                {
                    public virtual void M() throws ArgumentException { }
                }

                class Derived : Base
                {
                    public override void M() throws ArgumentNullException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ERR_OverrideThrowsExceptionNotDeclaredByBase_UnrelatedType()
        {
            var source = """
                using System;
                using System.IO;

                class Base
                {
                    public virtual void M() throws ArgumentException { }
                }

                class Derived : Base
                {
                    public override void M() throws ArgumentException, IOException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics(
                // (11,64): error CS9342: 'Derived.M()': override method throws exception type 'IOException' not declared in base method
                //     public override void M() throws ArgumentException, IOException { }
                Diagnostic(ErrorCode.ERR_OverrideThrowsExceptionNotDeclaredByBase, "IOException").WithArguments("Derived.M()", "IOException").WithLocation(11, 64)
            );
        }

        #endregion

        #region CS9343: Interface implementation throws exception not declared by interface

        [Fact]
        public void ERR_InterfaceImplementationThrowsExceptionNotDeclaredByInterface_Simple()
        {
            var source = """
                using System;

                interface I
                {
                    void M() throws ArgumentException;
                }

                class C : I
                {
                    public void M() throws IOException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics(
                // (10,36): error CS9343: 'C.M()': interface implementation throws exception type 'IOException' not declared in interface method
                //     public void M() throws IOException { }
                Diagnostic(ErrorCode.ERR_InterfaceImplementationThrowsExceptionNotDeclaredByInterface, "IOException").WithArguments("C.M()", "IOException").WithLocation(10, 36)
            );
        }

        [Fact]
        public void ValidInterfaceImplementation_SameExceptionType()
        {
            var source = """
                using System;

                interface I
                {
                    void M() throws ArgumentException;
                }

                class C : I
                {
                    public void M() throws ArgumentException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ValidInterfaceImplementation_SubsetOfExceptions()
        {
            var source = """
                using System;
                using System.IO;

                interface I
                {
                    void M() throws ArgumentException, IOException, InvalidOperationException;
                }

                class C : I
                {
                    public void M() throws ArgumentException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ValidInterfaceImplementation_DerivedExceptionType()
        {
            var source = """
                using System;

                interface I
                {
                    void M() throws ArgumentException;
                }

                class C : I
                {
                    public void M() throws ArgumentNullException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ERR_InterfaceImplementationThrowsExceptionNotDeclaredByInterface_ExplicitImplementation()
        {
            var source = """
                using System;
                using System.IO;

                interface I
                {
                    void M() throws ArgumentException;
                }

                class C : I
                {
                    void I.M() throws ArgumentException, IOException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics(
                // (11,54): error CS9343: 'C.I.M()': interface implementation throws exception type 'IOException' not declared in interface method
                //     void I.M() throws ArgumentException, IOException { }
                Diagnostic(ErrorCode.ERR_InterfaceImplementationThrowsExceptionNotDeclaredByInterface, "IOException").WithArguments("C.I.M()", "IOException").WithLocation(11, 54)
            );
        }

        #endregion

        #region Symbol API Tests

        [Fact]
        public void ThrowsTypes_ReturnsEmptyArrayWhenNoThrowsClause()
        {
            var source = """
                class C
                {
                    void M() { }
                }
                """;

            var comp = CreateCompilation(source);
            var method = (IMethodSymbol)comp.GetTypeByMetadataName("C").GetMember("M");
            Assert.Empty(method.ThrowsTypes);
        }

        [Fact]
        public void ThrowsTypes_ReturnsSingleException()
        {
            var source = """
                using System;

                class C
                {
                    void M() throws ArgumentException { }
                }
                """;

            var comp = CreateCompilation(source);
            var method = (IMethodSymbol)comp.GetTypeByMetadataName("C").GetMember("M");
            Assert.Single(method.ThrowsTypes);
            Assert.Equal("System.ArgumentException", method.ThrowsTypes[0].ToTestDisplayString());
        }

        [Fact]
        public void ThrowsTypes_ReturnsMultipleExceptions()
        {
            var source = """
                using System;
                using System.IO;

                class C
                {
                    void M() throws ArgumentException, IOException, InvalidOperationException { }
                }
                """;

            var comp = CreateCompilation(source);
            var method = (IMethodSymbol)comp.GetTypeByMetadataName("C").GetMember("M");
            Assert.Equal(3, method.ThrowsTypes.Length);
            Assert.Equal("System.ArgumentException", method.ThrowsTypes[0].ToTestDisplayString());
            Assert.Equal("System.IO.IOException", method.ThrowsTypes[1].ToTestDisplayString());
            Assert.Equal("System.InvalidOperationException", method.ThrowsTypes[2].ToTestDisplayString());
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void ThrowsClause_WithGenericMethod()
        {
            var source = """
                using System;

                class C
                {
                    void M<T>() throws ArgumentException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ThrowsClause_WithAsyncMethod()
        {
            var source = """
                using System;
                using System.Threading.Tasks;

                class C
                {
                    async Task M() throws ArgumentException { await Task.CompletedTask; }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ThrowsClause_WithPartialMethod()
        {
            var source = """
                using System;

                partial class C
                {
                    partial void M() throws ArgumentException;
                    partial void M() throws ArgumentException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ThrowsClause_WithErrorTypes()
        {
            var source = """
                class C
                {
                    void M() throws UnknownException { }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics(
                // (3,25): error CS0246: The type or namespace name 'UnknownException' could not be found (are you missing a using directive or an assembly reference?)
                //     void M() throws UnknownException { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownException").WithArguments("UnknownException").WithLocation(3, 25)
            );
        }

        #endregion
    }
}
