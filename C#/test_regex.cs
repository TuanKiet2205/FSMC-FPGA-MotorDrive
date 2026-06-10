using System;
using System.Text.RegularExpressions;
class Program {
    static void Main() {
        string input = "target_deg:90, current_deg:45";
        string[] parts = input.Split(',');
        string strTarget = Regex.Match(parts[0], @"-?\d+(\.\d+)?").Value;
        Console.WriteLine("TARGET IS: " + strTarget);
        string strCurrent = Regex.Match(parts[1], @"-?\d+(\.\d+)?").Value;
        Console.WriteLine("CURRENT IS: " + strCurrent);
    }
}
