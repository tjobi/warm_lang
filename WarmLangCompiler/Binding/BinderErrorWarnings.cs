using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
using WarmLangLexerParser.ErrorReporting;

namespace WarmLangCompiler.Binding;

internal static class BinderErrorWarnings
{
    internal static void ReportBinaryOperatorCannotBeApplied(this ErrorWarrningBag bag, TextLocation loc, SyntaxToken op, TypeSymbol left, TypeSymbol right)
    {
        var message = $"Operator '{op.Kind.AsString()}' cannot be applied to type '{left.Name}' and '{right.Name}' ";
        bag.Report(message, true, loc);
    }

    internal static void ReportUnaryOperatorCannotBeApplied(this ErrorWarrningBag bag, TextLocation loc, SyntaxToken op, TypeSymbol left)
    {
        var message = $"Operator '{op.Kind.AsString()}' cannot be applied to type '{left.Name}'";
        bag.Report(message, true, loc);
    }

    internal static void ReportNameAlreadyDeclared(this ErrorWarrningBag bag, TextLocation location, string name)
    {
        var message = $"A variable, function, or type named '{name}' is already defined in this scope";
        bag.Report(message, true, location);
    }

    internal static void ReportNameAlreadyDeclared(this ErrorWarrningBag bag, SyntaxToken token)
    => ReportNameAlreadyDeclared(bag, token.Location, token.Name!);

    internal static void ReportTypeAlreadyDeclared(this ErrorWarrningBag bag, TextLocation location, string name)
    {
        var message = $"A type named '{name}' is already defined";
        bag.Report(message, true, location);
    }

    internal static void ReportNameDoesNotExist(this ErrorWarrningBag bag, TextLocation location, string name)
    {
        var message = $"The name '{name}' does not exist in current scope";
        bag.Report(message, true, location);
    }

    internal static void ReportNameDoesNotExist(this ErrorWarrningBag bag, SyntaxToken name) => ReportNameDoesNotExist(bag, name.Location, name.Name!);

    internal static void ReportParameterDuplicateName(this ErrorWarrningBag bag, SyntaxToken name)
    {
        var message = $"The parameter '{name.Name}' is a duplicate";
        bag.Report(message, true, name.Location);
    }

    internal static void ReportCannotImplicitlyConvertToType(this ErrorWarrningBag bag, TextLocation location, TypeSymbol expected, TypeSymbol badBoi)
    {
        var message = $"Cannot implicitly convert type '{badBoi.Name}' to '{expected.Name}'";
        bag.Report(message, true, location);
    }

    internal static void ReportCannotConvertToType(this ErrorWarrningBag bag, TextLocation location, TypeSymbol expected, TypeSymbol badBoi)
    {
        var message = $"Cannot convert type '{badBoi.Name}' to '{expected.Name}'";
        bag.Report(message, true, location);
    }

    internal static void ReportCannotSubscriptIntoType(this ErrorWarrningBag bag, TextLocation location, TypeSymbol badBoi)
    {
        var message = $"Cannot apply subscripting [] to expression of type '{badBoi.Name}'";
        bag.Report(message, true, location);
    }

    internal static void ReportFunctionCallMismatchArguments(this ErrorWarrningBag bag, TextLocation location, string name, int expectedNumArgs, int realNumArgs)
    {
        //TODO: Go through the arguments, and find the ones that are missing.
        var message = $"Function '{name}' expected {expectedNumArgs} arguments but got {realNumArgs}";
        bag.Report(message, true, location);
    }

    internal static void ReportNameIsNotAFunction(this ErrorWarrningBag bag, TextLocation location, string name, TypeSymbol type)
    {
        var message = $"The name '{name}' of type '{type}' cannot be called";
        bag.Report(message, true, location);
    }

    internal static void ReportCannotReturnOutsideFunction(this ErrorWarrningBag bag, SyntaxToken retToken)
    {
        var message = $"Invalid 'return' outside of function body";
        bag.Report(message, true, retToken.Location);
    }

    internal static void ReportReturnIsMissingExpression(this ErrorWarrningBag bag, SyntaxToken retToken, TypeSymbol expectedType)
    {
        var message = $"The return requires an expression of type '{expectedType}'";
        bag.Report(message, true, retToken.Location);
    }

    internal static void ReportNotAllCodePathsReturn(this ErrorWarrningBag bag, FunctionSymbol function)
    {
        var message = $"All code paths of '{function}' must return value of type '{function.ReturnType}'";
        bag.Report(message, true, function.Location);
    }

    internal static void ReportReturnWithValueInVoidFunction(this ErrorWarrningBag bag, SyntaxToken retToken, FunctionSymbol function)
    {
        var message = $"The function '{function}' returns void, so the return keyword must not be followed by an expression";
        bag.Report(message, true, retToken.Location);
    }

    internal static void ReportInvalidLeftSideOfAssignment(this ErrorWarrningBag bag, TextLocation location)
    {
        var message = $"The left-hand side of an assignment must be a variable or a subscript";
        bag.Report(message, true, location);
    }

    internal static void ReportTypeOfEmptyListMustBeExplicit(this ErrorWarrningBag bag, TextLocation location)
    {
        var message = $"The type of empty list must be explicit unless used for variable declaration. Looks like '[]int' for int";
        bag.Report(message, true, location);
    }

    internal static void ReportSubscriptTargetIsReadOnly(this ErrorWarrningBag bag, TypeSymbol type, TextLocation location)
    {
        var message = $"Cannot assign to subscript of '{type}' -- it is readonly";
        bag.Report(message, true, location);
    }

    internal static void ReportProgramHasBothMainAndTopLevelStatements(this ErrorWarrningBag bag, TextLocation mainFunc)
    {
        var message = $"Program cannot contain both a main function and global statements";
        bag.Report(message, true, mainFunc);
    }

    internal static void ReportCouldNotFindMemberForType(this ErrorWarrningBag bag, TextLocation location, TypeSymbol type, string? name)
    {
        var message = $"Type '{type}' does not contain a definition for '{name ?? "<null>"}'. Are you sure you spelt it right?";
        bag.Report(message, true, location);
    }

    internal static void ReportExpectedFunctionName(this ErrorWarrningBag bag, TextLocation location)
    {
        var message = "Expected a function or a callable expression";
        bag.Report(message, true, location);
    }

    internal static void ReportExpectedVariableName(this ErrorWarrningBag bag, TextLocation location, string name)
    {
        var message = $"The name '{name}' is not variable but a function. Did you forget to call it?";
        bag.Report(message, true, location);
    }

    internal static void ReportLocalMemberFuncDeclaration(this ErrorWarrningBag bag, SyntaxToken funcNameToken, TextLocation typeLocation, TypeSymbol type)
    {
        var message = $"Function members cannot be declared in a local scope, move '{type}.{funcNameToken.Name}' outside of function body";
        bag.Report(message, true, TextLocation.FromTo(typeLocation, funcNameToken.Location));
    }

    internal static void ReportMemberFuncFirstParameterMustMatchOwner(this ErrorWarrningBag bag, TextLocation funcLocation, string name, TypeSymbol owner, TypeSymbol paramType)
    {
        var message = $"The first parameter of member function '{name}' must same as the owner type '{owner}' and not '{paramType}'. Did you forget the owner?";
        bag.Report(message, true, funcLocation);
    }

    internal static void ReportMemberFuncNoParameters(this ErrorWarrningBag bag, TextLocation funcLocation, string name, TypeSymbol owner)
    {
        var message = $"Member functions cannot take 0 parameters. The member function '{name}' must a parameter of type '{owner}' as its first";
        bag.Report(message, true, funcLocation);
    }

    internal static void ReportTypeNotFound(this ErrorWarrningBag bag, string name, TextLocation loc)
    {
        var message = $"The type '{name}' could not be found (are you sure it is spelt right?)";
        bag.Report(message, true, loc);
    }

    internal static void ReportNonGenericType(this ErrorWarrningBag bag, string name, TextLocation loc)
    {
        var message = $"The non-generic type '{name}' can not be used with type arguments";
        bag.Report(message, true, loc);
    }

    internal static void ReportCannotInstantiateTypeWithNew(this ErrorWarrningBag bag, TypeSymbol guiltyType, TextLocation loc)
    {
        var message = $"The type '{guiltyType}' cannot be instantiated with 'new' as it is either a primitive or a type parameter";
        bag.Report(message, true, loc);
    }

    internal static void ReportTypeHasNoSuchMember(this ErrorWarrningBag bag, TypeSymbol type, SyntaxToken member)
    {
        var message = $"The type '{type}' contains no definition for '{member.Name}' (are you sure it is spelt right?)";
        bag.Report(message, true, member.Location);
    }

    internal static void ReportCannotAssignToReadonlyMember(this ErrorWarrningBag bag, TypeSymbol type, string memberName, TextLocation location)
    {
        var message = $"Cannot assign to field '{type}.{memberName}' -- it is readonly";
        bag.Report(message, true, location);
    }

    internal static void ReportFunctionMismatchingTypeParameters(this ErrorWarrningBag bag, TextLocation location, int received, int expected)
    {
        var message = $"Generic function required {expected} type arguments but got {received}";
        bag.Report(message, true, location);
    }

    internal static void ReportGenericTypeMismatchingTypeArguments(this ErrorWarrningBag bag, string name, int received, int expected, TextLocation location)
    {
        var message = $"Using the generic type '{name}' requires {expected} type arguments but got {received}";
        bag.Report(message, true, location);
    }

    internal static void ReportFeatureNotImplemented(this ErrorWarrningBag bag, TextLocation location, string msg)
    {
        bag.Report("FEATURE MISSING - " + msg, true, location);
    }

    internal static void ReportVariablesCapturedByClosureAreLocal(this ErrorWarrningBag bag, TextLocation location, string name)
    {
        var message = $"'{name}' is captured by a closure, if you want mutability consider using a reference type (object, list).";
        bag.Report(message, false, location);
    }
    internal static void ReportTooManyParametersInLocal(this ErrorWarrningBag bag, FunctionSymbol symbol, TextLocation location, int maxLocalParameters)
    {
        var message = $"Local function '{symbol.Name}' has more than {maxLocalParameters} parameters, consider using an object";
        bag.Report(message, true, location);
    }

    internal static void ReportCannotAssignVoidToImplicitlyTypedVariable(this ErrorWarrningBag bag, SyntaxToken nameToken, TextLocation location)
    {
        var message = $"Cannot assign void to implicitly typed variable '{nameToken.Name}'";
        bag.Report(message, true, TextLocation.FromTo(nameToken.Location, location));
    }

    internal static void ReportTypeVoidIsNotValidHere(this ErrorWarrningBag bag, TextLocation location)
    {
        var message = $"The type 'void' is not valid in this context";
        bag.Report(message, true, location);
    }
}