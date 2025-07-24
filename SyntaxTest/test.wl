//Why does List` have to have List`x (surely we can find T by name)
// How do we differentiate List`<T> and List`<T> ... 

function myFunc(int i) int {
    return i;
}

function main() { 
    var id = (int i) => i; 
    //
    stdWriteLine(string(myFunc(id(21312))));
    // ((a,b,c,b) => a)(1,2,3,4);
    // stdWriteLine(string(id));
    //It binder creates an error because "id" isn't a function
    // We need to somehow break the depency on FunctionSymbols...
    //    because when we read a variable pointing to some lambda
    //    we do not necessarily have access to its "symbol":, i.e.
    //    when it is passed as an argument to a funciton, we don't anything
    //    outside its type... we need to be able to call something purely
    //    on its type - AND THEN figure out how to link the things back up...
    //    Essentailly, we cannot distinguish between functionsymbols and variable symbols
    //    they are one in the same
    stdWriteLine(string(id(1)));
    stdWriteLine(string(id));
}