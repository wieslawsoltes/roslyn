# C# Throws Clause Language Specification

## Summary

This document specifies the addition of a `throws` clause to C# method declarations, similar to Java's checked exception mechanism. This feature allows developers to explicitly declare which exception types a method may throw.

## Motivation

The `throws` clause provides several benefits:

1. **Documentation**: Makes it explicit which exceptions a method can throw
2. **Static Analysis**: Enables better tooling and compile-time checks
3. **API Clarity**: Makes it clear to callers what exceptions they need to handle
4. **Migration Path**: Helps teams moving from Java to C# maintain similar patterns

## Detailed Design

### Syntax

The `throws` clause appears after the method parameter list and before the method body or expression body:

```csharp
// Basic syntax
returnType MethodName(parameters) throws ExceptionType
{
    // method body
}

// Multiple exception types
returnType MethodName(parameters) throws ExceptionType1, ExceptionType2
{
    // method body
}

// With constraints
returnType MethodName<T>(parameters) where T : class throws IOException
{
    // method body
}

// Expression bodied method
returnType MethodName(parameters) throws ExceptionType => expression;
```

### Grammar

The grammar is extended as follows:

```
method-declaration:
    attributes? method-modifiers? partial? return-type method-member-name type-parameter-list?
        ( parameter-list? ) type-parameter-constraints-clauses? throws-clause? method-body

throws-clause:
    'throws' exception-type-list

exception-type-list:
    exception-type
    exception-type-list ',' exception-type

exception-type:
    type
```

### Semantic Rules

1. **Type Requirements**:
   - All types in the `throws` clause MUST be types that derive from `System.Exception`
   - Generic type parameters are allowed if they are constrained to derive from `System.Exception`

2. **First Iteration Scope**:
   - Only method declarations are supported
   - Properties, constructors, operators, and other members are NOT supported in this iteration

3. **Inheritance**:
   - A derived type can list the same or a subset of exceptions
   - A derived type CANNOT add new exception types not declared in the base
   - If a base method throws `ExceptionA` and `ExceptionB`, the derived method can throw:
     - Nothing (empty throws clause or no clause)
     - Only `ExceptionA`
     - Only `ExceptionB`
     - Both `ExceptionA` and `ExceptionB`
     - Derived types of `ExceptionA` or `ExceptionB`

4. **Interface Implementation**:
   - An implementing method must declare the same or a subset of exceptions as the interface method

5. **Compilation Behavior**:
   - The `throws` clause is primarily for documentation and static analysis
   - No runtime enforcement is performed in this iteration
   - The information is preserved in metadata for reflection

### Examples

#### Basic Usage

```csharp
public class FileProcessor
{
    public string ReadFile(string path) throws IOException
    {
        // Implementation that may throw IOException
        return File.ReadAllText(path);
    }

    public void ProcessFile(string path) throws IOException, ArgumentException
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty");
        
        string content = ReadFile(path);
        // Process content
    }
}
```

#### Inheritance Example

```csharp
public class BaseProcessor
{
    public virtual void Process() throws IOException, InvalidOperationException
    {
        // Implementation
    }
}

public class DerivedProcessor : BaseProcessor
{
    // Valid: subset of exceptions
    public override void Process() throws IOException
    {
        // Implementation
    }
}
```

#### Interface Implementation

```csharp
public interface IDataReader
{
    string ReadData() throws IOException;
}

public class FileDataReader : IDataReader
{
    // Valid: same exception
    public string ReadData() throws IOException
    {
        // Implementation
    }
}

public class CachedDataReader : IDataReader
{
    // Valid: no exceptions (subset)
    public string ReadData()
    {
        // Implementation
    }
}
```

### Metadata Representation

The `throws` clause information is stored as a custom attribute in metadata:

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class ThrowsAttribute : Attribute
{
    public ThrowsAttribute(params Type[] exceptionTypes)
    {
        ExceptionTypes = exceptionTypes;
    }

    public Type[] ExceptionTypes { get; }
}
```

### Diagnostics

The following diagnostics are introduced:

1. **CS9001**: Type in throws clause must derive from System.Exception
2. **CS9002**: Duplicate exception type in throws clause
3. **CS9003**: Override method declares exceptions not in base method
4. **CS9004**: Interface implementation declares exceptions not in interface method
5. **CS9005**: Throws clause not supported on this member (non-method)

## Future Enhancements

Future iterations may include:

1. **Runtime Enforcement**: Option to enforce that methods only throw declared exceptions
2. **Caller Analysis**: Warnings when calling methods that throw exceptions without handling them
3. **Extended Member Support**: Support for properties, constructors, operators
4. **Generic Constraints**: Allow throws clause constraints on generic type parameters
5. **Async Methods**: Special handling for Task-returning methods

## Breaking Changes

This is a purely additive change with no breaking changes to existing code. The `throws` keyword becomes a contextual keyword.

## Design Meetings

- Initial design: October 24, 2025

---

**Note**: This is a first iteration implementation focusing on core functionality. The feature is primarily for documentation and static analysis purposes.
