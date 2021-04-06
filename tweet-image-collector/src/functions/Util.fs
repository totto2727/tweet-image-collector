namespace tweet_image_collector.functions

open System
open System.IO
open System.Net.Http
open Avalonia.Controls
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Microsoft.VisualBasic.FileIO
open Tweetinvi.Models.V2

type Statics =
    static member HttpClient = new HttpClient()

    static member rootFolderPath =
        Path.Join [|
//            Path.GetDirectoryName(System
//                .Diagnostics
//                .Process
//                .GetCurrentProcess()
//                .MainModule.FileName)
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            "tweet_images"
        |]

module Util =
    let createdAtToDatetime (created: string) =
        created.Split([| '/'; ':'; ' ' |])
        |> Array.map int
        |> function
        | [| year; month; day; hour; minute; second |] ->
            DateTime(year, month, day, hour, minute, second)
        | _ -> DateTime.Now

    let getSaveFilePath basePath ((tweet, media): TweetV2 * MediaV2) =
        Path.Join(basePath, sprintf $"""{tweet.Id}-{media.MediaKey}.jpg""")
