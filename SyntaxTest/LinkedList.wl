type LinkedListNode<T> = { LinkedListNode<T> next; T value; }
type LinkedList<T> = { LinkedListNode<T> head; }

function main() {
    LinkedList<int> q = createLinkedList();
    q.enqueue(1);
    q.enqueue(2);
    stdWriteLine(q.toString());
    stdWriteLine("dequeued: " + string(q.dequeue()));
    stdWriteLine(q.toString());

    int x = createLinkedList().popEnd();
    stdWriteLine(string(x));

    stdWriteLine(fromList([1,2,3,4]).toString());
}

function LinkedListNode<T>.toString<T>(LinkedListNode<T> self) string {
    return "LinkedListNode { " + string(self.value) + ", " + self.next.toString<T>() + " }";   
}

function createLinkedList<T>() LinkedList<T> {
    return new LinkedList<T>{ head = null };
}

function fromList<T>(T[] ts) LinkedList<T> {
    LinkedList<T> lst = createLinkedList();
    int i = 0;
    while i < ts.len : i = i + 1 {
        lst.push(ts[i]);
    }
    return lst;
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