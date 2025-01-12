int[] xs = [1,2,3];

function safeIndex(int i) int {
    if xs.len < i {
        return xs.len-1;
    }
    return i;
}
xs = xs + [];
xs[safeIndex(5000)];