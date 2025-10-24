using System;
using System.IO;

namespace ThrowsClauseDemo
{
    class ValidationTest
    {
        // Test 1: Valid - exception types derive from Exception
        void ValidMethod() throws IOException, ArgumentException
        {
            throw new IOException("Test");
        }

        // Test 2: Invalid - string does not derive from Exception (should be CS9340)
        void InvalidType1() throws string
        {
        }

        // Test 3: Invalid - int does not derive from Exception (should be CS9340)
        void InvalidType2() throws int
        {
        }

        // Test 4: Invalid - duplicate exception types (should be CS9341)
        void DuplicateTypes() throws IOException, ArgumentException, IOException
        {
        }

        // Test 5: Invalid - multiple duplicates (should be CS9341 for second occurrence)
        void MultipleDuplicates() throws IOException, IOException, ArgumentException, ArgumentException
        {
        }

        // Test 6: Valid - no throws clause
        void NoThrowsClause()
        {
        }

        // Test 7: Valid - single exception type
        void SingleExceptionType() throws IOException
        {
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Validation test compiled");
        }
    }
}
