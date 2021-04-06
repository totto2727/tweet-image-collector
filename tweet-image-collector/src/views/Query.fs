namespace tweet_image_collector.views

open System
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.Elmish
open Elmish
open tweet_image_collector.functions
open tweet_image_collector.functions.Twitter
open tweet_image_collector.views
open Tweetinvi.Models.V2

module Query =

    type State =
        { Query: string
          ImageArray: string array
          IsEnableQueryButton: bool }

    type Msg =
        | QueryUpdate of string
        | StartQuery
        | EnableQueryButton
        | QueryTweet
        | DownloadImage of (TweetV2 * MediaV2) array
        | InsertMediaInfo of (TweetV2 * MediaV2 * bool) array

    let init: State * Cmd<_> =
        { Query = ""
          ImageArray = Array.empty
          IsEnableQueryButton = true },
        Cmd.OfAsyncImmediate.perform (fun () ->
            async {
                let! lastQuery = Sql.getLatestQueryHistoryAsync ()
                return lastQuery.Query
            }) () QueryUpdate

    let StartQueryCmd (state: State) =
        Cmd.OfAsyncImmediate.perform
            Sql.insertQueryHistorySingleAsync
            ({ Id = 0L
               Query = state.Query
               QueryDateTime = DateTime.UtcNow.ToString("u") }: SqlType.QueryHistory)
        <| fun _ -> QueryTweet

    let queryTweetCmd (state: State) =
        Cmd.OfAsyncImmediate.perform Request.queryTweetAsync state.Query DownloadImage

    let downloadImageCmd (data: (TweetV2 * MediaV2) array) =
        Cmd.OfAsyncImmediate.perform Request.downloadImagesAsync data InsertMediaInfo

    let insertMediaInfo (data: (TweetV2 * MediaV2 * bool) array) =
        Cmd.OfAsyncImmediate.perform Sql.upsertMediaInfoAsync data
        <| fun _ -> EnableQueryButton

    let update (msg: Msg) (state: State): State * Cmd<_> =
        match msg with
        | QueryUpdate query -> { state with Query = query }, Cmd.none
        | EnableQueryButton ->
            { state with
                  IsEnableQueryButton = true },
            Cmd.none

        | StartQuery ->
            { state with
                  IsEnableQueryButton = false },
            StartQueryCmd state

        | QueryTweet ->
            { state with
                  IsEnableQueryButton = false },
            queryTweetCmd state

        | DownloadImage data ->
            { state with
                  IsEnableQueryButton = false },
            downloadImageCmd data

        | InsertMediaInfo data ->
            { state with
                  IsEnableQueryButton = false },
            insertMediaInfo data

    let view (state: State) dispatch =
        DockPanel.create [
            DockPanel.children [
                Template.stackPanel [
                    Template.textBox
                    <| Template.TextBoxRecord<_>
                        .Create(label = "Query", text = state.Query, msg = QueryUpdate)
                    <| dispatch

                    Template.stackPanel [
                        Button.create [
                            Button.onClick (fun _ -> dispatch StartQuery)
                            Button.content "Query"
                            Button.isEnabled state.IsEnableQueryButton
                        ]
                    ]
                ]
            ]
        ]

    type Host() as this=
        inherit Hosts.HostControl()
        do
            let startFn()=init
            Elmish.Program.mkProgram startFn update view
            |>Program.withHost this
            |>Program.run