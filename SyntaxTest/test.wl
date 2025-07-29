

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

function List<T>.count<T>(List<T> self) int {
    if self == null { return 0; }
    int res = 1;
    while self.tail != null : self = self.tail, res = res + 1{ }
    return res;
}

function T[].count<T>(T[] self) int {
    return self.len;
}

function List<T>.next<T>(List<T> self) List<T> { 
    return self.tail;
}

function List<T>.toList<T>(List<T> self) T[] {
    T[] res = [self.hd];
    while (self = self.next()) != null : res :: self.hd { }
    return res;
}

function listobjFromList<T>(T[] l) List<T> {
    var front = new List<T>{tail = null};
    var tail = front;
    var i = 0;
    while i < l.len : i = i + 1 {
        tail.hd = l[i];
        if i < l.len - 1 {
            tail = tail.tail = new List<T>{ tail = null };
        }
    }
    return front;
}

function List<T>.toString<T>(List<T> self) string {
    if self == null { return "[[]]"; }
    var out = "[[";
    while self != null : self = self.tail {
        out = out + string(self.hd);
        if self.tail != null {
            out = out + ", ";
        }
    }
    return out + "]]";
}

function List<T>.filter<T>(List<T> self, Func<T, bool> predicate) List<T> {
    List<T> head = null;
    List<T> tail = null;

    while self != null : self = self.tail {
        if predicate(self.hd) {
            var newNode = new List<T> {hd = self.hd, tail = null };

            if head == null {
                head = tail = newNode;
            } else {
                tail = tail.tail = newNode;
            }
        }
        
    }
    return head;
}

function greaterEqualsTwo(int i) bool  { return i >= 2; }

//TODO: not yet
//function myFilterGenerator(int treshold) Func<int,bool> { return (int i) => i >= treshold; }
function greaterEquals5Generator() Func<int,bool> { return (int i) => i >= 5; }

function main() { 
    var l = listobjFromList([1,2,3,4,5]);
    
    stdWriteLine(string( (int i) => true )); //TODO: pretty printing?
    stdWriteLine(l.filter((int i) => i >= 1).toString());
    stdWriteLine(l.filter(greaterEquals5Generator()).toString());
    stdWriteLine(l.filter(greaterEqualsTwo).toString());
    stdWriteLine(l.filter((i) => false).toString());

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