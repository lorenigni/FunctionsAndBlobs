using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FunctionApp1;

public class HttpExample
{
    //The Azure Functions project template in Visual Studio creates a C# class library project that you
    // can publish to a function app in Azure.

    //The Http request trigger the function in the function app.
    //An HTTP trigger doesn't use an Azure Storage account connection string;
    // all other trigger types require a valid Azure Storage account connection string.

    private readonly ILogger<HttpExample> _logger;

    public HttpExample(ILogger<HttpExample> logger)
    {
        _logger = logger;
    }

    //The Function method attribute sets the name of the function. The Function attribute marks the method as
    // a function entry point. The name must be unique within a project. The method must be a public member of
    // a public class.
    //The HttpTrigger attribute specifies that the function is triggered by an HTTP request.
    //A function can have zero or more input bindings that can pass data to a function. Like triggers, input
    // bindings are defined by applying a binding attribute to an input parameter. When the function executes,
    // the runtime tries to get data specified in the binding. The data being requested is often dependent on
    // information provided by the trigger using binding parameters.

    //A function can accept a CancellationToken parameter, which enables the operating system to notify your code
    // when the function is about to be terminated. 
    [Function("HttpExample")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, CancellationToken cancellationToken)
    {
        CancellationTokenSource source = new CancellationTokenSource();
        cancellationToken = source.Token;
        source.Cancel();
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        if (cancellationToken.IsCancellationRequested) { _logger.LogInformation("si chiude"); }
        return new OkObjectResult("Welcomedzcdz!");
    }


}
