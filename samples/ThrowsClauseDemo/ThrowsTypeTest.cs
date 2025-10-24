using System;
using System.IO;

namespace ThrowsTypeTest
{
    public class TestClass
    {
        // Method with no throws clause
        public void NoThrows()
        {
            Console.WriteLine("No exceptions declared");
        }

        // Method with single exception
        public void SingleException() throws IOException
        {
            throw new IOException("Test");
        }

        // Method with multiple exceptions
        public int MultipleExceptions(int x, int y) throws ArgumentException, InvalidOperationException, DivideByZeroException
        {
            if (x < 0) throw new ArgumentException("x must be positive");
            if (y == 0) throw new DivideByZeroException();
            return x / y;
        }

        // Expression-bodied method with throws
        public string ExpressionBodied(string input) throws ArgumentNullException 
            => input ?? throw new ArgumentNullException(nameof(input));

        static void Main()
        {
            Console.WriteLine("Testing throws clause binding...");
            var test = new TestClass();
            
            // Test that methods with throws compile
            try
            {
                test.SingleException();
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Caught expected IOException: {ex.Message}");
            }
            
            Console.WriteLine("All tests passed!");
        }
    }
}
