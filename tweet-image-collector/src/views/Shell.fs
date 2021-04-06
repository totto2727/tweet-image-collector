namespace tweet_image_collector.views

open System
open Avalonia.FuncUI.DSL
open tweet_image_collector.functions
open tweet_image_collector.functions.Twitter
open tweet_image_collector.functions.SqlType
open tweet_image_collector.views

module Shell =

    open Elmish
    open Avalonia
    open Avalonia.Controls
    open Avalonia.Input
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI
    open Avalonia.FuncUI.Builder
    open Avalonia.FuncUI.Components.Hosts
    open Avalonia.FuncUI.Elmish

    type State =
        { //counterState: Counter.State
          //queryState: Query.State
          settingState: Setting.State }

    type Msg =
        //| CounterMsg of Counter.Msg
        //| QueryMsg of Query.Msg
        | SettingMsg of Setting.Msg

    let init: State * Cmd<Msg> =
        let settingState, settingCmd = Setting.init
        //let queryState, queryCmd = Query.init

        { //counterState = Counter.init
          //queryState = queryState
          settingState = settingState },
        Cmd.batch [
            Cmd.map SettingMsg settingCmd
        //Cmd.map QueryMsg queryCmd
        ]

    let update (msg: Msg) (state: State): State * Cmd<Msg> =
        match msg with
//        | CounterMsg msg ->
//            { state with
//                  counterState = Counter.update msg state.counterState },
//            Cmd.none

        //        | QueryMsg msg ->
//            let queryState, cmd = Query.update msg state.queryState
//            { state with queryState = queryState }, Cmd.map QueryMsg cmd

        | SettingMsg msg ->
            let settingState, cmd =
                Setting.update msg state.settingState

            { state with
                  settingState = settingState },
            Cmd.map SettingMsg cmd

    let view (state: State) (dispatch) =
        DockPanel.create [
            DockPanel.children [
                TabControl.create [
                    TabControl.tabStripPlacement Dock.Top
                    TabControl.viewItems [
                        //                        TabItem.create [
//                            TabItem.header "Counter"
//                            TabItem.content (Counter.view state.counterState (CounterMsg >> dispatch))
//                        ]
                        TabItem.create [
                            TabItem.header "Request"
                            TabItem.content (ViewBuilder.Create<Query.Host>([]))
                        ]
                        TabItem.create [
                            TabItem.header "Setting"
                            TabItem.content
                                (Setting.view state.settingState (SettingMsg >> dispatch))
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
