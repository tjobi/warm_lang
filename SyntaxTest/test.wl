// Next step could easily be adding support for generic types
// That is to allow us to do 
//    type myType<T> = { T myField1; T[] myTs; } etc.
// Also, if we could manage
//    function myType<T>.add(myType<T> self, T t) { self.myTs :: t; }
// I am not sold on the syntax - but the idea is there :)
//
// A next step could also be nuking removing the TypeParameterSymbol :)

function main() {
    int[] xs = [];
    int ads = (<- xs) + 1;



    first<int>(listId([]) :: 2, 0);
    string x = "hi";
    stdWriteLine(string(f<int, string>(2, x)));
    stdWriteLine(string(safeIndex<int>([1,2,3,4,5,6], 5, -1)));
    
    B b = new B{};
    function id<T>(T t) T { return t; }
    id<int>(1);

    f<int, int[]>(0, []);

    stdWriteLine(string([b][0].id<int>(1)));

}

function f<T,X>(T t, X x) X[] {
    return [x];
}

function listId(int[] is) int[] { return is; }

//Imagine anonymous functions - and we are back in scala land !!
function first<T>(T[] xs, T orElse) T { 
    if xs.len > 0 {
        return xs[0];
    }
    return orElse;
}

function safeIndex<T>(T[] xs, int i, T orElse) T {
    if xs.len > i && i >= 0 {
        return xs[i];
    }
    return orElse;
}

type B = {}

function B.id<T>(B self, T t) T { return t; }

// // function main() {
// //     // id<int[]>([]);
// //     int[] xs = [1];
// //     //The emitter doesn't know what to do with type list<T> that is created as the parameter of id2...
// //     id2<int>(xs, 1);
// //     stdWriteLine(string(id2<int>(xs, 1)));
// //     stdWriteLine(string(id<int>(2)));
    
// //     Bingo b = new Bingo{};
// //     b.setValue(42);
// //     BingoConsumer(b);
    
// //     string(2);
// //     stdWriteLine(id<Bingo>(b).toString());

// //     //id<int[]>([]);
// //     ID([]);
// //     return;
// // }

// // function id2<T>(T[] ts, T t) T {
// //     return t;
// // }

// // function add<T>(T[] xs, T x) T[] {
// //     return xs :: x;
// // }

// // FIXME: May want BinderTypeScope to handle the list<unknown>, the unify:

// // function id<T>(T t) T {
// //     return t;
// // }
// // type Bingo = { int value; } 

// // type Bingos = {Bingo[] bs; }

// // function BingoConsumer(Bingo b) int {
// //     stdWriteLine(string(b.value));
// //     return b.value;
// // }

// // function Bingo.setValue(Bingo self, int val) {
// //     self.value = val;
// //     return;
// // }

// // function int.toString(int self) string { return string(self); }

// // function Bingo.toString(Bingo self) string {
// //     return "Bingo{ value=" + string(self.value) + " }";
// // }

// // function int[].length(int[] self) int {return self.len;}


// // function ID(int[] is) int[] { return is; }

// // function main() {
// //     [1,2,3].length();
// //     Bingo b = new Bingo{};
// //     b.setValue(42);
// //     BingoConsumer(b);
    
// //     string(2);
// //     string(id<Bingo>(b));



// //     // string(id<int>(2));

// // }