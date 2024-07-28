int[] xs = [1,2,3];
int xsLength = 3;

function safeIndex(int i) int
{
    if xsLength < i {
        xsLength-1;
    }
    else {
        i;
    }
}

xs[safeIndex(5000)];