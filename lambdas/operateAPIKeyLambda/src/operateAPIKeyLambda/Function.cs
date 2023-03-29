using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace operateAPIKeyLambda;

public class Function
{
    public class APIKey
    {
        [DynamoDBHashKey]
        public string guid { get; set; } = Guid.NewGuid().ToString();
        [DynamoDBHashKey]
        public int available_cnt { get; set; } = 0;
        public int total_cnt { get; set; } = 0;
        public long apply_time { get; set; } =
            (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
        public long approve_time { get; set; } = 0;
    }

    public class Operator
    {
        [DynamoDBHashKey]
        public string guid { get; set; } = Guid.NewGuid().ToString();
        public string nickname { get; set; } = "Test";
    }
    /// <summary>
    /// Set API Key capacity and availability
    /// </summary>
    /// <param name="lambdaEvent"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<JsonObject> FunctionHandler(JsonObject lambdaEvent, ILambdaContext context)
    {
        JsonObject result = new();
        result.Add("statusCode", 400);
        result.Add("headers", new JsonObject());
        result["headers"]!.AsObject().Add("Access-Control-Allow-Origin",
            Environment.GetEnvironmentVariable("CORSORIGIN"));

        if (!lambdaEvent.ContainsKey("rawQueryString") ||
            string.IsNullOrEmpty(lambdaEvent["rawQueryString"]!.ToString()))
        {
            return result;
        }

        var apiKeysTableConfig = new DynamoDBOperationConfig();
        apiKeysTableConfig.OverrideTableName = Environment.GetEnvironmentVariable("APIKEYSTABLE");
        var operatorsTableConfig = new DynamoDBOperationConfig();
        operatorsTableConfig.OverrideTableName = Environment.GetEnvironmentVariable("OPERATORSTABLE");

        var opGuid = lambdaEvent["rawQueryString"]!.ToString();

        using (AmazonDynamoDBClient client = new AmazonDynamoDBClient())
        {
            using (DynamoDBContext dBContext = new DynamoDBContext(client))
            {
                var operatorObj = await dBContext.LoadAsync<Operator>(opGuid, operatorsTableConfig);
                if (operatorObj == null)
                {
                    return result;
                }
            }
        }

        if (!lambdaEvent.ContainsKey("data") ||
            string.IsNullOrEmpty(lambdaEvent["data"]!.ToString()))
        {
            using (AmazonDynamoDBClient client = new AmazonDynamoDBClient())
            {
                using (DynamoDBContext dBContext = new DynamoDBContext(client))
                {
                    var apiKeyObj = await dBContext. .LoadAsync<Operator>(guid, operatorsTableConfig);
                    if (apiKeyObj == null)
                    {
                        return result;
                    }
                }
            }
        }


        var data = Encoding.Default.GetString(
            Convert.FromBase64String(lambdaEvent["data"]!.ToString()));

        var props = new Dictionary<string, string>();
        var propParts = data.Split('&');
        foreach (var prop in propParts)
        {
            var fieldValue = prop.Split('=');
            var field = fieldValue[0];
            var value = fieldValue[1];

            props.Add(field, value);
        }

        if (props.ContainsKey(""))

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
