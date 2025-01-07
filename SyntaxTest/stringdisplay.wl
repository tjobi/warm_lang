type StringDisplay = {
    string text;
    int displayLength;
    bool moveCursor;
}

function main() {
    StringDisplay display = new StringDisplay { 
        text = "PUT SOME TEXT HERE", 
        displayLength = 20,
        moveCursor = false
    };
    display.show(); 
}

function StringDisplay.show(StringDisplay self) {
    string s = self.text;
    int from = 0;
    int to = s.len - 1;
    int printcol = self.displayLength - 1;
    while true {
        stdClear();
        int i = 0;
        int nextIdx = 0;
        while i < self.displayLength : i = i + 1 {
            if i >= printcol && nextIdx + from <= to {
                stdWritec(s[(from + nextIdx)]);
                nextIdx = nextIdx + 1;
            } else {
                stdWrite(" ");
            }
        }
        
        if printcol <= 0 {
            if from != to {
                from = from + 1;
            } else {
                from = 0;
                to = s.len - 1;
                printcol = self.displayLength-1;
            }
        } else {
            printcol = printcol - 1;
        }
        if self.moveCursor {
            stdWritec(13); //write \r
        }
        
        //2147483600 <- MAX INT
        // Empty loop to waste some time :D - busy waiting
        while i < 715827866 : i = i + 1 { }
    }
}