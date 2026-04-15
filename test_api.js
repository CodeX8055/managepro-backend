const test = async () => {
    try {
        const r1 = await fetch("http://localhost:5260/api/auth/register", {
            method: "POST", headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username: "t9", email: "t9@t.com", password: "password1" })
        });
        console.log("Reg:", await r1.text());
        
        const r2 = await fetch("http://localhost:5260/api/auth/login", {
            method: "POST", headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email: "t9@t.com", password: "password1" })
        });
        const loginText = await r2.text();
        console.log("Log:", loginText);
        const token = JSON.parse(loginText).token;
        
        const r3 = await fetch("http://localhost:5260/api/project", {
            method: "POST", headers: { "Content-Type": "application/json", "Authorization": "Bearer " + token },
            body: JSON.stringify({ name: "Project9", description: "Desc" })
        });
        const projText = await r3.text();
        console.log("Proj:", projText);
        const pid = JSON.parse(projText).id;

        const r4 = await fetch("http://localhost:5260/api/task", {
            method: "POST", headers: { "Content-Type": "application/json", "Authorization": "Bearer " + token },
            body: JSON.stringify({ projectId: pid, title: "Task 9", priority: "Medium", status: "To Do", deadline: "2026-04-10" })
        });
        console.log("Task Response:", r4.status, await r4.text());
    } catch(e) {
        console.log("ERR", e);
    }
};
test();
