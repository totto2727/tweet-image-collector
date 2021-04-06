namespace tweet_image_collector.functions

open System
open System.IO
open System.Net.Http
open System.Net.Http.Headers
open Tweetinvi
open Tweetinvi.Iterators
open Tweetinvi.Models
open Tweetinvi.Models.V2
open Tweetinvi.Parameters
open tweet_image_collector.functions.SqlType

module Twitter =
    module Setting =
        let createParameters query =
            let newParameters =
                V2.SearchTweetsV2Parameters($"has:images -is:retweet -is:quote {query}")

            newParameters.Expansions.Add(V2.TweetResponseFields.Expansions.AttachmentsMediaKeys)
            |> ignore

            newParameters.TweetFields.Add(V2.TweetResponseFields.Tweet.Attachments)
            |> ignore

            newParameters.TweetFields.Add(V2.TweetResponseFields.Tweet.AuthorId)
            |> ignore

            newParameters.TweetFields.Add(V2.TweetResponseFields.Tweet.CreatedAt)
            |> ignore

            newParameters.MediaFields.Add(V2.TweetResponseFields.Media.Url)
            |> ignore

            newParameters.PageSize <- 50

            newParameters

        let createCredentialStore bearer =
            let credentialStore = ConsumerOnlyCredentials()

            do credentialStore.BearerToken <- bearer

            credentialStore

    module Request =
        let createTweetSearchIterator (client: TwitterClient)
                                      (parameters: V2.SearchTweetsV2Parameters)
                                      =
            client.SearchV2.GetSearchTweetsV2Iterator(parameters)

        let queryHasImageTweetsAsync (iterator: ITwitterRequestIterator<SearchTweetsV2Response, string>) =
            async {
                let! response = iterator.NextPageAsync() |> Async.AwaitTask

                return
                    [| for tweet in response.Content.Tweets do
                        for mediaKey in tweet.Attachments.MediaKeys do
                            let media =
                                response.Content.Includes.Media
                                |> Array.find (fun x -> x.MediaKey.Equals mediaKey)

                            if media.Type = "photo" then yield tweet, media |]
            }

        let downloadImageAsync (client: HttpClient) (url: string) (path: String) =
            async {
                if File.Exists path then
                    printfn $"Exist {path}"

                else
                    let url = url + "?format=jpg&name=large"
                    printfn $"Downloading {url}"

                    let! response = client.GetStreamAsync url |> Async.AwaitTask

                    use fileStream = File.Create path

                    do! response.CopyToAsync fileStream
                        |> Async.AwaitTask

                    printfn "finish Download"

                return true
            }

        let downloadImagesAsync (data: (TweetV2 * MediaV2) array) =
            let getSaveFilePath basePath ((tweet, media): TweetV2 * MediaV2) =
                Path.Join(basePath, sprintf $"""{tweet.Id}-{media.MediaKey}.jpg""")

            let createDownloader basePath ((tweet, media): TweetV2 * MediaV2) =
                async {
                    let path =
                        getSaveFilePath basePath (tweet, media)

                    let! result = downloadImageAsync Statics.HttpClient media.Url path
                    return tweet, media, result
                }

            async {
                let! setting = Sql.getSettingFirstAsync ()

                Directory.CreateDirectory setting.SaveFolderPath
                |> ignore

                let downloader =
                    createDownloader setting.SaveFolderPath

                return! data |> Array.map downloader |> Async.Parallel
            }

        let queryTweetAsync (query: string) =
            async {
                let! setting = Sql.getSettingFirstAsync ()

                let credentialStore =
                    Setting.createCredentialStore setting.Bearer

                let client = TwitterClient(credentialStore)
                let parameters = Setting.createParameters query

                let iterator =
                    createTweetSearchIterator client parameters

                return! queryHasImageTweetsAsync iterator
            }
        