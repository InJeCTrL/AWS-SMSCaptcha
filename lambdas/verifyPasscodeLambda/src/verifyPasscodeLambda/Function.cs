using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace verifyPasscodeLambda;

public class Function
{
    public class Passcode
    {
        [DynamoDBHashKey]
        public string guid { get; set; } = Guid.NewGuid().ToString();
        public string api_key { get; set; }
        public string code { get; set; }
        public long expire_duration { get; set; }
        public long create_time { get; set; } =
            (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
        public long verified_time { get; set; } = 0;
    }
    /// <summary>
    /// Verify one-time passcode
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
        result.Add("body", new JsonObject());

        var passcodesTableConfig = new DynamoDBOperationConfig();
        passcodesTableConfig.OverrideTableName = Environment.GetEnvironmentVariable("PASSCODETABLE");

        var data = Encoding.Default.GetString(
            Convert.FromBase64String(lambdaEvent["body"]!.ToString()));

        var props = new Dictionary<string, string>();
        var propParts = data.Split('&');
        foreach (var prop in propParts)
        {
            var fieldValue = prop.Split('=');
            var field = fieldValue[0];
            var value = fieldValue[1];

            props.Add(field, value);
        }

        if (!props.ContainsKey("code") ||
            !props.ContainsKey("guid") ||
            !props.ContainsKey("api_key"))
        {
            return result;
        }

        using (AmazonDynamoDBClient client = new AmazonDynamoDBClient())
        {
            using (DynamoDBContext dBContext = new DynamoDBContext(client))
            {
                var passcodeObj = await dBContext.LoadAsync<Passcode>(props["guid"], passcodesTableConfig);
                if (passcodeObj == null)
                {
                    result["body"]!.AsObject().Add(
                        "errorTip", "incorrect guid");
                    return result;
                }

                if (passcodeObj.api_key != props["api_key"])
                {
                    result["body"]!.AsObject().Add(
                        "errorTip", "incorrect api_key");
                    return result;
                }

                if (passcodeObj.code != props["code"])
                {
                    result["body"]!.AsObject().Add(
                        "errorTip", "incorrect passcode");
                    return result;
                }

                if (passcodeObj.verified_time != 0)
                {
                    result["body"]!.AsObject().Add(
                        "errorTip", "reverify passcode");
                    return result;
                }

                var timestampNow = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
                if (passcodeObj.expire_duration + passcodeObj.create_time < timestampNow)
                {
                    result["body"]!.AsObject().Add(
                        "errorTip", "exceed expire time");
                    return result;
                }

                passcodeObj.verified_time = timestampNow;
                await dBContext.SaveAsync<Passcode>(passcodeObj, passcodesTableConfig);
                result["statusCode"] = 200;
                result["body"]!.AsObject().Add(
                        "passCode", JsonSerializer.SerializeToNode<Passcode>(passcodeObj));
            }
        }

        return result;
    }
}
