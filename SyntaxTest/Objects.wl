// Create a new type of object, that has a field 'ID' of type 'int'
type MyType = { int ID; }

//You can create consturctors like this or just use the 'new MyType{}' object initializer
function newMyType(int id) MyType { return new MyType{ID = id}; }

//TODO: Something like this should be provided for every type created ... would be nice
function MyType.toString(MyType self) string { return "MyType{" + string(self.ID) + "}"; }

function MyType.equals(MyType self, MyType other) bool {
    return self.ID == other.ID;
}

// Non-generic, it can only hold entries of MyType
type LinkedListNode = {
    MyType data;
    LinkedListNode next;
}

type LinkedList = {
    LinkedListNode head;
}

function newLinkedList() LinkedList {
    return new LinkedList{};
}

//You can add methods to a type by creating of a function of 'TypeToExtend.MethodName'
function LinkedList.add(LinkedList self, MyType e) {
    LinkedListNode newHead = new LinkedListNode{data = e};
    if self.head != null {
        newHead.next = self.head;
    }
    self.head = newHead;
}

function LinkedList.contains(LinkedList self, MyType e) bool {
    LinkedListNode cur = self.head;

    while cur != null : cur = cur.next {
        if e.equals(cur.data) {
            return true;
        }
    }
    return false;
}

function LinkedList.toString(LinkedList self) string {
    LinkedListNode cur = self.head;
    string out = "[ ";

    while cur != null : cur = cur.next {
        MyType data = cur.data;
        out = out + data.toString() + "; ";
    }
    return out + "]";
}

//Let's now use our linked list. 
//When there is no function called 'main' any statements at the top/global level
// are used to create a 'main' function. This will be the entry point for the CLR.
LinkedList myList = newLinkedList();
int n = 10;
int i = 0;
while i < n : i = i+1 {
    myList.add(newMyType(i));
}

stdWriteLine("myList contains MyType{ID = 5} - " + string(myList.contains(new MyType{ID = 5})));
stdWriteLine("-----");
stdWriteLine(myList.toString());
