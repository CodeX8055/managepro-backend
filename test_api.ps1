$log = @()
$regObj = @{ username="test5"; email="test5@test.com"; password="password1" } | ConvertTo-Json
$rReg = Invoke-RestMethod -Uri "http://localhost:5260/api/auth/register" -Method POST -Headers @{"Content-Type"="application/json"} -Body $regObj -SkipHttpErrorCheck 
$log += "Reg: " + $rReg

$logObj = @{ email="test5@test.com"; password="password1" } | ConvertTo-Json
$rLog = Invoke-RestMethod -Uri "http://localhost:5260/api/auth/login" -Method POST -Headers @{"Content-Type"="application/json"} -Body $logObj -SkipHttpErrorCheck
$token = $rLog.token
$log += "Login: " + $rLog

$projObj = @{ name="Project5"; description="Test desc" } | ConvertTo-Json
$rProj = Invoke-RestMethod -Uri "http://localhost:5260/api/project" -Method POST -Headers @{"Authorization"="Bearer $($token)"; "Content-Type"="application/json"} -Body $projObj -SkipHttpErrorCheck
$pid = $rProj.id
$log += "Proj: " + $rProj

$taskObj = @{ projectId=$pid; title="Task 5"; priority="Medium"; status="To Do"; deadline=$null } | ConvertTo-Json
$rTask = Invoke-RestMethod -Uri "http://localhost:5260/api/task" -Method POST -Headers @{"Authorization"="Bearer $($token)"; "Content-Type"="application/json"} -Body $taskObj -SkipHttpErrorCheck
$log += "Task: " + $rTask

$log | Out-File run_log.txt -Encoding ASCII
