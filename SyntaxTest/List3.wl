function makeArr(int start, int size, int step) int[]
{
    function inner(int i, int cur, int[] res) int[]
    {
        if i < size {
            int next = cur + step;
            return inner(i+1, next, res :: next);
        } 
        return res;
    }
    return inner(0,start,[]);
}

function main () 
{
    // [2, 4, 6, 8, 10, 12, 14, 16, 18, 20]
    stdWriteLine(string(makeArr(0,10,2)));
}