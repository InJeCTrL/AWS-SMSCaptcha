using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace applyAPIKeyLambda;

public class Function
{
    public class APIKey
    {
        [DynamoDBHashKey]
        public string guid { get; set; } = Guid.NewGuid().ToString();
        public int available_cnt { get; set; } = 0;
        public int total_cnt { get; set; } = 0;
        public long apply_time { get; set; } =
            (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
        public long approve_time { get; set; } = 0;
    }

    /// <summary>
    /// Apply for API key
    /// </summary>
    /// <param name="lambdaEvent"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<JsonObject> FunctionHandler(JsonObject lambdaEvent, ILambdaContext context)
    {
        JsonObject result = new();
        result.Add("statusCode", 200);
        result.Add("headers", new JsonObject());
        result["headers"]!.AsObject().Add("Access-Control-Allow-Origin",
            Environment.GetEnvironmentVariable("CORSORIGIN"));

        var config = new DynamoDBOperationConfig();
        config.OverrideTableName = Environment.GetEnvironmentVariable("APIKEYSTABLE");

        if (lambdaEvent.ContainsKey("rawQueryString") &&
            !string.IsNullOrEmpty(lambdaEvent["rawQueryString"]!.ToString()))
        {
            var guid = lambdaEvent["rawQueryString"]!.ToString();

            using (AmazonDynamoDBClient client = new AmazonDynamoDBClient())
            {
                using (DynamoDBContext dBContext = new DynamoDBContext(client))
                {
                    var apiKeyObj = await dBContext.LoadAsync<APIKey>(guid, config);
                    if (apiKeyObj == null)
                    {
                        result["statusCode"] = 404;
                    }
                    else
                    {
                        result.Add("body", JsonSerializer.Serialize<APIKey>(apiKeyObj));
                    }
                }
            }
        }
        else
        {
            var apiKey = new APIKey();

            using (AmazonDynamoDBClient client = new AmazonDynamoDBClient())
            {
                using (DynamoDBContext dbContext = new DynamoDBContext(client))
                {
                    await dbContext.SaveAsync(apiKey, config);
                    result.Add("body", JsonSerializer.Serialize<APIKey>(apiKey));
                }
            }
        }

        return result;
    }
}
