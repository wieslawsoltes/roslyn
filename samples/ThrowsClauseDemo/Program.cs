using System;
using System.IO;

namespace ThrowsClauseDemo
{
    /// <summary>
    /// Demonstrates the new throws clause feature in C#
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== C# Throws Clause Demo ===");
            Console.WriteLine();

            // Example 1: Basic throws clause
            Console.WriteLine("Example 1: Basic file operation with throws clause");
            try
            {
                var processor = new FileProcessor();
                processor.ReadFile("test.txt");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Caught IOException: {ex.Message}");
            }
            Console.WriteLine();

            // Example 2: Multiple exception types
            Console.WriteLine("Example 2: Method with multiple exception types");
            try
            {
                var processor = new FileProcessor();
                processor.ProcessFile("");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Caught ArgumentException: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Caught IOException: {ex.Message}");
            }
            Console.WriteLine();

            // Example 3: No exceptions thrown
            Console.WriteLine("Example 3: Safe operation with no exceptions");
            var calculator = new Calculator();
            int result = calculator.Add(5, 10);
            Console.WriteLine($"5 + 10 = {result}");
            Console.WriteLine();

            // Example 4: Expression-bodied method with throws clause
            Console.WriteLine("Example 4: Expression-bodied method");
            try
            {
                var validator = new DataValidator();
                validator.ValidateEmail("");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Caught ArgumentException: {ex.Message}");
            }
            Console.WriteLine();

            Console.WriteLine("=== Demo Complete ===");
        }
    }

    /// <summary>
    /// File processor demonstrating throws clause
    /// </summary>
    public class FileProcessor
    {
        /// <summary>
        /// Reads a file and returns its content
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>File content</returns>
        public string ReadFile(string path) throws IOException
        {
            Console.WriteLine($"  Attempting to read: {path}");
            
            if (!File.Exists(path))
            {
                throw new IOException($"File not found: {path}");
            }
            
            return File.ReadAllText(path);
        }

        /// <summary>
        /// Processes a file with validation
        /// </summary>
        /// <param name="path">The file path</param>
        public void ProcessFile(string path) throws ArgumentException, IOException
        {
            Console.WriteLine($"  Processing file: {path}");
            
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            }
            
            string content = ReadFile(path);
            Console.WriteLine($"  File content length: {content.Length}");
        }

        /// <summary>
        /// Safe method that doesn't throw exceptions
        /// </summary>
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }
    }

    /// <summary>
    /// Calculator demonstrating methods without throws clause
    /// </summary>
    public class Calculator
    {
        /// <summary>
        /// Adds two numbers - no exceptions
        /// </summary>
        public int Add(int a, int b)
        {
            Console.WriteLine($"  Computing: {a} + {b}");
            return a + b;
        }

        /// <summary>
        /// Divides two numbers
        /// </summary>
        public double Divide(int numerator, int denominator) throws DivideByZeroException
        {
            if (denominator == 0)
            {
                throw new DivideByZeroException("Cannot divide by zero");
            }
            return (double)numerator / denominator;
        }
    }

    /// <summary>
    /// Data validator with expression-bodied methods
    /// </summary>
    public class DataValidator
    {
        /// <summary>
        /// Validates an email address - expression-bodied
        /// </summary>
        public void ValidateEmail(string email) throws ArgumentException =>
            throw new ArgumentException(
                string.IsNullOrEmpty(email) 
                    ? "Email cannot be empty" 
                    : "Invalid email format", 
                nameof(email));

        /// <summary>
        /// Validates a URL - expression-bodied
        /// </summary>
        public bool IsValidUrl(string url) throws ArgumentException => 
            !string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out _) 
                ? true 
                : throw new ArgumentException("Invalid URL", nameof(url));
    }

    /// <summary>
    /// Demonstrates inheritance with throws clause
    /// </summary>
    public abstract class BaseReader
    {
        /// <summary>
        /// Base method declaring exceptions it can throw
        /// </summary>
        public abstract string Read() throws IOException, InvalidOperationException;
    }

    /// <summary>
    /// Derived reader - can declare subset of base exceptions
    /// </summary>
    public class StreamReader : BaseReader
    {
        /// <summary>
        /// Override that only throws IOException (subset)
        /// </summary>
        public override string Read() throws IOException
        {
            Console.WriteLine("  Reading from stream...");
            throw new IOException("Stream read error");
        }
    }

    /// <summary>
    /// Another derived reader - no exceptions
    /// </summary>
    public class CachedReader : BaseReader
    {
        /// <summary>
        /// Override that throws no exceptions (empty subset)
        /// </summary>
        public override string Read()
        {
            Console.WriteLine("  Reading from cache...");
            return "cached data";
        }
    }

    /// <summary>
    /// Interface demonstrating throws clause
    /// </summary>
    public interface IDataAccess
    {
        /// <summary>
        /// Loads data from a source
        /// </summary>
        string LoadData(string source) throws IOException;
    }

    /// <summary>
    /// Implementation of interface with throws clause
    /// </summary>
    public class FileDataAccess : IDataAccess
    {
        /// <summary>
        /// Implements interface method with same exception
        /// </summary>
        public string LoadData(string source) throws IOException
        {
            Console.WriteLine($"  Loading data from: {source}");
            
            if (!File.Exists(source))
            {
                throw new IOException($"Source not found: {source}");
            }
            
            return File.ReadAllText(source);
        }
    }
}
