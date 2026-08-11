using System.Reflection;
using DbUp;

// AD-9: toda mudança de schema é um script DbUp numerado, executado exatamente uma vez.
// AD-21: seed nunca entra aqui — migration é schema, seed é dado de demonstração.

var conexao = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? throw new InvalidOperationException("CONNECTION_STRING não definida. Veja .env.example.");

Console.WriteLine("Aguardando o banco responder...");
for (var tentativa = 1; tentativa <= 40; tentativa++)
{
    try
    {
        EnsureDatabase.For.SqlDatabase(conexao);
        break;
    }
    catch (Exception ex) when (tentativa < 40)
    {
        Console.WriteLine($"  tentativa {tentativa}/40 — {ex.Message.Split('\n')[0]}");
        Thread.Sleep(3000);
    }
}

var upgrader = DeployChanges.To
    .SqlDatabase(conexao)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();

var resultado = upgrader.PerformUpgrade();

if (!resultado.Successful)
{
    Console.Error.WriteLine(resultado.Error);
    return 1;
}

Console.WriteLine("Schema atualizado.");
return 0;
