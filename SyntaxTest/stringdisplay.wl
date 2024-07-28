function main() {
    int OUTPUTLEN = 20;
    string s = "PUT SOME STRING HERE"; 
    int sLength = strLen(s);

    int from = 0;
    int to = sLength - 1;

    int printcol = OUTPUTLEN - 1;
    while true {
        stdClear();
        int i = 0;
        int nextIdx = 0;
        while i < OUTPUTLEN : i = i + 1 {
            if i >= printcol && nextIdx + from <= to {
                //stdWriteLine(string(from) + " -> " + string(to));
                stdWritec(s[(from + nextIdx)]);
                nextIdx = nextIdx + 1;
            } else {
                stdWrite(" ");
            }
        }
        
        if printcol <= 0 {
            if from != to
            {
                from = from + 1;
            } else {
                from = 0;
                to = sLength - 1;
                printcol = OUTPUTLEN-1;
            }
        } else {
            printcol = printcol - 1;
        } 
        
        //2147483600 <- MAX INT
        // Empty loop to waste some time :D
        while i < 715827866 : i = i + 1 { }
    }
}