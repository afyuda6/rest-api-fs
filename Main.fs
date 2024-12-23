open System
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open rest_api_fs.database.Sqlite
open rest_api_fs.handlers.User

[<EntryPoint>]
let main args =
    initializeDatabase ()
    let port =
        match Environment.GetEnvironmentVariable("PORT") with
        | null | "" -> "6012"
        | envPort ->
            match Int32.TryParse(envPort) with
            | true, value -> value.ToString()
            | _ -> "6012"
    Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHost ->
            webHost
                .UseKestrel()
                .UseUrls($"http://0.0.0.0:{port}")
                .Configure(configureApp)
                .ConfigureServices(configureServices)
            |> ignore)
        .Build()
        .Run()
    0