type LinkedListNode<T> = { LinkedListNode<T> next; T value; }
type LinkedList<T> = { LinkedListNode<T> head; }

function main() {
    LinkedList<int> q = createLinkedList<int>();
    q.enqueue<int>(1);
    q.enqueue<int>(2);
    stdWriteLine(q.toString<int>());
    stdWriteLine("dequeued: " + string(q.dequeue<int>()));
    stdWriteLine(q.toString<int>());

    int x = createLinkedList<int>().popEnd<int>();
    stdWriteLine(string(x));
}

function LinkedListNode<T>.toString<T>(LinkedListNode<T> self) string {
    return "LinkedListNode { " + string(self.value) + ", " + self.next.toString<T>() + " }";   
}

function createLinkedList<T>() LinkedList<T> {
    return new LinkedList<T>{ head = null };
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
    return self.pop<T>();
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