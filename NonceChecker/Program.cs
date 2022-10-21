using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

//Store list of nonces in a dictionary so lookup time is O(1) and space is O(n) where n is the number of unique nonces in the text file 
var nonceList = new Dictionary<Guid, DateTime>();
var serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddConsole()
        )
    .BuildServiceProvider();

var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();

try
{
    string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"assets\nonce.txt");

    foreach(var line in File.ReadLines(filePath))
    {
        var currentRecord = line.Split('\t', StringSplitOptions.TrimEntries);
        
        //Attempt to parse nonce and time out of current line
        if(Guid.TryParse(currentRecord[0], out Guid currentNonce) && DateTime.TryParse(currentRecord[1], out DateTime currentTime))
        {
            //If nonce does not exist in the dictionary already, add to collection 
            if (!nonceList.ContainsKey(currentNonce))
            {
                nonceList.Add(currentNonce, currentTime);
            }
            else
            {   
                //Nonce has already appeared once, check to see if it appeared last within 5mins or less
                var previousNonceTime = nonceList[currentNonce];
                var currentNonceTime = currentTime;
                var timeSpan = currentNonceTime - previousNonceTime;
                if(timeSpan.TotalMinutes <= 5)
                {
                    Console.WriteLine($"Duplicate nonce: {currentNonce} current time: {currentTime} last used: {nonceList[currentNonce]}");
                }
                //Update existing nonce with the new time 
                nonceList[currentNonce] = currentNonceTime; 
            }
        }
        else
        {
            logger.LogWarning("Encountered record that did not contain a valid nonce or time, continuing to next line"); 
        }
    }
}
catch(Exception ex)
{
    logger.LogError(ex, "Encountered error reading or parsing from nonce file");
}