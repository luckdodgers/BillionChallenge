var path = Directory.GetCurrentDirectory() + "/measurements3.txt";
var file = File.ReadAllText(path);
Console.Write(file);
Console.ReadKey();
