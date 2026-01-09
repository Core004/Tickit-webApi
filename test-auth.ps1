$baseUrl = "http://localhost:5176/api"

# Try to register
$regBody = @{ email = "test@example.com"; password = "Test123!"; firstName = "Test"; lastName = "User" } | ConvertTo-Json
try {
    $regResult = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json"
    Write-Host "Register: PASSED"
} catch {
    Write-Host "Register: $($_.Exception.Message)"
}

# Now try to login
$loginBody = @{ email = "test@example.com"; password = "Test123!" } | ConvertTo-Json
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    Write-Host "Login: PASSED - Token received"
    $loginResponse.token | Out-File -FilePath "C:\Projects\Tickit-webApi\token.txt"
    Write-Host "Token saved to token.txt"
} catch {
    Write-Host "Login: $($_.Exception.Message)"
}
