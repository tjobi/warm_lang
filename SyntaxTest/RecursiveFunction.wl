function incrementBySteps(int start, int incrementAmount, int amountOfTimes) int
{
    int result = start;
    
    //function delcared inside of the other function, we use it to loop (amountOfTimes) :D
    function loop(int cnt) //implicit void
    {
        if cnt > 0 {
            result = result + incrementAmount;
            loop(cnt-1);
        }
    }
    loop(amountOfTimes);
    return result;
}

//Starting at 0 increment by 5, and do it 10 times
int res = incrementBySteps(0, 5, 10);
stdWriteLine(string(res));