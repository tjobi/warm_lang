int[] res1 = [];

//Standard while loop as you'd expect
int n = 0;
while n < 5 {
    res1 :: n;
    n = n + 1;
}
// at this point res1 is [0,1,2,3,4]

//After a ":" follows the continue part, which runs after the body
  // at each iteration
int[] res2 = [];
n = 0;
while n < 5 : n = n + 1 { 
    res2 :: n;
}
// at this point res2 is [0,1,2,3,4]

//The continue part also allows for multiple expression using "," (comma)
int[] res3 = [];
n = 0;
while n < 5 : res3 :: n, n = n + 1 { } 
// at this point res3 is [0,1,2,3,4]

res1 + res2 + res3; 
//returns [0,1,2,3,4,0,1,2,3,4,0,1,2,3,4]