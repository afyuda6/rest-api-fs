open System
open System.Net
open rest_api_fs.database.Sqlite
open rest_api_fs.handlers.User

[<EntryPoint>]
let main _ =
    initializeDatabase ()
    let listener = new HttpListener()
    listener.Prefixes.Add("http://localhost:6012/")
    listener.Start()
    let rec listenForRequests () =
        async {
            do! userHandler listener
            return! listenForRequests ()
        }
    listenForRequests () |> Async.Start
    Console.ReadLine() |> ignore
    0
