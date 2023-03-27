import * as cdk from 'aws-cdk-lib';
import { Construct } from 'constructs';
import * as lambda from 'aws-cdk-lib/aws-lambda'
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import * as sns from 'aws-cdk-lib/aws-sns'
import * as path from 'path'
import { Runtime } from 'aws-cdk-lib/aws-lambda';

export class CdkStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const topic = new sns.Topic(this, 'passcodeTopic');

    const operatorsTable = new dynamodb.Table(this, "AWS_SMSCaptcha_Operators", {
      partitionKey: {
        name: 'guid',
        type: dynamodb.AttributeType.STRING
      }
    });

    const APIKeysTable = new dynamodb.Table(this, "AWS_SMSCaptcha_API_Keys", {
      partitionKey: {
        name: 'guid',
        type: dynamodb.AttributeType.STRING
      }
    });

    const passcodeTable = new dynamodb.Table(this, "AWS_SMSCaptcha_Codes", {
      partitionKey: {
        name: 'guid',
        type: dynamodb.AttributeType.STRING
      },
      sortKey: {
        name: 'api_key',
        type: dynamodb.AttributeType.STRING
      }
    });

    const bundlingCommands = [
        "cd /asset-input",
        "export XDG_DATA_HOME=\"/tmp/DOTNET_CLI_HOME\"",
        "export DOTNET_CLI_HOME=\"/tmp/DOTNET_CLI_HOME\"",
        "export PATH=\"$PATH:/tmp/DOTNET_CLI_HOME/.dotnet/tools\"",
        "dotnet tool install -g Amazon.Lambda.Tools",
        "dotnet lambda package -o output.zip",
        "unzip -o -d /asset-output output.zip"
    ];

    const assetOpt = {
      bundling: {
        image: Runtime.DOTNET_6.bundlingImage,
        command: ["bash", "-c", bundlingCommands.join(" && ")]
      }
    };

    const applyAPIKeyLambda = new lambda.Function(this, "applyAPIKeyLambda", {
      runtime: lambda.Runtime.DOTNET_6,
      handler: "applyAPIKeyLambda::applyAPIKeyLambda.Function::FunctionHandler",
      code: lambda.Code.fromAsset(
        path.join(__dirname, "../../lambdas/applyAPIKeyLambda/src/applyAPIKeyLambda"), assetOpt)
    });

    const operateAPIKeyLambda = new lambda.Function(this, "operateAPIKeyLambda", {
      runtime: lambda.Runtime.DOTNET_6,
      handler: "operateAPIKeyLambda::operateAPIKeyLambda.Function::FunctionHandler",
      code: lambda.Code.fromAsset(
        path.join(__dirname, "../../lambdas/operateAPIKeyLambda/src/operateAPIKeyLambda"), assetOpt)
    });

    const sendPasscodeLambda = new lambda.Function(this, "sendPasscodeLambda", {
      runtime: lambda.Runtime.DOTNET_6,
      handler: "sendPasscodeLambda::sendPasscodeLambda.Function::FunctionHandler",
      code: lambda.Code.fromAsset(
        path.join(__dirname, "../../lambdas/sendPasscodeLambda/src/sendPasscodeLambda"), assetOpt),
    });

    const verifyPasscodeLambda = new lambda.Function(this, "verifyPasscodeLambda", {
      runtime: lambda.Runtime.DOTNET_6,
      handler: "verifyPasscodeLambda::verifyPasscodeLambda.Function::FunctionHandler",
      code: lambda.Code.fromAsset(
        path.join(__dirname, "../../lambdas/verifyPasscodeLambda/src/verifyPasscodeLambda"), assetOpt),
    });

    const funcUrlOptions = {
      authType: lambda.FunctionUrlAuthType.NONE,
      cors: {
        allowedOrigins: [process.env.CORS_ORIGIN || '']
      }
    };

    const applyAPIKeyLambdaURL = applyAPIKeyLambda.addFunctionUrl(funcUrlOptions);
    const operateAPIKeyLambdaURL = operateAPIKeyLambda.addFunctionUrl(funcUrlOptions);
    const sendPasscodeLambdaURL = sendPasscodeLambda.addFunctionUrl(funcUrlOptions);
    const verifyPasscodeLambdaURL = verifyPasscodeLambda.addFunctionUrl(funcUrlOptions);
  }
}
