type LinkedListNode<T> = { LinkedListNode<T> next; T value; }
type LinkedList<T> = { LinkedListNode<T> head; }

function main() {
    var q = createLinkedList<int>();
    q.enqueue(1);
    q.enqueue(2);
    stdWriteLine(q.toString());
    stdWriteLine("with sum of: " + string(q.sum()));
    stdWriteLine("dequeued: " + string(q.dequeue()));
    stdWriteLine(q.toString());

    int x = createLinkedList().popEnd();
    stdWriteLine(string(x));

    //TODO: why does this need an explicit int - hmm
    //      `LinkedList<int> someInts = fromList<int>([1,2,3,4]);`
    //      it is fixed by var but there is a buuuuuug here ;(
    var someStrings = fromList(["how", "is", "it", "going", "?"]);
    stdWriteLine(someStrings.toString());
    someStrings.reverse();
    stdWriteLine(someStrings.toString());
    //Same as doing someStrings.map(...)
    var someInts = LinkedList.map(someStrings, (string s) => s.len);
    var target = 3;
    stdWriteLine(someInts.filter((i) => i >= target).toString());
}

function createLinkedList<T>() LinkedList<T> {
    return new LinkedList<T>{ head = null };
}

function fromList<T>(T[] ts) LinkedList<T> {
    var lst = createLinkedList<T>();
    var i = 0;
    while i < ts.len : i = i + 1 {
        lst.enqueue(ts[i]);
    }
    return lst;
}

function LinkedList<T>.reverse<T>(LinkedList<T> self) {
    LinkedListNode<T> w = null;
    LinkedListNode<T> t = null;
    LinkedListNode<T> v = self.head;
    while v != null {
        t = v.next;
        v.next = w;
        w = v;
        v = t;
    }
    self.head = w;
}

function LinkedList<T>.push<T>(LinkedList<T> self, T e) LinkedList<T> {
    LinkedListNode<T> newHead = new LinkedListNode<T>{ next = self.head, value = e };
    self.head = newHead;
    return self;
}

function LinkedList<T>.enqueue<T>(LinkedList<T> self, T e) LinkedList<T> {
    LinkedListNode<T> tail = new LinkedListNode<T>{ value = e };
    if(self.head == null) {
        self.head = tail;
        return self;
    }
    LinkedListNode<T> cur = self.head;
    while cur.next != null : cur = cur.next { }
    cur.next = tail;
    return self;
}

function LinkedList<T>.pop<T>(LinkedList<T> self) T {
    if self.head != null {
        LinkedListNode<T> tmp = self.head;
        self.head = tmp.next;
        return tmp.value;
    }
    return null;
}

function LinkedList<T>.dequeue<T>(LinkedList<T> self) T {
    return self.pop();
}

function LinkedList<T>.popEnd<T>(LinkedList<T> self) T {
    if(self.head == null) {
        return null;
    }
    LinkedListNode<T> cur = self.head;
    LinkedListNode<T> prev = null;
    while cur.next != null : prev = cur, cur = cur.next { }
    prev.next = null;
    return cur.value;
}

function LinkedList<T>.toString<T>(LinkedList<T> self) string {
    string out = "[ ";
    if self.head != null {
        LinkedListNode<T> cur = self.head;
        while cur.next != null : cur = cur.next {
            out = out + string(cur.value) + " -> ";
        }
        out = out + string(cur.value);
    }
    return out + " ]";
}

function LinkedList<T>.filter<T>(LinkedList<T> self, Func<T, bool> predicate) LinkedList<T> {
    var l = createLinkedList<T>();
    var cur = self.head;
    while cur != null : cur = cur.next {
        if predicate(cur.value) { l.enqueue(cur.value); }
    }
    return l;
}

function LinkedList<A>.fold<A, B>(LinkedList<A> self, B z, Func<B, A, B> op) B {
    var acc = z;
    var cur = self.head;
    while cur != null : cur = cur.next {
        acc = op(acc, cur.value);
    }
    return acc;
}

function LinkedList<int>.sum(LinkedList<int> self) int {
    return self.fold(0, (int a, int b) => a + b);
}

function LinkedList<A>.map<A,B>(LinkedList<A> self, Func<A,B> mapper) LinkedList<B> {
    var hd = createLinkedList<B>();
    var cur = self.head;
    while cur != null : cur = cur.next{
        hd.enqueue(mapper(cur.value));
    }
    return hd;
}