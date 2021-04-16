module tweet_image_collector.views.Setting

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.Elmish
open Elmish
open tweet_image_collector.functions
open tweet_image_collector.views
open tweet_image_collector.views.Template

type State =
    { Setting: AppSetting
      IsEnableUpdateButton: bool }

type Msg =
    | NoChange
    | UpdateSetting of AppSetting
    | UpdateSaveFolderPath of string
    | UpdateBearer of string
    | UpdateSql
    | EnableButton

let init: State * Cmd<_> =
    { Setting =
          { Bearer = ""
            SaveFolderPath = ""
            Id = 0L }
      IsEnableUpdateButton = true },
    Cmd.OfAsyncImmediate.perform Sql.getSettingFirstAsync () UpdateSetting

let update (msg: Msg) (state: State) =
    match msg with
    | NoChange -> state, Cmd.none
    | UpdateSetting setting -> { state with Setting = setting }, Cmd.none
    | UpdateSaveFolderPath saveFolderPath ->
        { state with
              Setting =
                  { state.Setting with
                        SaveFolderPath = saveFolderPath } },
        Cmd.none

    | UpdateBearer bearer ->
        { state with
              Setting = { state.Setting with Bearer = bearer } },
        Cmd.none

    | UpdateSql ->
        { state with
              IsEnableUpdateButton = false },
        Cmd.OfAsyncImmediate.perform Sql.updateSettingAsync state.Setting
        <| fun _ -> EnableButton

    | EnableButton ->
        { state with
              IsEnableUpdateButton = true },
        Cmd.none



let view (state: State) dispatch =
    DockPanel.create [
        DockPanel.children [
            stackPanel [
                textBox
                <| TextBoxRecord<Msg>
                    .Create(label = "Bearer", text = state.Setting.Bearer, msg = UpdateBearer)
                <| dispatch

                textBox
                <| TextBoxRecord<Msg>
                    .Create(label = "SaveFolderPath", text = state.Setting.SaveFolderPath, msg = UpdateSaveFolderPath)
                <| dispatch

                stackPanel [
                    Button.create [
                        Button.onClick <| fun _ -> dispatch UpdateSql
                        Button.content "Update"
                        Button.isEnabled state.IsEnableUpdateButton
                    ]
                ]
            ]
        ]
    ]

type Host() as this =
    inherit Hosts.HostControl()

    do
        let startFn () = init

        Elmish.Program.mkProgram startFn update view
        |> Program.withHost this
        |> Program.run
