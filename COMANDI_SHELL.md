# --- Api project packages ---
dotnet add Api/Api.csproj package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add Api/Api.csproj package FluentValidation.AspNetCore
dotnet add Api/Api.csproj package AWSSDK.S3
dotnet add Api/Api.csproj package Swashbuckle.AspNetCore
dotnet add Api/Api.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Api/Api.csproj package Microsoft.EntityFrameworkCore.Tools
dotnet add Api/Api.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add Api/Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer

# --- Unit tests project (create if missing) ---
# (se non hai ancora creato il progetto di test)
# dotnet new xunit -o tests/Api.UnitTests
dotnet add tests/Api.UnitTests/Api.UnitTests.csproj package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/Api.UnitTests/Api.UnitTests.csproj package xunit
dotnet add tests/Api.UnitTests/Api.UnitTests.csproj package xunit.runner.visualstudio
dotnet add tests/Api.UnitTests/Api.UnitTests.csproj package Moq
dotnet add tests/Api.UnitTests/Api.UnitTests.csproj package Microsoft.AspNetCore.Http.Abstractions

# --- Integration tests project (create if missing) ---
# (se non hai ancora creato il progetto di integrazione)
# dotnet new xunit -o tests/Api.IntegrationTests
dotnet add tests/Api.IntegrationTests/Api.IntegrationTests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/Api.IntegrationTests/Api.IntegrationTests.csproj package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/Api.IntegrationTests/Api.IntegrationTests.csproj package xunit
dotnet add tests/Api.IntegrationTests/Api.IntegrationTests.csproj package xunit.runner.visualstudio


# Comandi utili

Aggiungere i package NuGet per Api:
dotnet add Api/Api.csproj package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add Api/Api.csproj package FluentValidation.AspNetCore
dotnet add Api/Api.csproj package AWSSDK.S3
dotnet add Api/Api.csproj package Swashbuckle.AspNetCore
dotnet add Api/Api.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Api/Api.csproj package Microsoft.EntityFrameworkCore.Tools
dotnet add Api/Api.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add Api/Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer

Creare i progetti test (se non presenti) e aggiungere le dipendenze:
dotnet new xunit -o tests/Api.UnitTests
dotnet new xunit -o tests/Api.IntegrationTests

dotnet add tests/Api.UnitTests/Api.UnitTests.csproj package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/Api.UnitTests/Api.UnitTests.csproj package Moq
dotnet add tests/Api.UnitTests/Api.UnitTests.csproj package Microsoft.AspNetCore.Http.Abstractions

dotnet add tests/Api.IntegrationTests/Api.IntegrationTests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/Api.IntegrationTests/Api.IntegrationTests.csproj package Microsoft.EntityFrameworkCore.InMemory

Eseguire tutti i test:
dotnet test