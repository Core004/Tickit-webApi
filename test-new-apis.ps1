$baseUrl = "http://localhost:5050/api"

# First, test password reset APIs (don't require auth)
Write-Host "=== PASSWORD RESET APIs (No Auth Required) ===" -ForegroundColor Cyan

$passed = 0
$failed = 0

# Password reset request (public endpoint)
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/auth/password-reset-request" -Method Post -Body (@{ email = "test@example.com" } | ConvertTo-Json) -ContentType "application/json"
    Write-Host "POST /auth/password-reset-request: PASSED" -ForegroundColor Green
    $passed++
} catch {
    Write-Host "POST /auth/password-reset-request: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# Password reset (public endpoint - will fail with invalid token but endpoint works)
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/auth/password-reset" -Method Post -Body (@{ email = "test@example.com"; token = "invalid-token"; newPassword = "NewPass123!" } | ConvertTo-Json) -ContentType "application/json" -ErrorAction Stop
    Write-Host "POST /auth/password-reset: PASSED" -ForegroundColor Green
    $passed++
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 400) {
        # 400 is expected for invalid token - endpoint is working
        Write-Host "POST /auth/password-reset: PASSED (400 - Invalid token expected)" -ForegroundColor Green
        $passed++
    } else {
        Write-Host "POST /auth/password-reset: FAILED - $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }
}

# Now login to get token
Write-Host "`n=== AUTH ===" -ForegroundColor Cyan
$loginBody = @{ email = "admin@gmail.com"; password = "Admin@123" } | ConvertTo-Json
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.accessToken
    Write-Host "Login: PASSED" -ForegroundColor Green
    $passed++
} catch {
    Write-Host "Login: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    $failed++
    exit
}

$headers = @{ Authorization = "Bearer $token" }

# Test change password (requires auth) - change to new password then back
try {
    # First change to a new password
    $response = Invoke-RestMethod -Uri "$baseUrl/auth/change-password" -Method Post -Body (@{ currentPassword = "Admin@123"; newPassword = "Admin@456!" } | ConvertTo-Json) -Headers $headers -ContentType "application/json"
    Write-Host "POST /auth/change-password (to new): PASSED" -ForegroundColor Green
    $passed++

    # Change it back to original
    $response = Invoke-RestMethod -Uri "$baseUrl/auth/change-password" -Method Post -Body (@{ currentPassword = "Admin@456!"; newPassword = "Admin@123" } | ConvertTo-Json) -Headers $headers -ContentType "application/json"
    Write-Host "POST /auth/change-password (back to original): PASSED" -ForegroundColor Green
    $passed++
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "POST /auth/change-password: FAILED - Status: $statusCode - $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

function Test-Api {
    param($Method, $Endpoint, $Body = $null, $Description)
    try {
        if ($Body) {
            $result = Invoke-RestMethod -Uri "$baseUrl$Endpoint" -Method $Method -Body ($Body | ConvertTo-Json) -Headers $headers -ContentType "application/json"
        } else {
            $result = Invoke-RestMethod -Uri "$baseUrl$Endpoint" -Method $Method -Headers $headers
        }
        Write-Host "$Description : PASSED" -ForegroundColor Green
        $script:passed++
        return $result
    } catch {
        Write-Host "$Description : FAILED - $($_.Exception.Message)" -ForegroundColor Red
        $script:failed++
        return $null
    }
}

# Test SLA Controller
Write-Host "`n=== SLA CONTROLLER ===" -ForegroundColor Cyan

# SLA Rules
Test-Api -Method "Get" -Endpoint "/sla/rules" -Description "GET /sla/rules"

$slaRuleId = Test-Api -Method "Post" -Endpoint "/sla/rules" -Body @{ name = "Test SLA Rule"; description = "Test"; responseTimeMinutes = 60; resolutionTimeMinutes = 240; businessHoursOnly = $true } -Description "POST /sla/rules"

if ($slaRuleId) {
    Test-Api -Method "Get" -Endpoint "/sla/rules/$slaRuleId" -Description "GET /sla/rules/{id}"
    Test-Api -Method "Put" -Endpoint "/sla/rules/$slaRuleId" -Body @{ name = "Updated SLA"; description = "Updated"; responseTimeMinutes = 30; resolutionTimeMinutes = 120; businessHoursOnly = $false } -Description "PUT /sla/rules/{id}"
    Test-Api -Method "Post" -Endpoint "/sla/rules/$slaRuleId/deactivate" -Description "POST /sla/rules/{id}/deactivate"
    Test-Api -Method "Post" -Endpoint "/sla/rules/$slaRuleId/activate" -Description "POST /sla/rules/{id}/activate"
}

# Business Hours
Test-Api -Method "Get" -Endpoint "/sla/business-hours" -Description "GET /sla/business-hours"

$bhId = Test-Api -Method "Post" -Endpoint "/sla/business-hours" -Body @{ name = "Monday Hours"; dayOfWeek = 1; startTime = "09:00:00"; endTime = "17:00:00"; timeZone = "UTC" } -Description "POST /sla/business-hours"

if ($bhId) {
    Test-Api -Method "Get" -Endpoint "/sla/business-hours/$bhId" -Description "GET /sla/business-hours/{id}"
    Test-Api -Method "Put" -Endpoint "/sla/business-hours/$bhId" -Body @{ name = "Updated Hours"; dayOfWeek = 1; startTime = "08:00:00"; endTime = "18:00:00"; timeZone = "EST" } -Description "PUT /sla/business-hours/{id}"
}

# Holidays
Test-Api -Method "Get" -Endpoint "/sla/holidays" -Description "GET /sla/holidays"

$holidayId = Test-Api -Method "Post" -Endpoint "/sla/holidays" -Body @{ name = "Test Holiday"; date = "2026-12-25"; isRecurring = $true } -Description "POST /sla/holidays"

if ($holidayId) {
    Test-Api -Method "Get" -Endpoint "/sla/holidays/$holidayId" -Description "GET /sla/holidays/{id}"
    Test-Api -Method "Put" -Endpoint "/sla/holidays/$holidayId" -Body @{ name = "Updated Holiday"; date = "2026-12-26"; isRecurring = $false } -Description "PUT /sla/holidays/{id}"
}

# Escalation Rules
Test-Api -Method "Get" -Endpoint "/sla/escalations" -Description "GET /sla/escalations"

$escId = Test-Api -Method "Post" -Endpoint "/sla/escalations" -Body @{ name = "Test Escalation"; slaRuleId = $slaRuleId; triggerMinutes = 30; action = 1 } -Description "POST /sla/escalations"

if ($escId) {
    Test-Api -Method "Get" -Endpoint "/sla/escalations/$escId" -Description "GET /sla/escalations/{id}"
    Test-Api -Method "Put" -Endpoint "/sla/escalations/$escId" -Body @{ name = "Updated Escalation"; slaRuleId = $slaRuleId; triggerMinutes = 45; action = 2 } -Description "PUT /sla/escalations/{id}"
}

# Test Knowledge Base Controller
Write-Host "`n=== KNOWLEDGE BASE CONTROLLER ===" -ForegroundColor Cyan

Test-Api -Method "Get" -Endpoint "/knowledgebase" -Description "GET /knowledgebase"

$articleId = Test-Api -Method "Post" -Endpoint "/knowledgebase" -Body @{ title = "Test Article"; content = "This is test content"; metaDescription = "Test meta"; isFeatured = $false } -Description "POST /knowledgebase"

if ($articleId) {
    Test-Api -Method "Get" -Endpoint "/knowledgebase/$articleId" -Description "GET /knowledgebase/{id}"
    Test-Api -Method "Put" -Endpoint "/knowledgebase/$articleId" -Body @{ title = "Updated Article"; content = "Updated content"; metaDescription = "Updated meta"; isFeatured = $true } -Description "PUT /knowledgebase/{id}"
    Test-Api -Method "Post" -Endpoint "/knowledgebase/$articleId/publish" -Description "POST /knowledgebase/{id}/publish"
    Test-Api -Method "Post" -Endpoint "/knowledgebase/$articleId/unpublish" -Description "POST /knowledgebase/{id}/unpublish"
}

# Tags
Test-Api -Method "Get" -Endpoint "/knowledgebase/tags" -Description "GET /knowledgebase/tags"

$tagId = Test-Api -Method "Post" -Endpoint "/knowledgebase/tags" -Body @{ name = "Test Tag" } -Description "POST /knowledgebase/tags"

if ($tagId) {
    Test-Api -Method "Get" -Endpoint "/knowledgebase/tags/$tagId" -Description "GET /knowledgebase/tags/{id}"
    Test-Api -Method "Put" -Endpoint "/knowledgebase/tags/$tagId" -Body @{ name = "Updated Tag" } -Description "PUT /knowledgebase/tags/{id}"
}

# Search
Test-Api -Method "Get" -Endpoint "/knowledgebase/search?q=test" -Description "GET /knowledgebase/search"

# Feedback
if ($articleId) {
    $feedbackId = Test-Api -Method "Post" -Endpoint "/knowledgebase/$articleId/feedback" -Body @{ isHelpful = $true; comment = "Great article!" } -Description "POST /knowledgebase/{id}/feedback"
    Test-Api -Method "Get" -Endpoint "/knowledgebase/$articleId/feedback" -Description "GET /knowledgebase/{id}/feedback"
}

# Test Surveys Controller
Write-Host "`n=== SURVEYS CONTROLLER ===" -ForegroundColor Cyan

# Survey Templates
Test-Api -Method "Get" -Endpoint "/surveys/templates" -Description "GET /surveys/templates"

$templateId = Test-Api -Method "Post" -Endpoint "/surveys/templates" -Body @{ name = "Test Survey"; description = "Test"; questions = "[{`"q`":`"How was it?`"}]"; isDefault = $false } -Description "POST /surveys/templates"

if ($templateId) {
    Test-Api -Method "Get" -Endpoint "/surveys/templates/$templateId" -Description "GET /surveys/templates/{id}"
    Test-Api -Method "Put" -Endpoint "/surveys/templates/$templateId" -Body @{ name = "Updated Survey"; description = "Updated"; questions = "[{`"q`":`"Updated?`"}]"; isDefault = $false } -Description "PUT /surveys/templates/{id}"
    Test-Api -Method "Post" -Endpoint "/surveys/templates/$templateId/set-default" -Description "POST /surveys/templates/{id}/set-default"
    Test-Api -Method "Post" -Endpoint "/surveys/templates/$templateId/deactivate" -Description "POST /surveys/templates/{id}/deactivate"
    Test-Api -Method "Post" -Endpoint "/surveys/templates/$templateId/activate" -Description "POST /surveys/templates/{id}/activate"
}

# Ticket Surveys (need a ticket first)
Test-Api -Method "Get" -Endpoint "/surveys" -Description "GET /surveys"
Test-Api -Method "Get" -Endpoint "/surveys/analytics" -Description "GET /surveys/analytics"

# Test Chatbot Controller
Write-Host "`n=== CHATBOT CONTROLLER ===" -ForegroundColor Cyan

Test-Api -Method "Get" -Endpoint "/chatbot/sessions" -Description "GET /chatbot/sessions"

$sessionId = Test-Api -Method "Post" -Endpoint "/chatbot/sessions" -Body @{ title = "Test Chat Session" } -Description "POST /chatbot/sessions"

if ($sessionId.Id) {
    $chatId = $sessionId.Id
    Test-Api -Method "Get" -Endpoint "/chatbot/sessions/$chatId" -Description "GET /chatbot/sessions/{id}"
    Test-Api -Method "Put" -Endpoint "/chatbot/sessions/$chatId/title" -Body @{ title = "Updated Chat Title" } -Description "PUT /chatbot/sessions/{id}/title"

    # Send message
    $msgResult = Test-Api -Method "Post" -Endpoint "/chatbot/sessions/$chatId/messages" -Body @{ message = "Hello, how can I create a ticket?" } -Description "POST /chatbot/sessions/{id}/messages"

    Test-Api -Method "Get" -Endpoint "/chatbot/sessions/$chatId/messages" -Description "GET /chatbot/sessions/{id}/messages"
    Test-Api -Method "Post" -Endpoint "/chatbot/sessions/$chatId/end" -Description "POST /chatbot/sessions/{id}/end"
}

# Quick chat (anonymous)
$headersAnon = @{}
try {
    $quickResult = Invoke-RestMethod -Uri "$baseUrl/chatbot/quick" -Method Post -Body (@{ message = "Help me" } | ConvertTo-Json) -ContentType "application/json"
    Write-Host "POST /chatbot/quick (anonymous): PASSED" -ForegroundColor Green
    $passed++
} catch {
    Write-Host "POST /chatbot/quick (anonymous): FAILED - $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

Test-Api -Method "Get" -Endpoint "/chatbot/analytics" -Description "GET /chatbot/analytics"

# Test Audit Log Controller
Write-Host "`n=== AUDIT LOG CONTROLLER ===" -ForegroundColor Cyan

Test-Api -Method "Get" -Endpoint "/auditlog" -Description "GET /auditlog"
Test-Api -Method "Get" -Endpoint "/auditlog/actions" -Description "GET /auditlog/actions"
Test-Api -Method "Get" -Endpoint "/auditlog/entity-types" -Description "GET /auditlog/entity-types"
Test-Api -Method "Get" -Endpoint "/auditlog/summary" -Description "GET /auditlog/summary"

$auditId = Test-Api -Method "Post" -Endpoint "/auditlog" -Body @{ action = "Test Action"; entityType = "TestEntity"; entityId = "123"; oldValues = "{}"; newValues = "{}" } -Description "POST /auditlog"

if ($auditId) {
    Test-Api -Method "Get" -Endpoint "/auditlog/$auditId" -Description "GET /auditlog/{id}"
    Test-Api -Method "Get" -Endpoint "/auditlog/entity/TestEntity/123" -Description "GET /auditlog/entity/{type}/{id}"
}

Test-Api -Method "Get" -Endpoint "/auditlog/export?format=json" -Description "GET /auditlog/export"

# Test Team Chat Controller
Write-Host "`n=== TEAM CHAT CONTROLLER ===" -ForegroundColor Cyan

# Group Chats
Test-Api -Method "Get" -Endpoint "/team-chat/groups" -Description "GET /team-chat/groups"

$groupId = Test-Api -Method "Post" -Endpoint "/team-chat/groups" -Body @{ name = "Test Group Chat"; description = "A test group"; isPrivate = $false } -Description "POST /team-chat/groups"

if ($groupId.id) {
    $gId = $groupId.id
    Test-Api -Method "Get" -Endpoint "/team-chat/groups/$gId" -Description "GET /team-chat/groups/{id}"
    Test-Api -Method "Put" -Endpoint "/team-chat/groups/$gId" -Body @{ name = "Updated Group Chat"; description = "Updated"; isPrivate = $false } -Description "PUT /team-chat/groups/{id}"

    # Group Members
    Test-Api -Method "Get" -Endpoint "/team-chat/groups/$gId/members" -Description "GET /team-chat/groups/{id}/members"

    # Group Messages
    Test-Api -Method "Get" -Endpoint "/team-chat/groups/$gId/messages" -Description "GET /team-chat/groups/{id}/messages"

    $msgResult = Test-Api -Method "Post" -Endpoint "/team-chat/groups/$gId/messages" -Body @{ content = "Hello group!"; messageType = 0 } -Description "POST /team-chat/groups/{id}/messages"

    if ($msgResult.id) {
        $msgId = $msgResult.id
        Test-Api -Method "Put" -Endpoint "/team-chat/messages/$msgId" -Body @{ content = "Updated message" } -Description "PUT /team-chat/messages/{id}"

        # Reactions
        Test-Api -Method "Post" -Endpoint "/team-chat/messages/$msgId/reactions" -Body @{ emoji = "thumbsup" } -Description "POST /team-chat/messages/{id}/reactions"
        Test-Api -Method "Delete" -Endpoint "/team-chat/messages/$msgId/reactions/thumbsup" -Description "DELETE /team-chat/messages/{id}/reactions/{emoji}"

        Test-Api -Method "Delete" -Endpoint "/team-chat/messages/$msgId" -Description "DELETE /team-chat/messages/{id}"
    }

    # Read status
    Test-Api -Method "Post" -Endpoint "/team-chat/groups/$gId/read" -Description "POST /team-chat/groups/{id}/read"
    Test-Api -Method "Post" -Endpoint "/team-chat/groups/$gId/mute?mute=true" -Description "POST /team-chat/groups/{id}/mute"
    Test-Api -Method "Post" -Endpoint "/team-chat/groups/$gId/mute?mute=false" -Description "POST /team-chat/groups/{id}/unmute"
}

# Team Messages (need an existing team)
Test-Api -Method "Get" -Endpoint "/team-chat/teams/1/messages" -Description "GET /team-chat/teams/{teamId}/messages"

# Test User Permissions
Write-Host "`n=== USER PERMISSIONS ===" -ForegroundColor Cyan

# Get current user ID from login response or use a test user
$currentUserId = $loginResponse.userId
if ($currentUserId) {
    Test-Api -Method "Get" -Endpoint "/users/$currentUserId/permissions" -Description "GET /users/{id}/permissions"
    Test-Api -Method "Put" -Endpoint "/users/$currentUserId/permissions" -Body @{ grantedPermissionIds = @(1, 2); deniedPermissionIds = @() } -Description "PUT /users/{id}/permissions"
} else {
    # Get first user from the list
    $usersResult = Test-Api -Method "Get" -Endpoint "/users?pageSize=1" -Description "GET /users (for permissions test)"
    if ($usersResult.items -and $usersResult.items.Count -gt 0) {
        $testUserId = $usersResult.items[0].id
        Test-Api -Method "Get" -Endpoint "/users/$testUserId/permissions" -Description "GET /users/{id}/permissions"
        Test-Api -Method "Put" -Endpoint "/users/$testUserId/permissions" -Body @{ grantedPermissionIds = @(); deniedPermissionIds = @() } -Description "PUT /users/{id}/permissions"
    }
}

# Test Analytics Endpoints
Write-Host "`n=== ANALYTICS CONTROLLER ===" -ForegroundColor Cyan

Test-Api -Method "Get" -Endpoint "/analytics/dashboard" -Description "GET /analytics/dashboard"
Test-Api -Method "Get" -Endpoint "/analytics/tickets" -Description "GET /analytics/tickets"
Test-Api -Method "Get" -Endpoint "/analytics/tickets?startDate=2025-01-01&endDate=2026-12-31" -Description "GET /analytics/tickets (with dates)"
Test-Api -Method "Get" -Endpoint "/analytics/tickets/trend" -Description "GET /analytics/tickets/trend"
Test-Api -Method "Get" -Endpoint "/analytics/tickets/by-category" -Description "GET /analytics/tickets/by-category"
Test-Api -Method "Get" -Endpoint "/analytics/tickets/by-agent" -Description "GET /analytics/tickets/by-agent"
Test-Api -Method "Get" -Endpoint "/analytics/tickets/by-company" -Description "GET /analytics/tickets/by-company"
Test-Api -Method "Get" -Endpoint "/analytics/performance" -Description "GET /analytics/performance"
Test-Api -Method "Get" -Endpoint "/analytics/performance?startDate=2025-01-01&endDate=2026-12-31" -Description "GET /analytics/performance (with dates)"
Test-Api -Method "Get" -Endpoint "/analytics/sla/performance" -Description "GET /analytics/sla/performance"
Test-Api -Method "Get" -Endpoint "/analytics/export" -Description "GET /analytics/export (JSON)"
Test-Api -Method "Get" -Endpoint "/analytics/export?format=csv" -Description "GET /analytics/export (CSV)"

# Cleanup - Delete test data
Write-Host "`n=== CLEANUP ===" -ForegroundColor Cyan

if ($escId) { Test-Api -Method "Delete" -Endpoint "/sla/escalations/$escId" -Description "DELETE /sla/escalations/{id}" }
if ($slaRuleId) { Test-Api -Method "Delete" -Endpoint "/sla/rules/$slaRuleId" -Description "DELETE /sla/rules/{id}" }
if ($bhId) { Test-Api -Method "Delete" -Endpoint "/sla/business-hours/$bhId" -Description "DELETE /sla/business-hours/{id}" }
if ($holidayId) { Test-Api -Method "Delete" -Endpoint "/sla/holidays/$holidayId" -Description "DELETE /sla/holidays/{id}" }
if ($tagId) { Test-Api -Method "Delete" -Endpoint "/knowledgebase/tags/$tagId" -Description "DELETE /knowledgebase/tags/{id}" }
if ($articleId) { Test-Api -Method "Delete" -Endpoint "/knowledgebase/$articleId" -Description "DELETE /knowledgebase/{id}" }
if ($templateId) { Test-Api -Method "Delete" -Endpoint "/surveys/templates/$templateId" -Description "DELETE /surveys/templates/{id}" }
if ($chatId) { Test-Api -Method "Delete" -Endpoint "/chatbot/sessions/$chatId" -Description "DELETE /chatbot/sessions/{id}" }
if ($auditId) { Test-Api -Method "Delete" -Endpoint "/auditlog/$auditId" -Description "DELETE /auditlog/{id}" }
if ($gId) { Test-Api -Method "Delete" -Endpoint "/team-chat/groups/$gId" -Description "DELETE /team-chat/groups/{id}" }

# Summary
Write-Host "`n=====================================" -ForegroundColor Cyan
Write-Host "TOTAL PASSED: $passed" -ForegroundColor Green
Write-Host "TOTAL FAILED: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "=====================================" -ForegroundColor Cyan
