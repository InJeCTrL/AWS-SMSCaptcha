# AWS-SMSCaptcha
A simple one-time passcode verification function based on AWS.

I used to design this whole project to be a bigger and more perfect one, which can offer a simple panel for  customers and multi-apikey management. Since the SMS service on AWS is not that cheap, I decided to reduce functions, just provide the basic send&verify functions.

This repository consists of 2 parts, infra and lambdas. 
1. infra is one aws-cdk project, which set the infrastructures and deploy them on AWS. 
2. lambdas have 2 lambda projects developed by CSharp. `sendPasscodeLambda` will create passcode record in dynamodb, while `verifyPasscodeLambda` aims at  comparing the input `guid-passcode` pair with the past record, then return verification result.

## Infrastructure Design
![infra](./infra_design.svg)

To make the whole system as cheap as possible, here I don't use RDS, but a DynamoDB. There is only one table `AWS_SMSCaptcha_Codes` inside now, which contains one-time passcode information. Explanation of each field:
1. guid (Primary Key): Global unique Id of each one-time passcode. One can find the specific passcode record by this field.
2. api_key: Key of this system. Only permitted users are approved to request this system.
3. code: This is the very passcode. It may be a digit or letter serial.
4. expire_duration: Set the expiration duration(seconds) of the passcode.
5. create_time: Timestamp(UTC) of this passcode record created.
6. verified_time: Timestamp(UTC) of this passcode verified. If this value equals to 0, means this passcode has never been verified.

The implemented name of function `Verify Captcha code` is `sendPasscodeLambda`. Customers will send `POST` request to the exposed URL of this lambda function to verify whether this passcode is right.

The implemented name of function `Send Captcha code` is `sendPasscodeLambda`. The lambda function will first send SMS message to target phone number, then create the one-time passcode record in DynamoDB if the SMS sent successfully.

This SNS topic will send transaction SMS messages to target phone number.**SNS topic need some config, including [origin phone numbers](https://us-west-2.console.aws.amazon.com/pinpoint/home?region=us-west-2#/sms-account-settings/overview) and [leaving the SMS sandbox](https://docs.aws.amazon.com/us_en/sns/latest/dg/sns-sms-sandbox-moving-to-production.html).**

For the WebAPI URLs, the 2 lambda functions will expose their own lambda URL, you can find them through lambda function panel inside AWS Web console.

## Requirement
- Docker
- .Net 6.0 SDK
- AWS CLI
- AWS SDK
- AWS CDK
- AWS Account

## Deploy
Make sure you have configured the environment variables blow:
1. `PERMISSIONID`: The api-key of this captcha service, keep it as secret, and request the service with it.
2. `CORS_ORIGIN`: Allowed Cross-Origin, the domain of your webservice.

Besides the environment variables above, others will be configured automatically if you have setup your AWS CLI.

From the `infra` directory, run:
```shell
cdk deploy
```

## Usage
1. Invoke `sendPasscodeLambda`

    - Method: `POST`

    - URL: `{lambda function url}/?{PERMISSIONID}`

    - Request schema:
        ```js
        expire_duration=300&code=123456&prefix=Test_Corp&tip=Don't share it with others.&phone=+XXXXXXXXX
        ```
    
    - Response schema:
        ```json
        {
            "sendResponse": {},
            "passcode": {
                "guid": "xxx",
                "api_key": "xxx",
                "code": "xxx",
                "expire_duration": xxx,
                "create_time": xxx,
                "verified_time": xxx
            },
            "verifyCallback": "{verifyPasscodeLambda function url}",
            "errorTip": "xxx"
        }
        ```
        If the it error occurs, the HTTP status code will be `400`, and there will be `errorTip` field inside response body.

2. Invoke `verifyPasscodeLambda`
   
   - Method: `POST`

    - URL: `{lambda function url}`

    - Request schema:
        ```js
        code=123456&guid=47ff50fb-060b-4aac-8f9e-e0d6a37644f3&api_key=2B180436-3374-7979-FD60-B1520A609B16
        ```
    
    - Response schema:
        ```json
        {
            "passcode": {
                "guid": "xxx",
                "api_key": "xxx",
                "code": "xxx",
                "expire_duration": xxx,
                "create_time": xxx,
                "verified_time": xxx
            },
            "errorTip": "xxx"
        }
        ```
        If the it error occurs, the HTTP status code will be `400`, and there will be `errorTip` field inside response body.