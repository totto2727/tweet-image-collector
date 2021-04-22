module tweet_image_collector.functions.Sql

open System
open System.Diagnostics
open System.Reflection
open System.Data
open Microsoft.Data.Sqlite
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Tweetinvi.Models
open System.IO
open Dapper
open tweet_image_collector.functions

let databaseSaveFolder =
    Path.Join [|
        Statics.rootFolderPath
        "sql"
    |]

let databaseFileName = "sqlite.sqlite3"

let databaseFileFullPath =
    Path.Join [|
        databaseSaveFolder
        databaseFileName
    |]

let connectionString =
    let builder = SqliteConnectionStringBuilder()
    builder.DataSource <- databaseFileFullPath
    builder.Mode <- SqliteOpenMode.ReadWriteCreate
    builder.ConnectionString

let queryAsync<'T> (sql: string) =
    async {
        use connection = new SqliteConnection(connectionString)

        try
            do! connection.OpenAsync() |> Async.AwaitTask

            let! result =
                sql
                |> connection.QueryAsync<'T>
                |> Async.AwaitTask

            do! connection.CloseAsync() |> Async.AwaitTask

            return Ok <| Array.ofSeq result

        with :? SqliteException as ex -> return Error ex.ErrorCode
    }

let querySingleAsync<'T> (sql: string) =
    async {
        try
            use connection = new SqliteConnection(connectionString)

            do! connection.OpenAsync() |> Async.AwaitTask

            let! result =
                sql
                |> connection.QueryFirstAsync<'T>
                |> Async.AwaitTask

            do! connection.CloseAsync() |> Async.AwaitTask

            return Ok result

        with :? SqliteException as ex -> return Error ex.ErrorCode
    }

let executeAsync sql data =
    async {
        try
            use connection = new SqliteConnection(connectionString)

            do! connection.OpenAsync() |> Async.AwaitTask

            for x in data do
                printfn "%s" <| x.ToString()

            let! times =
                (sql, data)
                |> fun (x, y) -> connection.ExecuteAsync(x, param = y)
                |> Async.AwaitTask

            do! connection.CloseAsync() |> Async.AwaitTask

            return Ok times

        with :? SqliteException as ex -> return Error ex.ErrorCode
    }

let executeSingleAsync sql data = executeAsync sql ([ data ])

let upsertMediaInfoAsync (data: (V2.TweetV2 * V2.MediaV2 * bool) array) =
    executeAsync
        @"
insert into MediaInfo values
( @Id, @Prefix, @MediaUrl, @TweetId, @TweetAuthorId, @CreatedAt, @IsDownloaded)
on conflict (Id) do update set IsDownloaded=@IsDownloaded;"
    <| Array.map MediaInfo.Create data

let getLatestCreatedAtAsync () =
    querySingleAsync<{| CreatedAt: string |}>
        @"
select CreatedAt from MediaInfo order by MediaKey desc limit 1;"

let getOldestCreatedAtAsync () =
    querySingleAsync<{| CreatedAt: string |}>
        @"
select CreatedAt from MediaInfo order by MediaKey asc limit 1;"

let getSettingFirstAsync () =
    querySingleAsync<AppSetting>
        @"
select * from Setting order by Id desc limit 1;"

let insertSettingSingleAsync (data: AppSetting) =
    executeSingleAsync
        @"
insert or ignore into Setting(Bearer,SaveFolderPath) values (@Bearer,@SaveFolderPath);"
    <| data

let updateSettingAsync (data: AppSetting) =
    executeSingleAsync
        @"
update Setting set Bearer=@Bearer,SaveFolderPath=@SaveFolderPath where Id=@Id;"
    <| data

let insertQueryHistorySingleAsync (data: QueryHistory) =
    executeSingleAsync
        @"
insert or ignore into QueryHistory(Query,QueryDateTime) values (@Query,@QueryDateTime);"
    <| data

let getLatestQueryHistoryAsync () =
    querySingleAsync<string>
        @"
select Query from QueryHistory order by Id desc limit 1;"

let getMediaInfoAsync () =
    queryAsync<MediaInfo>
        @"
select * from MediaInfo order by Id desc limit 100;"

type InitializeError =
    | UnauthorizedAccess
    | PathTooLong
    | DirectoryNotFound
    | AlreadyFileExisted
    | UnauthorizedArgument
    | ArgumentNull
    | NotSupported

let initializeDBFileAsync () =
    async {
        try
            Directory.CreateDirectory(databaseSaveFolder)
            |> ignore

            if not <| File.Exists databaseFileFullPath then
                use newFs = File.Create(databaseFileFullPath)
                let assembly = Assembly.GetExecutingAssembly()

                let assemblyDBFileName =
                    assembly.GetManifestResourceNames()
                    |> fun x -> Array.Find(x, (fun x -> x.Contains databaseFileName))

                use databaseFs =
                    assembly.GetManifestResourceStream(assemblyDBFileName)

                do! databaseFs.CopyToAsync newFs |> Async.AwaitTask

            return Ok()
        with
            | :? UnauthorizedAccessException -> return Error UnauthorizedAccess
            | :? PathTooLongException -> return Error PathTooLong
            | :? DirectoryNotFoundException -> return Error DirectoryNotFound
            | :? IOException -> return Error AlreadyFileExisted
            | :? ArgumentNullException -> return Error ArgumentNull
            | :? ArgumentException -> return Error UnauthorizedArgument
            | :? NotSupportedException -> return Error NotSupported
    }

let initializeDBAsync () =
    let createMediaInfo = @"
create table if not exists MediaInfo
(   Id            integer primary key ,
    Prefix        integer not null,
    MediaUrl      text not null,
    TweetId       integer not null,
    TweetAuthorId integer not null,
    CreatedAt     text not null,
    IsDownloaded  integer not null
);"

    let createSetting = @"
create table if not exists Setting
(
    Id             integer primary key,
    Bearer         text not null,
    SaveFolderPath text not null
);"

    let createQueryHistory = @"
create table if not exists QueryHistory
(
    Id integer primary key,
    Query text not null,
    QueryDateTime text not null
)"

    let createSqlArray =
        [| createMediaInfo
           createSetting
           createQueryHistory |]

    async {
        printfn "start Check"

        match! initializeDBFileAsync () with
        | Ok _ ->

            do!
                createSqlArray
                |> Array.Parallel.map (fun x -> executeSingleAsync x 0)
                |> Async.Parallel
                |> Async.Ignore

            do!
                executeSingleAsync
                    "insert or ignore into Setting values (@Id,@Bearer,@SaveFolderPath);"
                    AppSetting.Default
                |> Async.Ignore

            do!
                executeSingleAsync
                    "insert or ignore into QueryHistory values (@Id,@Query,@QueryDateTime);"
                    QueryHistory.Default
                |> Async.Ignore

            return Ok()

        | Error _ as ex -> return Error ex
    }
