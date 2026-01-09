$baseUrl = "http://localhost:5176/api"

# Login
$loginBody = @{ email = "test@example.com"; password = "Test123!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
Write-Host "Token length: $($token.Length)"
Write-Host "Token prefix: $($token.Substring(0, 50))..."

$headers = @{ Authorization = "Bearer $token" }
Write-Host "Headers: $($headers | ConvertTo-Json)"

# Test existing endpoint that works
Write-Host "`n=== Testing existing endpoints ==="
try {
    $statuses = Invoke-RestMethod -Uri "$baseUrl/statuses" -Headers $headers
    Write-Host "GET /statuses: PASSED - $($statuses.Count) items"
} catch {
    Write-Host "GET /statuses: FAILED - $($_.Exception.Message)"
}

# Test new SLA endpoint
Write-Host "`n=== Testing new SLA endpoint ==="
try {
    $rules = Invoke-RestMethod -Uri "$baseUrl/sla/rules" -Headers $headers
    Write-Host "GET /sla/rules: PASSED - $($rules.Count) items"
} catch {
    Write-Host "GET /sla/rules: FAILED - $($_.Exception.Message)"
    Write-Host "Response: $($_.Exception.Response.StatusCode)"
}

# Test knowledge-base endpoint
Write-Host "`n=== Testing Knowledge Base endpoint ==="
try {
    $articles = Invoke-RestMethod -Uri "$baseUrl/knowledge-base" -Headers $headers
    Write-Host "GET /knowledge-base: PASSED"
} catch {
    Write-Host "GET /knowledge-base: FAILED - $($_.Exception.Message)"
}

# Test with different URL format
try {
    $articles = Invoke-RestMethod -Uri "$baseUrl/knowledgebase" -Headers $headers
    Write-Host "GET /knowledgebase: PASSED"
} catch {
    Write-Host "GET /knowledgebase: FAILED - $($_.Exception.Message)"
}
