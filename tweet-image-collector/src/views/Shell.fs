module tweet_image_collector.views.Shell

open System
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open tweet_image_collector.functions
open tweet_image_collector.functions.Twitter
open tweet_image_collector.views
open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI.Elmish

type State = { isStarting: Boolean }

type Msg = | Start

let init: State * Cmd<Msg> =

    { isStarting = true },
    Cmd.batch [
        Cmd.OfAsyncImmediate.perform Sql.initializeDBAsync () (fun _ -> Start)

    ]

let update (msg: Msg) (state: State): State * Cmd<Msg> =
    match msg with
    | Start -> { state with isStarting = false }, Cmd.none

let view (state: State) (dispatch) =
    DockPanel.create [
        DockPanel.children [
            if state.isStarting then
                TextBlock.create [
                    TextBlock.fontSize 32.0
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    StackPanel.maxWidth 700.0
                    
                    TextBlock.text "Starting"
                ]
            else
                TabControl.create [
                    TabControl.tabStripPlacement Dock.Top
                    TabControl.viewItems [
                        TabItem.create [
                            TabItem.header "Request"
                            TabItem.content (ViewBuilder.Create<Query.Host>([]))
                        ]
                        TabItem.create [
                            TabItem.header "Setting"
                            TabItem.content (ViewBuilder.Create<Setting.Host>([]))
                        ]
                    ]
                ]
        ]
    ]

type MainWindow() as this =
    inherit HostWindow()

    do
        base.Title <- "Full App"
        base.Width <- 800.0
        base.Height <- 600.0
        base.MinWidth <- 800.0
        base.MinHeight <- 600.0

        Elmish.Program.mkProgram (fun () -> init) update view
        |> Program.withHost this
        |> Program.run
