module tweet_image_collector.functions.SqlType

open System
open System.IO
open Tweetinvi.Models.V2

[<CLIMutable>]
type AppSetting =
    { Id: int64
      Bearer: string
      SaveFolderPath: string }

    static member Default: AppSetting =
        { Id = 1L
          Bearer = ""
          SaveFolderPath =
              Path.Join [|
                  Statics.rootFolderPath
              |] }

[<CLIMutable>]
type QueryHistory =
    { Id: int64
      Query: string
      QueryDateTime: string }

    static member Default: QueryHistory =
        { Id = 1L
          Query = ""
          QueryDateTime = DateTime.UtcNow.ToString("u") }

[<CLIMutable>]
type MediaInfo =
    { TweetId: int64
      TweetAuthorId: int64
      CreatedAt: string
      Id: int64
      Prefix: int64
      MediaUrl: string
      IsDownloaded: bool }

    static member Create((tweet, media, isDownloaded): (TweetV2 * MediaV2 * bool)): MediaInfo =
        let mediaPrefix, mediaId =
            media.MediaKey.Split '_'
            |> Array.map Int64.Parse
            |> fun x -> x.[0], x.[1]

        { TweetId = Int64.Parse tweet.Id
          TweetAuthorId = Int64.Parse tweet.AuthorId
          CreatedAt = tweet.CreatedAt.UtcDateTime.ToString("u")
          Id = mediaId
          Prefix = mediaPrefix
          MediaUrl = media.Url
          IsDownloaded = isDownloaded }

    member this.MediaKey = sprintf $"{this.Prefix}_{this.Id}"
