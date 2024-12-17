module rest_api_fs.handlers.User

open System
open System.Data.SQLite
open System.IO
open System.Net
open System.Text.Json
open System.Text
open System.Text.Json.Serialization

type Users = {
    [<JsonPropertyName("id")>] Id: int
    [<JsonPropertyName("name")>] Name: string
}

type ApiResponse = {
    [<JsonPropertyName("status")>] Status: string
    [<JsonPropertyName("code")>] Code: int
}

type ApiResponseWithData = {
    [<JsonPropertyName("status")>] Status: string
    [<JsonPropertyName("code")>] Code: int
    [<JsonPropertyName("data")>] Data: Users list
}

type ApiResponseWithErrors = {
    [<JsonPropertyName("status")>] Status: string
    [<JsonPropertyName("code")>] Code: int
    [<JsonPropertyName("errors")>] Errors: string
}

let extractBody (request: HttpListenerRequest) =
    use reader = new StreamReader(request.InputStream)
    reader.ReadToEnd()
    
let parseFormEncoded (body: string) =
    if String.IsNullOrWhiteSpace(body) then
        Map.empty
    else
        body.Split('&')
        |> Array.choose (fun pair ->
            let parts = pair.Split('=')
            if parts.Length = 2 then
                let key = System.Web.HttpUtility.UrlDecode(parts.[0])
                let value = System.Web.HttpUtility.UrlDecode(parts.[1])
                Some (key, value)
            else
                None)
        |> Map.ofArray
        
let handleReadUsers(response: HttpListenerResponse) =
    async {
        use conn = new SQLiteConnection("Data Source=rest_api_fs.db;Version=3;")
        conn.Open()
        let cmd = conn.CreateCommand()
        cmd.CommandText <- "SELECT id, name FROM users;"
        use reader = cmd.ExecuteReader()
        let users = [ while reader.Read() do
                      yield { Id = reader.GetInt32(0); Name = reader.GetString(1) } ]
        response.StatusCode <- int HttpStatusCode.OK
        let responseObject = { Status = "OK"; Code = 200; Data = users }
        let responseBody = JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
        return responseBody
    }
    
let handleCreateUser(request: HttpListenerRequest, response: HttpListenerResponse) =
    async {
        let body = extractBody request
        let formData = parseFormEncoded body
        let responseBody =
            match Map.tryFind "name" formData with
                | Some name when not (String.IsNullOrWhiteSpace(name)) ->
                    use conn = new SQLiteConnection("Data Source=rest_api_fs.db;Version=3;")
                    conn.Open()
                    let cmd = conn.CreateCommand()
                    cmd.CommandText <- "INSERT INTO users (name) VALUES (@name);"
                    cmd.Parameters.AddWithValue("@name", name) |> ignore
                    cmd.ExecuteNonQuery() |> ignore
                    response.StatusCode <- int HttpStatusCode.Created
                    let responseObject = { Status = "Created"; Code = 201 }
                    JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
                | Some _ ->
                    response.StatusCode <- int HttpStatusCode.BadRequest
                    let responseObject = { Status = "Bad Request"; Code = 400; Errors = "Missing 'name' parameter" }
                    JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
                | None ->
                    response.StatusCode <- int HttpStatusCode.BadRequest
                    let responseObject = { Status = "Bad Request"; Code = 400; Errors = "Missing 'name' parameter" }
                    JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
        return responseBody
    }
    
let handleUpdateUser(request: HttpListenerRequest, response: HttpListenerResponse)=
    async {
        let body = extractBody request
        let formData = parseFormEncoded body
        let responseBody =
            match Map.tryFind "id" formData, Map.tryFind "name" formData with
            | Some id, Some name when not (String.IsNullOrWhiteSpace(id) || String.IsNullOrWhiteSpace(name)) ->
                use conn = new SQLiteConnection("Data Source=rest_api_fs.db;Version=3;")
                conn.Open()
                let cmd = conn.CreateCommand()
                cmd.CommandText <- "UPDATE users SET name = @name WHERE id = @id;"
                cmd.Parameters.AddWithValue("@name", name) |> ignore
                cmd.Parameters.AddWithValue("@id", id) |> ignore
                cmd.ExecuteNonQuery() |> ignore
                response.StatusCode <- int HttpStatusCode.OK
                let responseObject = { Status = "OK"; Code = 200 }
                JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
            | Some _, Some _ ->
                response.StatusCode <- int HttpStatusCode.BadRequest
                let responseObject = { Status = "Bad Request"; Code = 400; Errors = "Missing 'id' or 'name' parameter" }
                JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
            | _ ->
                response.StatusCode <- int HttpStatusCode.BadRequest
                let responseObject = { Status = "Bad Request"; Code = 400; Errors = "Missing 'id' or 'name' parameter" }
                JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
        return responseBody
    }
    
let handleDeleteUser(request: HttpListenerRequest, response: HttpListenerResponse) =
    async {
        let body = extractBody request
        let formData = parseFormEncoded body
        let responseBody =
            match Map.tryFind "id" formData with
            | Some id when not (String.IsNullOrWhiteSpace(id)) ->
                use conn = new SQLiteConnection("Data Source=rest_api_fs.db;Version=3;")
                conn.Open()
                let cmd = conn.CreateCommand()
                cmd.CommandText <- "DELETE FROM users WHERE id = @id;"
                cmd.Parameters.AddWithValue("@id", id) |> ignore
                cmd.ExecuteNonQuery() |> ignore
                response.StatusCode <- int HttpStatusCode.OK
                let responseObject = { Status = "OK"; Code = 200 }
                JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
            | Some _ ->
                response.StatusCode <- int HttpStatusCode.BadRequest
                let responseObject = { Status = "Bad Request"; Code = 400; Errors = "Missing 'id' parameter" }
                JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
            | None ->
                response.StatusCode <- int HttpStatusCode.BadRequest
                let responseObject = { Status = "Bad Request"; Code = 400; Errors = "Missing 'id' parameter" }
                JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
        return responseBody
    }

let userHandler(listener: HttpListener) =
    let rec listenForRequests () =
        async {
            let! context = listener.GetContextAsync() |> Async.AwaitTask
            let request = context.Request
            let response = context.Response
            try
                let! responseBody =
                    if request.Url.AbsolutePath = "/users" || request.Url.AbsolutePath = "/users/" then
                        match request.HttpMethod with
                        | "GET" -> handleReadUsers(response)
                        | "POST" -> handleCreateUser(request, response)
                        | "PUT" -> handleUpdateUser(request, response)
                        | "DELETE" -> handleDeleteUser(request, response)
                        | _ ->
                            async {
                                response.StatusCode <- int HttpStatusCode.MethodNotAllowed
                                let responseObject = { Status = "Method Not Allowed"; Code = 405 }
                                return JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
                            }
                    else
                        async {
                            response.StatusCode <- int HttpStatusCode.NotFound
                            let responseObject = { Status = "Not Found"; Code = 404 }
                            return JsonSerializer.Serialize(responseObject, JsonSerializerOptions(PropertyNamingPolicy = null))
                        }
                let buffer: byte[] = Encoding.UTF8.GetBytes(responseBody)
                response.ContentLength64 <- int64 buffer.Length
                response.ContentType <- "application/json"
                do! response.OutputStream.AsyncWrite(buffer, 0, buffer.Length)
                response.OutputStream.Close()
            with ex ->
                response.OutputStream.Close()
            return! listenForRequests ()
        }
    listenForRequests ()
