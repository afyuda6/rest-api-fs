module rest_api_fs.database.Sqlite

open System.Data.SQLite

let initializeDatabase () =
    let connection = new SQLiteConnection($"Data Source=rest_api_fs.db;Version=3;")
    connection.Open()
    use command = connection.CreateCommand()
    command.CommandText <-
        """
        DROP TABLE IF EXISTS users;
        """
    command.ExecuteNonQuery();
    command.CommandText <-
        """
        CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL
        );
        """
    command.ExecuteNonQuery()
    connection.Close()
