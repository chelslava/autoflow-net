# AutoFlow.NET Docker Image
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files
COPY AutoFlow.sln .
COPY src/AutoFlow.Abstractions/AutoFlow.Abstractions.csproj ./src/AutoFlow.Abstractions/
COPY src/AutoFlow.Parser/AutoFlow.Parser.csproj ./src/AutoFlow.Parser/
COPY src/AutoFlow.Runtime/AutoFlow.Runtime.csproj ./src/AutoFlow.Runtime/
COPY src/AutoFlow.Validation/AutoFlow.Validation.csproj ./src/AutoFlow.Validation/
COPY src/AutoFlow.Reporting/AutoFlow.Reporting.csproj ./src/AutoFlow.Reporting/
COPY src/AutoFlow.Cli/AutoFlow.Cli.csproj ./src/AutoFlow.Cli/
COPY libraries/AutoFlow.Library.Assertions/AutoFlow.Library.Assertions.csproj ./libraries/AutoFlow.Library.Assertions/
COPY libraries/AutoFlow.Library.Files/AutoFlow.Library.Files.csproj ./libraries/AutoFlow.Library.Files/
COPY libraries/AutoFlow.Library.Http/AutoFlow.Library.Http.csproj ./libraries/AutoFlow.Library.Http/
COPY libraries/AutoFlow.Library.Browser/AutoFlow.Library.Browser.csproj ./libraries/AutoFlow.Library.Browser/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ ./src/
COPY libraries/ ./libraries/

# Build and publish
RUN dotnet publish src/AutoFlow.Cli/AutoFlow.Cli.csproj -c Release -o /app/publish \
    --no-restore \
    /p:PublishSingleFile=true \
    /p:SelfContained=true \
    /p:RuntimeIdentifier=linux-x64 \
    /p:PublishTrimmed=true

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-preview AS runtime
WORKDIR /app

# Install Playwright dependencies for browser automation
RUN apt-get update && apt-get install -y \
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    libpango-1.0-0 \
    libcairo2 \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Create directories for workflows and reports
RUN mkdir -p /workflows /reports

# Set environment variables
ENV AUTOFLOW_WORKFLOWS_DIR=/workflows
ENV AUTOFLOW_REPORTS_DIR=/reports

# Entry point
ENTRYPOINT ["./autoflow"]
CMD ["--help"]
