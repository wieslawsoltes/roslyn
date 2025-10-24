using System;
using System.IO;

namespace ThrowsClauseDemo
{
    // Base class with various throws clause scenarios
    class BaseClass
    {
        // Base method throws IOException
        public virtual void Method1() throws IOException
        {
            throw new IOException("Base");
        }

        // Base method throws multiple exceptions
        public virtual void Method2() throws IOException, ArgumentException
        {
            throw new IOException("Base");
        }

        // Base method with no throws clause
        public virtual void Method3()
        {
            Console.WriteLine("No exceptions");
        }

        // Base method throws Exception (most general)
        public virtual void Method4() throws Exception
        {
            throw new Exception("Base");
        }
    }

    class DerivedValid : BaseClass
    {
        // ✅ Valid: Override throws same exception as base
        public override void Method1() throws IOException
        {
            throw new IOException("Derived");
        }

        // ✅ Valid: Override throws subset of base exceptions
        public override void Method2() throws IOException
        {
            throw new IOException("Derived");
        }

        // ✅ Valid: Override throws no exceptions (empty is always compatible)
        public override void Method3()
        {
            Console.WriteLine("Still no exceptions");
        }

        // ✅ Valid: Override throws subtype of base exception
        public override void Method4() throws IOException
        {
            throw new IOException("Derived - subtype of Exception");
        }
    }

    class DerivedInvalid : BaseClass
    {
        // ❌ Invalid: Override throws exception not in base (CS9342)
        public override void Method1() throws ArgumentException
        {
            throw new ArgumentException("Invalid");
        }

        // ❌ Invalid: Override throws exception not in base (CS9342)
        public override void Method2() throws InvalidOperationException
        {
            throw new InvalidOperationException("Invalid");
        }

        // ❌ Invalid: Override throws exception when base doesn't (CS9342)
        public override void Method3() throws IOException
        {
            throw new IOException("Invalid");
        }

        // Valid: IOException is subtype of Exception
        public override void Method4() throws IOException
        {
            throw new IOException("Valid");
        }
    }

    // Test with exception hierarchy
    class CustomException : Exception
    {
        public CustomException(string message) : base(message) { }
    }

    class SpecificCustomException : CustomException
    {
        public SpecificCustomException(string message) : base(message) { }
    }

    class BaseWithCustomException
    {
        public virtual void CustomMethod() throws CustomException
        {
            throw new CustomException("Base");
        }
    }

    class DerivedWithSpecificException : BaseWithCustomException
    {
        // ✅ Valid: SpecificCustomException is subtype of CustomException
        public override void CustomMethod() throws SpecificCustomException
        {
            throw new SpecificCustomException("Derived - more specific");
        }
    }

    class DerivedWithBroaderException : BaseWithCustomException
    {
        // ❌ Invalid: Exception is broader than CustomException (CS9342)
        public override void CustomMethod() throws Exception
        {
            throw new Exception("Invalid - too broad");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Override validation test compiled");
        }
    }
}
