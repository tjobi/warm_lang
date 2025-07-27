//Why does List` have to have List`x (surely we can find T by name)
// How do we differentiate List`<T> and List`<T> ... 

// function myFunc(int i) int {
//     return i;
// }

// function int.print(int self) {
//     stdWriteLine(string(self));
// }

// function generic<T>(T t) T { return t; }

type List<T> = { T hd; List<T> tail; }

//TODO: Fix this for generics :)
function List<int>.filter(List<int> self, Func<int, bool> predicate) List<int> {
    List<int> head = null;
    List<int> tail = null;

    while self != null : self = self.tail {
        if predicate(self.hd) {
            var newNode = new List<int> { hd = self.hd, tail = null };

            if head == null {
                head = tail = newNode;
            } else {
                tail = tail.tail = newNode;
            }
        }
    }

    return head;
}

// function List<T>.count<T>(List<T> self) int {
//     int res = 1;
//     while self.next() != null : self = self.next(), res = res + 1{ }
//     return res;
// }

// function T[].count<T>(T[] self) int {
//     return self.len;
// }

// function List<T>.next<T>(List<T> self) List<T> { 
//     return self.tail;
// }

// function List<T>.toList<T>(List<T> self) T[] {
//     T[] res = [self.hd];
//     while (self = self.next()) != null : res :: self.hd { }
//     return res;
// }

function greaterEqualsTwo(int i) bool  { return i >= 2; }

//TODO: not yet
//function myFilterGenerator(int treshold) Func<int,bool> { return (int i) => i >= treshold; }

function main() { 
    List<int> l = new List<int>{ hd = 2, tail = new List<int>{hd = 1, tail = null} };
    // List<int> lEmpty = null;
    // int i = generic(1);
    // l.count();
    // var l2 = l.next().count();
    // stdWriteLine(string(l.toList().count()));
    // var f = l.toList;
    
    stdWriteLine(string( (int i) => true )); 
    stdWriteLine(string(l.filter((int i) => i >= 1)));
    stdWriteLine(string(l.filter(greaterEqualsTwo)));
    
    //TODO: Fix parser, it parses as ((generic < int) > ParseErr(48,26)) ... cringe
    // var id = generic<int>;

    //TODO: create closures!
    // FAILS - but shouldn't it just lock-in the 2? and then it becomes a function of () -> void
    // var twoPrinter = 2.print;
    // twoPrinter();

    
    //TODO: Closure goes wrong - it loops infinitely on the outer i...
    //int i = 5;
    //function inner(int i) int { if i < 10 { return inner(i+1); } else { return i; } }
}