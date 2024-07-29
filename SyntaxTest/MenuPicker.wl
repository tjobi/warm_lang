menuPicker();

function menuPicker() string {
    int menuLength = 3;     //There is no way to "get" the length fo a list, a little cringe...
    string[] choices = ["Pancake", "Wedding Cake", "Brunsviger"];

    stdWriteLine("Welcome to our dessert factory");
    stdWriteLine("Today's menu:");

    int i = 0;
    while i < menuLength : i = i + 1 {
        string toPrint = string(i + 1) + ": " + choices[i];
        stdWriteLine(toPrint);
    }

    stdWriteLine("Pick a dessert by typing in its corresponding number:");
    stdWrite("> ");

    string input = stdRead();

    string res = "";

    if isNumber(input) {
        //int choice = int(input) - 1;  //This is still very valid, int(someStr) will convert it to an int for now
        //but that ain't that much fun
        int choice = stringToInt(input) - 1;
        if choice >= 0 && choice < menuLength {
            res = choices[choice];
            stdWriteLine("You've chosen \"" + res + "\". Good choice!");
        } else if choice >= menuLength {
            stdWriteLine("You've selected a dessert ID that is too large");
        } else {
            stdWriteLine("You've selected a dessert ID that is too small");
        }
    } else {
        stdWriteLine("Not cool broski, I asked for a number!");
    }
    return res;
}

function isNumber(string s) bool {
    int i = 0;
    while i < strLen(s) : i = i + 1 {
        int charAtI = s[i];
        if charAtI > 57 || charAtI < 48 && charAtI != 45 { // 57 is the decimal value for '9'
            return false;
        }
    }
    return true;
}

function stringToInt(string s) int {
    int i = strLen(s) - 1;
    int res = 0;
    int tenth = 1;
    while i >= 0 : i = i - 1, tenth = tenth * 10 {
        if i == 0 && s[i] == 45 { //is the string prefixed with a minus?
            return -res;
        }
        res = res + tenth * (s[i] - 48);
    }

    return res; 
}