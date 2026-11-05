# ==========================================
# Build Stage (.NET 9 SDK)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project definition files for layer caching
COPY ["StackTrigger.sln", "./"]
COPY ["src/StackTrigger.Domain/StackTrigger.Domain.csproj", "src/StackTrigger.Domain/"]
COPY ["src/StackTrigger.Application/StackTrigger.Application.csproj", "src/StackTrigger.Application/"]
COPY ["src/StackTrigger.Infrastructure/StackTrigger.Infrastructure.csproj", "src/StackTrigger.Infrastructure/"]
COPY ["src/StackTrigger.Worker/StackTrigger.Worker.csproj", "src/StackTrigger.Worker/"]

# Restore packages with cached layers
RUN dotnet restore "src/StackTrigger.Worker/StackTrigger.Worker.csproj"

# Copy full source tree
COPY . .

# Publish optimized self-contained or trimmed executable (Release)
WORKDIR "/src/src/StackTrigger.Worker"
RUN dotnet publish "StackTrigger.Worker.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ==========================================
# Runtime Stage (.NET 9 Runtime - Non-Root)
# ==========================================
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS final
WORKDIR /app

# Harden container with a non-privileged system user (UID 10001)
RUN addgroup -g 10001 -S appgroup && \
    adduser -u 10001 -S appuser -G appgroup

# Copy published artifacts from build stage
COPY --from=build --chown=appuser:appgroup /app/publish .

# Set secure execution privileges
USER appuser:appgroup

# Configure production environment variables
ENV DOTNET_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

ENTRYPOINT ["dotnet", "StackTrigger.Worker.dll"]

