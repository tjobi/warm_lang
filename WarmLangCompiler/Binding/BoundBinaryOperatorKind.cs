namespace WarmLangCompiler.Binding;

public enum BoundBinaryOperatorKind 
{
    Addition, Subtraction, Division, Multiplication, Power,
    Equals, NotEquals, LessThan, LessThanEqual, GreaterThan, GreaterThanEqual,
    LogicAND, LogicaOR, 
    
    StringConcat, 
    ListConcat, ListAdd,  
    
     
}