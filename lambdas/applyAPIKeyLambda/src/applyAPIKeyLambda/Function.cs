using System.Text.Json.Nodes;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace applyAPIKeyLambda;

public class Function
{
    
    /// <summary>
    /// Apply for API key
    /// </summary>
    /// <param name="lambdaEvent"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public JsonObject FunctionHandler(JsonObject lambdaEvent, ILambdaContext context)
    {
        var jsonObject = new JsonObject();
        return lambdaEvent;
    }
}
