namespace tweet_image_collector.views

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.Elmish
open Elmish
open tweet_image_collector.functions
open tweet_image_collector.views
open tweet_image_collector.views.Template

module Setting =
    type State =
        { Setting: SqlType.AppSetting
          IsEnableUpdateButton: bool }

    type Msg =
        | NoChange
        | SettingUpdate of SqlType.AppSetting
        | SaveFolderPathUpdate of string
        | BearerUpdate of string
        | Update
        | ButtonEnable

    let init: State * Cmd<_> =
        { Setting =
              { Bearer = ""
                SaveFolderPath = ""
                Id = 0L }
          IsEnableUpdateButton = true },
        Cmd.OfAsyncImmediate.perform Sql.getSettingFirstAsync () SettingUpdate

    let update (msg: Msg) (state: State) =
        match msg with
        | NoChange -> state, Cmd.none
        | SettingUpdate setting -> { state with Setting = setting }, Cmd.none
        | SaveFolderPathUpdate saveFolderPath ->
            { state with
                  Setting =
                      { state.Setting with
                            SaveFolderPath = saveFolderPath } },
            Cmd.none
        | BearerUpdate bearer ->
            { state with
                  Setting =
                      { state.Setting with
                            Bearer = bearer } },
            Cmd.none
        | Update ->
            { state with
                  IsEnableUpdateButton = false },
            Cmd.OfAsyncImmediate.perform Sql.updateSettingAsync state.Setting
            <| fun _ -> ButtonEnable
        | ButtonEnable ->
            { state with
                  IsEnableUpdateButton = true },
            Cmd.none



    let view (state: State) dispatch =
        DockPanel.create [
            DockPanel.children [
                stackPanel [
                    textBox
                    <| TextBoxRecord<Msg>
                        .Create(label = "Bearer", text = state.Setting.Bearer, msg = BearerUpdate)
                    <| dispatch

                    textBox
                    <| TextBoxRecord<Msg>
                        .Create(label = "SaveFolderPath",
                                text = state.Setting.SaveFolderPath,
                                msg = SaveFolderPathUpdate)
                    <| dispatch

                    stackPanel [
                        Button.create [
                            Button.onClick <| fun _ -> dispatch Update
                            Button.content "Update"
                            Button.isEnabled state.IsEnableUpdateButton
                        ]
                    ]
                ]
            ]
        ]
