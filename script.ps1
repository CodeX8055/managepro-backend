$log = @()
$regObj = @{ username="tu3"; email="tu3@t.com"; password="password1" } | ConvertTo-Json
try { Invoke-RestMethod -Uri "http://localhost:5260/api/auth/register" -Method POST -Headers @{"Content-Type"="application/json"} -Body $regObj; $log+="Reg OK" } catch { $log+= "Reg Err: "+(New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() }

$logObj = @{ email="tu3@t.com"; password="password1" } | ConvertTo-Json
try { $r = Invoke-RestMethod -Uri "http://localhost:5260/api/auth/login" -Method POST -Headers @{"Content-Type"="application/json"} -Body $logObj; $token = $r.token; $log+="Login OK" } catch { $log+= "Log Err: "+(New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() }

$projObj = @{ name="Test"; description="Test desc" } | ConvertTo-Json
try { $p = Invoke-RestMethod -Uri "http://localhost:5260/api/project" -Method POST -Headers @{"Authorization"="Bearer $($token)"; "Content-Type"="application/json"} -Body $projObj; $pid = $p.id; $log+="Proj OK $pid" } catch { $log+= "Proj Err: "+(New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() }

$taskObj = @{ projectId=$pid; title="Task"; priority="Medium" } | ConvertTo-Json
try { Invoke-RestMethod -Uri "http://localhost:5260/api/task" -Method POST -Headers @{"Authorization"="Bearer $($token)"; "Content-Type"="application/json"} -Body $taskObj; $log+="Task OK" } catch { $log+= "Task Err: "+(New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() }

$log | Out-File result.log
