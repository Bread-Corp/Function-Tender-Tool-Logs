# 📄 Super-User Log Retrieval API

[![AWS Lambda](https://img.shields.io/badge/AWS-Lambda-orange.svg)](https://aws.amazon.com/lambda/)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Amazon API Gateway](https://img.shields.io/badge/AWS-API%20Gateway-yellow.svg)](https://aws.amazon.com/api-gateway/)
[![Amazon S3](https://img.shields.io/badge/AWS-S3-green.svg)](https://aws.amazon.com/s3/)
[![Amazon CloudWatch](https://img.shields.io/badge/AWS-CloudWatch-blueviolet.svg)](https://aws.amazon.com/cloudwatch/)
[![Amazon RDS](https://img.shields.io/badge/AWS-RDS-informational.svg)](https://aws.amazon.com/rds/)

This project is a secure, on-demand logging facade for the tender-tool application. It provides an authenticated API endpoint (`/api/logs`) that front-end super-users can call to retrieve recent logs for any of the backend processing functions.

The Lambda authenticates the user against the RDS database, fetches the latest log events from CloudWatch, dynamically generates a styled HTML report (inspired by the application's dark-mode theme), uploads that report to a private S3 bucket, and returns a secure, 15-minute pre-signed URL for viewing.

## 📚 Table of Contents

- [✨ Key Features](#-key-features)
- [🧭 Architecture & Data Flow](#-architecture--data-flow)
- [🚀 API Specification (for Front-End)](#-api-specification-for-front-end)
- [🧩 Project Structure](#-project-structure)
- [⚙️ Configuration](#️-configuration)
- [🔒 IAM Permissions](#-iam-permissions)
- [📦 Tech Stack](#-tech-stack)
- [🚀 Getting Started](#-getting-started)
- [📦 Deployment Guide](#-deployment-guide)
- [🧰 Troubleshooting & Team Gotchas](#-troubleshooting--team-gotchas)

## ✨ Key Features

- **🛡️ Super-User Authentication**: Connects to the primary RDS database via EF Core to verify the user's `IsSuperUser` flag before processing any request.

- **📄 Modern HTML Reports**: Dynamically generates a "Tailwind-inspired" dark-mode HTML report from the log data. This avoids heavy dependencies like PDF libraries and provides a fast, clean, and readable output.

- **🎨 Smart Log Highlighting**: The generated HTML report automatically highlights log messages containing "error" (red) or "warning" (yellow), allowing for rapid visual scanning.

- **🔐 Secure, Temporary Access**: The generated report is uploaded to a private S3 bucket, and the API returns a secure, 15-minute pre-signed S3 URL for viewing. This ensures logs are never publicly accessible.

- **⚡ Fast & Reliable**: Fetches only the last 200 log events to ensure the request completes well under the 29-second API Gateway timeout.

- **🔗 VPC Native**: Runs inside the application's VPC to securely access the RDS database for authentication.

- **🌐 NAT Gateway Enabled**: Utilizes a NAT Gateway to securely access public AWS APIs (CloudWatch and S3) from its private subnets, resolving all networking timeouts.

## 🧭 Architecture & Data Flow

This function acts as a secure broker between the front-end user and various backend AWS services.

```
Front-End (Super-User)
   |
   ├─ 1. POST /api/logs
   │  (Payload: { "category": "scrapers", "functionName": "SarsLambda", "userId": "..." })
   ↓
API Gateway (https://h6nnlrf3lf...)
   ↓
Tender Tool Logs Lambda
   │
   ├─ 1. AuthService ───> Amazon RDS (VPC)
   │  (Checks if user is a super-user)
   │
   ├─ 2. LogMapperService
   │  (Maps "SarsLambda" to "/aws/lambda/SarsLambda")
   │
   ├─ 3. CloudWatchService ───> NAT Gateway ───> CloudWatch API
   │  (Fetches last 200 log events for /aws/lambda/SarsLambda)
   │
   ├─ 4. LogFormatterService
   │  (Builds dark-mode HTML string with log data)
   │
   ├─ 5. S3Service ───> NAT Gateway ───> S3 API (PutObject)
   │  (Uploads "report.html" to 'tender-tool-log-reports-super-user' bucket)
   │
   ├─ 6. S3Service ───> NAT Gateway ───> S3 API (GetObject)
   │  (Generates a 15-minute pre-signed URL)
   │
   └─ 7. Return 200 OK
      (Payload: { "fileName": "...", "downloadUrl": "https://..." })
   |
   ↓
Front-End (Super-User)
   │
   └─ Displays a clean link: <a href="[downloadUrl]">log-reports/SarsLambda-....html</a>
```

## 🚀 API Specification (for Front-End)

Here is the technical documentation for integrating the front-end with this API.

### 1. API Health Check (Root URL)

You can perform a simple `GET` request to the root URL of the API to confirm that it is deployed and running.

- **Method:** `GET`
- **Endpoint URL:** `https://h6nnlrf3lf.execute-api.us-east-1.amazonaws.com/Prod`
- **Expected Response:** A plain text string: `Welcome to the Tender Tool Logging Lambda`

### 2. Log Generation Endpoint & Request

This is the main endpoint for generating the log report. It is triggered by a `POST` request.

- **Method:** `POST`
- **Endpoint URL:** `https://h6nnlrf3lf.execute-api.us-east-1.amazonaws.com/Prod/api/logs`
- **Body (Request):** The body must be a JSON object with three properties: `category`, `functionName`, and `userId`.

**Example Request Body:**

```json
{
  "category": "pipeline",
  "functionName": "DeduplicationLambda",
  "userId": "B84EA17E-F718-43AC-84D4-7FC7155C6151"
}
```

### 3. Available Log Groups (Payload Options)

Here are the valid strings to send in the `category` and `functionName` fields. These are **case-insensitive**.

#### 🔍 Scraping

**Category:** `scrapers`

| Function Name (for `functionName`) |
|------------------------------------|
| `eTenderLambda` |
| `EskomLambda` |
| `TransnetLambda` |
| `SanralLambda` |
| `SarsLambda` |

#### 📦 Data Pipeline

**Category:** `pipeline`

| Function Name (for `functionName`) |
|------------------------------------|
| `DeduplicationLambda` |
| `AISummaryLambda` |
| `AITaggingLambda` |
| `DBWriterLambda` |
| `TenderCleanupLambda` |

### 4. Response Handling

The API will return standard HTTP status codes.

#### ✅ Success (200 OK)

This is returned when the log report is generated and uploaded successfully.

**Body (Response):**
```json
{
  "fileName": "log-reports/DBWriterLambda-20251029194802853.html",
  "downloadUrl": "https://...[a_very_long_url]..."
}
```

- **`fileName`**: A clean, human-readable name for the file. **This is what you should display to the user.**
- **`downloadUrl`**: The long, temporary, and secure S3 URL. **This is what the link's `href` should be.**

#### ❌ Error (4xx / 5xx)

All error responses will return a simple JSON object:

```json
{
  "message": "The reason for the error."
}
```

Here are the most common errors to handle:

- **`401 Unauthorized`**:
  - **Why:** The `userId` sent in the request was not found in the database or their `IsSuperUser` flag is `false`.
  - **Message:** `"User is not authorized to perform this action."`

- **`404 Not Found`**:
  - **Why:** The `category` or `functionName` sent did not match any of the known functions in the lists above.
  - **Message:** `"Log group mapping not found for 'scrapers' -> 'SarsLambda'"`

- **`500 Internal Server Error`**:
  - **Why:** A generic server-side failure. This could be a CloudWatch API error (`Rate exceeded`) or an S3 upload failure.
  - **Message:** `"An internal server error occurred. Please check the logs."`

### 5. How to Display the Log Report Link (Crucial)

To handle the long, ugly `downloadUrl`, you **must** use the `fileName` property as the visible part of the link. The `downloadUrl` is the link's destination.

**Do NOT display this:**
```
https://tender-tool-log-reports-super-user.s3.us-east-1.amazonaws.com/log-reports/DBWriter...
```

**Instead, do this:**

```html
<a [href]="response.downloadUrl" target="_blank">
  {{ response.fileName }}
</a>
```

This will correctly display a clean link like this to the user:
```
log-reports/DBWriterLambda-20251029194802853.html
```

When clicked, this link will open the styled HTML log report in a new browser tab.

## 🧩 Project Structure

```
Tender_Tool_Logs_Lambda/
├── Controllers/
│   └── LogsController.cs         # The main API endpoint, orchestrates all services
├── Data/
│   └── ApplicationDbContext.cs   # EF Core context for user authentication
├── Interfaces/
│   ├── IAuthService.cs
│   ├── ICloudWatchService.cs
│   ├── ILogFormatterService.cs   # Interface for the HTML report generator
│   ├── ILogMapperService.cs
│   └── IS3Service.cs
├── Models/
│   ├── LogRequest.cs             # API Request payload
│   ├── LogResponse.cs            # API Response payload
│   └── User/                     # User/SuperUser models for EF Core
├── Services/
│   ├── AuthService.cs            # Checks IsSuperUser flag in RDS
│   ├── CloudWatchService.cs      # Fetches log events from CloudWatch
│   ├── LogFormatterService.cs    # Builds the styled HTML report string
│   ├── LogMapperService.cs       # Maps friendly names to CloudWatch log group ARNs
│   └── S3Service.cs              # Uploads file to S3 and gets pre-signed URL
├── LatoFont/                     # Bundled fonts (Lato-Regular, Lato-Italic)
├── LambdaEntryPoint.cs           # Entry point for AWS Lambda
├── LocalEntryPoint.cs            # Entry point for local debugging
├── Startup.cs                    # Dependency Injection & JSON logging setup
├── appsettings.json              # Config for DB Connection & S3 Bucket
├── serverless.template           # CloudFormation blueprint (defines role, VPC, etc.)
└── README.md
```

## ⚙️ Configuration

The function is configured via `appsettings.json`.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=[YOUR_RDS_ENDPOINT];Database=[DB_NAME];User Id=[USER];Password=[PASS];"
  },
  "S3_BUCKET_NAME": "tender-tool-log-reports-super-user"
}
```

## 🔒 IAM Permissions

The Lambda's execution role (`TenderToolLogsLambdaRole`) requires the following permissions in its inline policy:

1. **CloudWatch Logs (Self):** To write its *own* logs.
2. **VPC:** To connect to the VPC for RDS access.
3. **CloudWatch (Target):** To read logs from *other* functions.
4. **S3:** To upload the report (`PutObject`) and generate the link (`GetObject`).

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "AllowCloudWatchLogs",
            "Effect": "Allow",
            "Action": [
                "logs:CreateLogGroup",
                "logs:CreateLogStream",
                "logs:PutLogEvents"
            ],
            "Resource": "arn:aws:logs:*:*:*"
        },
        {
            "Sid": "AllowVPCConnectionForRDS",
            "Effect": "Allow",
            "Action": [
                "ec2:CreateNetworkInterface",
                "ec2:DescribeNetworkInterfaces",
                "ec2:DeleteNetworkInterface"
            ],
            "Resource": "*"
        },
        {
            "Sid": "AllowReadTargetFunctionLogs",
            "Effect": "Allow",
            "Action": [
                "logs:DescribeLogStreams",
                "logs:GetLogEvents"
            ],
            "Resource": [
                "arn:aws:logs:us-east-1:211635102441:log-group:/aws/lambda/eTendersLambda:*",
                "arn:aws:logs:us-east-1:211635102441:log-group:/aws/lambda/EskomLambda:*",
                "arn:aws:logs:us-east-1:211635102441:log-group:/aws/lambda/TransnetLambda:*",
                "arn:aws:logs:us-east-1:211635102441:log-group:/aws/lambda/SanralFunction:*",
                "arn:aws:logs:us-east-1:211635102441:log-group:/aws/lambda/SarsLambda:*",
                "arn:aws:logs:us-east-1:211635102441:log-group:/aws/lambda/TenderDeduplicationLambda:*",
                "arn:aws:logs:us-east-1:211635102441:log-group:/aws/lambda/AILambda:*",
                "arn:aws:logs:us-east-1:211635102441:log-group:/aws/lambda/TenderAITaggingLambda:*",
                "arn:aws:logs:us-east-1:211635102441:log-group:/aws/lambda/TenderDatabaseWriterLambda:*",
                "arn:aws:logs:us-east-1:211635102441:log-group:/aws/lambda/TenderCleanupHandler:*"
            ]
        },
        {
            "Sid": "AllowS3AccessForLogReports",
            "Effect": "Allow",
            "Action": [
                "s3:PutObject",
                "s3:GetObject"
            ],
            "Resource": "arn:aws:s3:::tender-tool-log-reports-super-user/*"
        }
    ]
}
```

## 📦 Tech Stack

- **.NET 8** (LTS)
- **Compute**: AWS Lambda
- **API**: Amazon API Gateway
- **Storage**: Amazon S3 (for HTML reports)
- **Log Source**: Amazon CloudWatch
- **Database**: Amazon RDS (for user authentication)
- **Networking**: AWS VPC, NAT Gateway, Private Route Tables
- **Logging**: `Microsoft.Extensions.Logging.Console` (for structured JSON logging)

## 🚀 Getting Started

Follow these steps to set up the project for local development.

### Prerequisites

- .NET 8 SDK
- AWS CLI configured with appropriate credentials
- Visual Studio 2022 or VS Code with C# extensions

### Local Setup

1. **Clone the repository:**
   ```bash
   git clone <your-repository-url>
   cd Tender_Tool_Logs_Lambda
   ```

2. **Restore Dependencies:**
   ```bash
   dotnet restore
   ```

3. **Configure Application Settings:**
   Update `appsettings.json` with your local configuration:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=your-local-db;Database=TenderTool;..."
     },
     "S3_BUCKET_NAME": "your-test-bucket"
   }
   ```

4. **Run Locally:**
   ```bash
   dotnet run
   ```

## 📦 Deployment

This ASP.NET Core Lambda function can be deployed using three different methods. Choose the one that best fits your workflow and requirements.

### Prerequisites

Before deploying, ensure you have:

- .NET 8 SDK installed
- AWS CLI configured with appropriate credentials
- SQL Server RDS instance running and accessible from VPC
- Private S3 bucket for log reports (`tender-tool-log-reports-super-user`)
- VPC configured with NAT Gateway and appropriate subnets/security groups
- IAM role `TenderToolLogsLambdaRole` with required permissions

---

### Method 1: AWS Toolkit Deployment

Deploy directly from your IDE using the AWS Toolkit extension.

#### For Visual Studio 2022:

1. **Install AWS Toolkit:**
   - Install the AWS Toolkit for Visual Studio from the Visual Studio Marketplace

2. **Configure AWS Credentials:**
   - Ensure your AWS credentials are configured in Visual Studio
   - Go to View → AWS Explorer and configure your profile

3. **Deploy the Function:**
   - Right-click on the `Tender_Tool_Logs_Lambda.csproj` project
   - Select "Publish to AWS Lambda..."
   - Choose "ASP.NET Core Web API" as the function blueprint
   - Configure the deployment settings:
     - **Function Name**: `TenderToolLogsLambda`
     - **Runtime**: `.NET 8`
     - **Memory**: `512 MB`
     - **Timeout**: `240 seconds` (4 minutes)
     - **Handler**: `Tender_Tool_Logs_Lambda::Tender_Tool_Logs_Lambda.LambdaEntryPoint::FunctionHandlerAsync`
     - **IAM Role**: `arn:aws:iam::211635102441:role/TenderToolLogsLambdaRole`

4. **Configure VPC Settings:**
   - **VPC**: Select your VPC
   - **Subnets**: `subnet-0f47b68400d516b1e`, `subnet-072a27234084339fc` (private subnets)
   - **Security Groups**: `sg-0dc0af4fcf50676e9`

5. **Configure API Gateway:**
   - The function will automatically create an API Gateway with `/{proxy+}` and `/` routes
   - Note the generated API Gateway URL for testing

#### For VS Code:

1. **Install AWS Toolkit:**
   - Install the AWS Toolkit extension for VS Code

2. **Open Command Palette:**
   - Press `Ctrl+Shift+P` (Windows/Linux) or `Cmd+Shift+P` (Mac)
   - Type "AWS: Deploy SAM Application"

3. **Follow the deployment wizard** to configure and deploy your function

---

### Method 2: SAM Deployment

Deploy using AWS SAM CLI with the provided serverless template.

#### Step 1: Install SAM CLI

```bash
# For Windows (using Chocolatey)
choco install aws-sam-cli

# For macOS (using Homebrew)
brew install aws-sam-cli

# For Linux (using pip)
pip install aws-sam-cli
```

#### Step 2: Install Lambda Tools

```bash
dotnet tool install -g Amazon.Lambda.Tools
```

#### Step 3: Configure Application Settings

Ensure your `appsettings.json` has the correct configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-rds-endpoint,1433;Database=tendertool_db;User Id=admin;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True"
  },
  "S3_BUCKET_NAME": "tender-tool-log-reports-super-user"
}
```

#### Step 4: Build and Deploy

```bash
# Build the project
dotnet restore
dotnet build -c Release

# Package the Lambda function (ASP.NET Core style)
dotnet lambda package -c Release -o ./lambda-package.zip Tender_Tool_Logs_Lambda.csproj

# Deploy using SAM with guided setup
sam deploy --template-file serverless.template \
           --stack-name tender-tool-log-api-stack \
           --capabilities CAPABILITY_IAM \
           --guided
```

#### Alternative: Direct SAM Deploy

For subsequent deployments after initial setup:

```bash
sam deploy --template-file serverless.template \
           --stack-name tender-tool-log-api-stack \
           --capabilities CAPABILITY_IAM \
           --parameter-overrides \
             DatabaseConnectionString="Server=your-rds-endpoint,1433;Database=tendertool_db;User Id=admin;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True" \
             S3BucketName="tender-tool-log-reports-super-user"
```

#### Important: Pre-existing IAM Role

The serverless template references a pre-existing IAM role:
```
"Role": "arn:aws:iam::211635102441:role/TenderToolLogsLambdaRole"
```

Ensure this role exists with the required permissions before deployment. If the role doesn't exist, create it with the following permissions:

- CloudWatch Logs access (self)
- VPC access permissions
- CloudWatch Logs read access (target functions)
- S3 bucket access for log reports

---

### Method 3: Workflow Deployment (GitHub Actions)

Deploy automatically using GitHub Actions when pushing to the release branch.

#### Step 1: Set Up Repository Secrets

In your GitHub repository, go to Settings → Secrets and variables → Actions, and add:

```
AWS_ACCESS_KEY_ID: your-aws-access-key-id
AWS_SECRET_ACCESS_KEY: your-aws-secret-access-key
AWS_REGION: us-east-1
```

#### Step 2: Deploy via Release Branch

```bash
# Create and switch to release branch
git checkout -b release

# Make your changes and commit
git add .
git commit -m "Deploy Tender Tool Logs Lambda updates"

# Push to trigger deployment
git push origin release
```

#### Step 3: Monitor Deployment

1. Go to your repository's **Actions** tab
2. Monitor the "Deploy .NET Lambda to AWS" workflow
3. Check the deployment logs for any issues

#### Manual Trigger

You can also trigger the deployment manually:

1. Go to the **Actions** tab in your repository
2. Select "Deploy .NET Lambda to AWS"
3. Click "Run workflow"
4. Select the branch and click "Run workflow"

---

### Post-Deployment Verification

After deploying using any method, verify the deployment:

#### 1. Check Lambda Function

```bash
# Verify function exists and configuration
aws lambda get-function --function-name tender-tool-log-api-stack-AspNetCoreFunction-fLHMagDT2qjX

# Check environment variables and VPC configuration
aws lambda get-function-configuration --function-name tender-tool-log-api-stack-AspNetCoreFunction-fLHMagDT2qjX
```

#### 2. Test API Gateway Endpoint

```bash
# Get the API Gateway URL from CloudFormation outputs
aws cloudformation describe-stacks --stack-name tender-tool-log-api-stack --query 'Stacks[0].Outputs'

# Test the health check endpoint
curl -X GET https://your-api-gateway-url.execute-api.us-east-1.amazonaws.com/Prod/

# Test the logs endpoint (requires valid super-user credentials)
curl -X POST https://your-api-gateway-url.execute-api.us-east-1.amazonaws.com/Prod/api/logs \
  -H "Content-Type: application/json" \
  -d '{
    "category": "scrapers",
    "functionName": "SarsLambda",
    "userId": "valid-super-user-id"
  }'
```

#### 3. Verify Database and S3 Connectivity

```bash
# Check CloudWatch logs for any connection issues
aws logs describe-log-groups --log-group-name-prefix "/aws/lambda/tender-tool-log-api-stack"

# View recent logs
aws logs tail "/aws/lambda/tender-tool-log-api-stack-AspNetCoreFunction" --follow

# Verify S3 bucket exists
aws s3 ls s3://tender-tool-log-reports-super-user/
```

---

### Environment Variables Setup

The function uses `appsettings.json` for configuration. Ensure this file contains:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-rds-endpoint,1433;Database=tendertool_db;User Id=admin;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True"
  },
  "S3_BUCKET_NAME": "tender-tool-log-reports-super-user"
}
```

> **Security Note**: For production deployments, store the database connection string in AWS Secrets Manager and reference it in the Lambda function instead of using plain text.

---

### Critical VPC and Networking Configuration

This Lambda function requires specific VPC configuration to access RDS, CloudWatch, and S3:

#### VPC Requirements:
- **Subnets**: Must be in private subnets: `subnet-0f47b68400d516b1e`, `subnet-072a27234084339fc`
- **NAT Gateway**: Required for accessing public AWS APIs (CloudWatch, S3)
- **Security Groups**: Must allow outbound traffic to RDS and internet

#### Security Group Configuration:

**For Lambda Security Group (sg-0dc0af4fcf50676e9):**
```
Outbound Rules:
- Type: MS SQL, Port: 1433, Destination: RDS Security Group
- Type: HTTPS, Port: 443, Destination: 0.0.0.0/0 (for AWS APIs via NAT)
- Type: All Traffic, Port: All, Destination: 0.0.0.0/0 (for API Gateway integration)
```

**For RDS Security Group:**
```
Inbound Rules:
- Type: MS SQL, Port: 1433, Source: Lambda Security Group (sg-0dc0af4fcf50676e9)
```

#### Route Table Configuration:

**Private Route Table (for Lambda subnets):**
```
Routes:
- Destination: 10.0.0.0/16, Target: Local (VPC CIDR)
- Destination: 0.0.0.0/0, Target: NAT Gateway ID
```

---

### IAM Role Configuration

The Lambda uses a pre-existing IAM role. Ensure the `TenderToolLogsLambdaRole` has the following permissions:

#### Required Policies:

1. **CloudWatch Logs (Self):**
```json
{
  "Effect": "Allow",
  "Action": [
    "logs:CreateLogGroup",
    "logs:CreateLogStream",
    "logs:PutLogEvents"
  ],
  "Resource": "arn:aws:logs:*:*:*"
}
```

2. **VPC Access:**
```json
{
  "Effect": "Allow",
  "Action": [
    "ec2:CreateNetworkInterface",
    "ec2:DescribeNetworkInterfaces",
    "ec2:DeleteNetworkInterface"
  ],
  "Resource": "*"
}
```

3. **CloudWatch Logs (Target Functions):**
```json
{
  "Effect": "Allow",
  "Action": [
    "logs:DescribeLogStreams",
    "logs:GetLogEvents"
  ],
  "Resource": [
    "arn:aws:logs:us-east-1:211635102441:log-group:/aws/lambda/*"
  ]
}
```

4. **S3 Access:**
```json
{
  "Effect": "Allow",
  "Action": [
    "s3:PutObject",
    "s3:GetObject"
  ],
  "Resource": "arn:aws:s3:::tender-tool-log-reports-super-user/*"
}
```

---

### API Testing and Usage

After successful deployment, test the API endpoints:

#### Health Check:
```bash
curl -X GET https://your-api-gateway-url.execute-api.us-east-1.amazonaws.com/Prod/
# Expected: "Welcome to the Tender Tool Logging Lambda"
```

#### Log Report Generation:
```bash
curl -X POST https://your-api-gateway-url.execute-api.us-east-1.amazonaws.com/Prod/api/logs \
  -H "Content-Type: application/json" \
  -d '{
    "category": "scrapers",
    "functionName": "SarsLambda",
    "userId": "B84EA17E-F718-43AC-84D4-7FC7155C6151"
  }'
```

#### Expected Response:
```json
{
  "fileName": "log-reports/SarsLambda-20251104172407.html",
  "downloadUrl": "https://tender-tool-log-reports-super-user.s3.us-east-1.amazonaws.com/..."
}
```

---

### Troubleshooting Deployment Issues

**Database Connection Errors:**
- Verify Lambda is in the same VPC as RDS instance
- Check security group rules allow Lambda to reach RDS on port 1433
- Verify connection string format and credentials
- Ensure RDS instance is running and accessible

**CloudWatch API Rate Limiting:**
- Function is configured to fetch only 200 most recent log events
- If rate limiting persists, check for multiple concurrent requests
- Review CloudWatch service quotas in AWS console

**S3 Upload Failures:**
- Verify S3 bucket `tender-tool-log-reports-super-user` exists
- Check IAM permissions for S3 PutObject and GetObject
- Ensure bucket is in the same region as Lambda

**NAT Gateway Issues:**
- Verify NAT Gateway is in a public subnet
- Check route table configuration for private subnets
- Ensure NAT Gateway has an Elastic IP assigned

**API Gateway Timeouts:**
- Function timeout is set to 240 seconds (4 minutes)
- API Gateway has a hard limit of 29 seconds for HTTP APIs
- Monitor CloudWatch logs for execution duration

**Authentication Errors:**
- Verify user exists in RDS database with `IsSuperUser = true`
- Check database connection from Lambda
- Ensure correct userId format in API requests

## 🧰 Troubleshooting & Team Gotchas

<details>
<summary><strong>ERROR: 500 - `AmazonCloudWatchLogsException: Rate exceeded`</strong></summary>

**Issue**: The most common error. The `GetLogEvents` API has a very low quota (5 TPS). The default AWS SDK retry logic, combined with a loop, can easily hit this limit.

**Fix**: The `CloudWatchService` was simplified to remove all loops and make only **one** call to `GetLogEventsAsync` for a maximum of 200 events. This stays well under the quota.

</details>

<details>
<summary><strong>ERROR: 504 - `Endpoint request timed out` (API Gateway)</strong></summary>

**Issue**: The API call from Postman timed out after 29-30 seconds.

**Reason**: This was caused by the Lambda trying to download the *entire* log stream (thousands of events), which is a slow network operation that exceeded the 29-second hard limit of API Gateway.

**Fix**: We limited the `CloudWatchService` to fetch only the `Limit = 200` most recent logs, ensuring the function is always fast.

</details>

<details>
<summary><strong>ERROR: 502/500 - `Internal Server Error` (Networking Timeout)</strong></summary>

**Issue**: The function authenticated against RDS (proving VPC worked) but then hung when calling CloudWatch or S3.

**Reason**: The Lambda was in a private subnet (to reach RDS) and had no route to the public internet (to reach CloudWatch/S3 APIs).

**Fix**: We created a **NAT Gateway** in a public subnet and a **Private Route Table** for the Lambda's subnets that routes all `0.0.0.0/0` traffic to the NAT Gateway. This provides secure, one-way internet access.

</details>

<details>
<summary><strong>Pivoted from QuestPDF to HTML</strong></summary>

**Issue**: Initial attempts used the `QuestPDF` library. This caused numerous, hard-to-debug crashes (`500 Internal Server Error`) on Lambda.

**Reason**: QuestPDF relies on native libraries (`SkiaSharp`) and system fonts, which are not included in the standard .NET 8 Lambda runtime. This led to file-not-found errors, font-loading errors, and other native crashes.

**Fix**: We **removed all QuestPDF/SkiaSharp dependencies** and replaced the `PdfService` with a lightweight, zero-dependency `LogFormatterService` that builds an HTML string. This is faster, more reliable, and achieved a better-looking result.

</details>

---

> Built with love, bread, and code by **Bread Corporation** 🦆❤️💻
