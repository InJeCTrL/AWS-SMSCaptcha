using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace sendPasscodeLambda;

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
    /// Send passcode to target
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

        var permissionId = Environment.GetEnvironmentVariable("PERMISSIONID")!;

        if (!lambdaEvent.ContainsKey("rawQueryString") ||
            string.IsNullOrEmpty(lambdaEvent["rawQueryString"]!.ToString()) ||
            permissionId != lambdaEvent["rawQueryString"]!.ToString())
        {
            return result;
        }

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

        if (!props.ContainsKey("expire_duration") ||
            !props.ContainsKey("code") ||
            !props.ContainsKey("prefix") ||
            !props.ContainsKey("tip") ||
            !props.ContainsKey("phone"))
        {
            return result;
        }

        var passcode = new Passcode
        {
            api_key = permissionId,
            code = props["code"],
            expire_duration = int.Parse(props["expire_duration"])
        };

        using (AmazonSimpleNotificationServiceClient smsclient = new AmazonSimpleNotificationServiceClient())
        {
            PublishRequest publishReq = new PublishRequest()
            {
                Message = $"[{props["prefix"]}] Your one-time passcode is {props["code"]}. {props["tip"]}",
                PhoneNumber = props["phone"],
                MessageAttributes = new()
                {
                    { "AWS.SNS.SMS.SMSType", new MessageAttributeValue()
                    { DataType = "String", StringValue = "Transactional" } }
                }
            };

            try
            {
                PublishResponse response = await smsclient.PublishAsync(publishReq);
                result["body"]!.AsObject().Add(
                    "sendResponse", JsonSerializer.SerializeToNode<PublishResponse>(response));
                result["statusCode"] = 200;
                result["body"]!.AsObject().Add(
                    "verifyCallback", Environment.GetEnvironmentVariable("VERIFYURL")!);

                using (AmazonDynamoDBClient dBClient = new AmazonDynamoDBClient())
                {
                    using (DynamoDBContext dBContext = new DynamoDBContext(dBClient))
                    {
                        await dBContext.SaveAsync<Passcode>(passcode, passcodesTableConfig);
                        result["body"]!.AsObject().Add(
                            "passcode", JsonSerializer.SerializeToNode<Passcode>(passcode));
                    }
                }
            }
            catch(Exception ex)
            {
                result["body"]!.AsObject().Add(
                    "errorTip", ex.ToString());
            }
        }

        return result;
    }
}
