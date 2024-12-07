open System
open System.Net
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open rest_api_fs.database.Sqlite
open rest_api_fs.handlers.User

type ApiResponse = {
    [<JsonPropertyName("status")>] Status: string
    [<JsonPropertyName("code")>] Code: int
}

[<EntryPoint>]
let main _ =
    initializeDatabase ()

    let listener = new HttpListener()
    listener.Prefixes.Add("http://localhost:6012/")
    listener.Start()

    let rec listenForRequests () =
        async {
            let! context = listener.GetContextAsync() |> Async.AwaitTask
            let request = context.Request
            let response = context.Response

            try
                if request.Url.AbsolutePath = "/users" || request.Url.AbsolutePath = "/users/" then
                    do! userHandle request response
                else
                    let mutable responseBody = ""
                    response.StatusCode <- int HttpStatusCode.NotFound
                    let responseObject = { Status = "Not Found"; Code = 404 }
                    responseBody <- JsonSerializer.Serialize(responseObject)
                    let buffer: byte[] = Encoding.UTF8.GetBytes(responseBody : string)
                    response.ContentLength64 <- int64 buffer.Length
                    response.ContentType <- "application/json"
                    do! response.OutputStream.AsyncWrite(buffer, 0, buffer.Length)
                    response.OutputStream.Close()
            with ex ->
                let mutable responseBody = ""
                response.StatusCode <- int HttpStatusCode.InternalServerError
                let responseObject = { Status = "Internal Server Error"; Code = 500 }
                responseBody <- JsonSerializer.Serialize(responseObject)
                let buffer: byte[] = Encoding.UTF8.GetBytes(responseBody : string)
                response.ContentLength64 <- int64 buffer.Length
                response.ContentType <- "application/json"
                do! response.OutputStream.AsyncWrite(buffer, 0, buffer.Length)
                response.OutputStream.Close()

            return! listenForRequests ()
        }

    listenForRequests () |> Async.Start

    Console.ReadLine() |> ignore
    0
