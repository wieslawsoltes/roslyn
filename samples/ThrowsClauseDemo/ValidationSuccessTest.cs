using System;
using System.IO;

namespace ThrowsClauseDemo
{
    class ValidationSuccessTest
    {
        // ✅ Valid: Single exception type
        void TestSingleException() throws IOException
        {
            throw new IOException("Test");
        }

        // ✅ Valid: Multiple different exception types
        void TestMultipleExceptions() throws IOException, ArgumentException, InvalidOperationException
        {
            throw new ArgumentException("Test");
        }

        // ✅ Valid: No throws clause
        void TestNoThrowsClause()
        {
            Console.WriteLine("No exceptions");
        }

        // ✅ Valid: Exception subclass
        void TestExceptionSubclass() throws FileNotFoundException
        {
            throw new FileNotFoundException("File not found");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("✅ All validation tests passed!");
            Console.WriteLine("The compiler correctly accepts valid throws clauses.");
            
            var test = new ValidationSuccessTest();
            
            try
            {
                test.TestSingleException();
            }
            catch (IOException e)
            {
                Console.WriteLine($"Caught IOException: {e.Message}");
            }
            
            try
            {
                test.TestMultipleExceptions();
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"Caught ArgumentException: {e.Message}");
            }
            
            test.TestNoThrowsClause();
            
            try
            {
                test.TestExceptionSubclass();
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine($"Caught FileNotFoundException: {e.Message}");
            }
            
            Console.WriteLine("✅ Runtime execution successful!");
        }
    }
}
