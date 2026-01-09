$baseUrl = "http://localhost:5176/api"

# Login
$loginBody = @{ email = "test@example.com"; password = "Test123!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"

Write-Host "Login Response:"
$loginResponse | ConvertTo-Json -Depth 10

# Get the token from the correct property
$token = $loginResponse.accessToken
if (-not $token) {
    $token = $loginResponse.Token
}
if (-not $token) {
    $token = $loginResponse.token
}
if (-not $token) {
    Write-Host "Token not found in any expected property"
    Write-Host "Available properties: $($loginResponse | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name)"
    exit
}

Write-Host "`nToken length: $($token.Length)"
$headers = @{ Authorization = "Bearer $token" }

# Test existing endpoint
Write-Host "`n=== Testing authenticated endpoints ==="
try {
    $statuses = Invoke-RestMethod -Uri "$baseUrl/statuses" -Headers $headers
    Write-Host "GET /statuses: PASSED - $($statuses.Count) items"
} catch {
    Write-Host "GET /statuses: FAILED - $($_.Exception.Message)"
}

# Test SLA endpoint
try {
    $rules = Invoke-RestMethod -Uri "$baseUrl/sla/rules" -Headers $headers
    Write-Host "GET /sla/rules: PASSED - $($rules.Count) items"
} catch {
    Write-Host "GET /sla/rules: FAILED - $($_.Exception.Message)"
}

# Test with different route formats
try {
    $articles = Invoke-RestMethod -Uri "$baseUrl/knowledgebase" -Headers $headers
    Write-Host "GET /knowledgebase: PASSED"
} catch {
    Write-Host "GET /knowledgebase: FAILED - $($_.Exception.Message)"
}

try {
    $articles = Invoke-RestMethod -Uri "$baseUrl/KnowledgeBase" -Headers $headers
    Write-Host "GET /KnowledgeBase: PASSED"
} catch {
    Write-Host "GET /KnowledgeBase: FAILED - $($_.Exception.Message)"
}
