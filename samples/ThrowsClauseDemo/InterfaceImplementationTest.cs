using System;
using System.IO;

namespace ThrowsClauseDemo
{
    // Interface with various throws clause scenarios
    interface IBasicOperations
    {
        // Interface method throws IOException
        void Method1() throws IOException;

        // Interface method throws multiple exceptions
        void Method2() throws IOException, ArgumentException;

        // Interface method with no throws clause
        void Method3();

        // Interface method throws Exception (most general)
        void Method4() throws Exception;
    }

    class ValidImplementation : IBasicOperations
    {
        // ✅ Valid: Implementation throws same exception as interface
        public void Method1() throws IOException
        {
            throw new IOException("Implementation");
        }

        // ✅ Valid: Implementation throws subset of interface exceptions
        public void Method2() throws IOException
        {
            throw new IOException("Implementation");
        }

        // ✅ Valid: Implementation throws no exceptions (empty is always compatible)
        public void Method3()
        {
            Console.WriteLine("No exceptions");
        }

        // ✅ Valid: Implementation throws subtype of interface exception
        public void Method4() throws FileNotFoundException
        {
            throw new FileNotFoundException("More specific exception");
        }
    }

    class InvalidImplementation : IBasicOperations
    {
        // ❌ Invalid: Implementation throws exception not in interface (CS9343)
        public void Method1() throws ArgumentException
        {
            throw new ArgumentException("Invalid");
        }

        // ❌ Invalid: Implementation throws exception not in interface (CS9343)
        public void Method2() throws InvalidOperationException
        {
            throw new InvalidOperationException("Invalid");
        }

        // ❌ Invalid: Implementation throws exception when interface doesn't (CS9343)
        public void Method3() throws IOException
        {
            throw new IOException("Invalid");
        }

        // Valid: FileNotFoundException is subtype of Exception
        public void Method4() throws FileNotFoundException
        {
            throw new FileNotFoundException("Valid");
        }
    }

    // Test with explicit interface implementation
    class ExplicitImplementation : IBasicOperations
    {
        // ✅ Valid: Explicit implementation with compatible throws clause
        void IBasicOperations.Method1() throws IOException
        {
            throw new IOException("Explicit implementation");
        }

        // ❌ Invalid: Explicit implementation with incompatible throws clause (CS9343)
        void IBasicOperations.Method2() throws InvalidOperationException
        {
            throw new InvalidOperationException("Invalid");
        }

        void IBasicOperations.Method3()
        {
            Console.WriteLine("Explicit - no exceptions");
        }

        void IBasicOperations.Method4() throws IOException
        {
            throw new IOException("Valid - subtype");
        }
    }

    // Test with custom exception hierarchy
    class CustomException : Exception
    {
        public CustomException(string message) : base(message) { }
    }

    class SpecificCustomException : CustomException
    {
        public SpecificCustomException(string message) : base(message) { }
    }

    interface ICustomExceptions
    {
        void CustomMethod() throws CustomException;
    }

    class ValidCustomImplementation : ICustomExceptions
    {
        // ✅ Valid: SpecificCustomException is subtype of CustomException
        public void CustomMethod() throws SpecificCustomException
        {
            throw new SpecificCustomException("More specific");
        }
    }

    class InvalidCustomImplementation : ICustomExceptions
    {
        // ❌ Invalid: Exception is broader than CustomException (CS9343)
        public void CustomMethod() throws Exception
        {
            throw new Exception("Too broad");
        }
    }

    // Test with mixed implicit and explicit implementations
    interface IMultipleMembers
    {
        void ImplicitMethod() throws IOException;
        void ExplicitMethod() throws ArgumentException;
    }

    class MixedImplementation : IMultipleMembers
    {
        // ✅ Valid: Implicit implementation with correct throws clause
        public void ImplicitMethod() throws IOException
        {
            throw new IOException("Implicit");
        }

        // ✅ Valid: Explicit implementation with correct throws clause
        void IMultipleMembers.ExplicitMethod() throws ArgumentException
        {
            throw new ArgumentException("Explicit");
        }
    }

    // Test with no throws clause in implementation
    class NoThrowsImplementation : IBasicOperations
    {
        // ✅ Valid: No throws clause is always compatible
        public void Method1()
        {
            Console.WriteLine("No throws in implementation");
        }

        public void Method2()
        {
            Console.WriteLine("No throws in implementation");
        }

        public void Method3()
        {
            Console.WriteLine("No throws in implementation");
        }

        public void Method4()
        {
            Console.WriteLine("No throws in implementation");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Interface implementation validation test compiled");
        }
    }
}
