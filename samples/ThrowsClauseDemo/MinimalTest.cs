using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing throws clause syntax...");
    }
    
    public void SimpleMethod() throws IOException
    {
        throw new IOException("Test");
    }
    
    public int Calculate(int x, int y) throws ArgumentException, InvalidOperationException
    {
        if (x < 0) throw new ArgumentException("x must be positive");
        if (y == 0) throw new InvalidOperationException("y cannot be zero");
        return x / y;
    }
    
    // Expression-bodied method with throws clause
    public string Format(string input) throws ArgumentNullException 
        => input ?? throw new ArgumentNullException(nameof(input));
}
