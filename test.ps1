$regObj = @{ username="t2"; email="t2@t.com"; password="password1" } | ConvertTo-Json
$logObj = @{ email="t2@t.com"; password="password1" } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "http://localhost:5260/api/auth/register" -Method POST -Headers @{"Content-Type"="application/json"} -Body $regObj
    $r = Invoke-RestMethod -Uri "http://localhost:5260/api/auth/login" -Method POST -Headers @{"Content-Type"="application/json"} -Body $logObj
    $token = $r.token
    $projObj = @{ name="Test Project"; description="Test desc" } | ConvertTo-Json
    $p = Invoke-RestMethod -Uri "http://localhost:5260/api/project" -Method POST -Headers @{"Authorization"="Bearer $token"; "Content-Type"="application/json"} -Body $projObj
    $pid = $p.id
    $taskObj = @{ projectId=$pid; title="Test Task"; priority="Medium" } | ConvertTo-Json
    $t = Invoke-RestMethod -Uri "http://localhost:5260/api/task" -Method POST -Headers @{"Authorization"="Bearer $token"; "Content-Type"="application/json"} -Body $taskObj
    Write-Output "SUCCESS"
} catch {
    $_.Exception.Response.GetResponseStream() | %{ (New-Object System.IO.StreamReader($_)).ReadToEnd() }
}
