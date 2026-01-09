# ============================================================================
# Phase 1.1: Create Solution Structure
# Run this in PowerShell from C:\Projects\Tickit-webApi
# ============================================================================

Write-Host "Creating TicketSystem Solution..." -ForegroundColor Green

# Create solution
dotnet new sln -n TicketSystem

# Create projects
Write-Host "Creating Domain project..." -ForegroundColor Yellow
dotnet new classlib -n TicketSystem.Domain -o src/TicketSystem.Domain

Write-Host "Creating Application project..." -ForegroundColor Yellow
dotnet new classlib -n TicketSystem.Application -o src/TicketSystem.Application

Write-Host "Creating Infrastructure project..." -ForegroundColor Yellow
dotnet new classlib -n TicketSystem.Infrastructure -o src/TicketSystem.Infrastructure

Write-Host "Creating API project..." -ForegroundColor Yellow
dotnet new webapi -n TicketSystem.API -o src/TicketSystem.API

# Add projects to solution
Write-Host "Adding projects to solution..." -ForegroundColor Yellow
dotnet sln add src/TicketSystem.Domain
dotnet sln add src/TicketSystem.Application
dotnet sln add src/TicketSystem.Infrastructure
dotnet sln add src/TicketSystem.API

# Add project references
Write-Host "Adding project references..." -ForegroundColor Yellow

# Application references Domain
dotnet add src/TicketSystem.Application reference src/TicketSystem.Domain

# Infrastructure references Application and Domain
dotnet add src/TicketSystem.Infrastructure reference src/TicketSystem.Application
dotnet add src/TicketSystem.Infrastructure reference src/TicketSystem.Domain

# API references Infrastructure and Application
dotnet add src/TicketSystem.API reference src/TicketSystem.Infrastructure
dotnet add src/TicketSystem.API reference src/TicketSystem.Application

Write-Host "`nSolution structure created successfully!" -ForegroundColor Green
Write-Host "Run 'dotnet build' to verify the setup." -ForegroundColor Cyan
