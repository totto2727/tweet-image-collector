module tweet_image_collector.views.Template

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout

let stackPanel children =
    StackPanel.create [
        StackPanel.dock Dock.Top
        StackPanel.margin 10.0
        StackPanel.spacing 10.0
        StackPanel.children children
        StackPanel.maxWidth 700.0
    ]
    |> Helpers.generalize

type TextBoxRecord<'T> =
    { Label: string
      Text: string
      Msg: string -> 'T
      TextBoxAttributes: Types.IAttr<TextBox> list option
      TextBlockAttributes: Types.IAttr<TextBlock> list option }

    static member Create(label, text, msg, ?textBoxAttributes, ?textBlockAttributes): TextBoxRecord<_> =
        { Label = label
          Text = text
          Msg = msg
          TextBoxAttributes = textBoxAttributes
          TextBlockAttributes = textBlockAttributes }

let textBox (record: TextBoxRecord<_>) dispatch =
    DockPanel.create [
        DockPanel.children [
            stackPanel [
                TextBlock.create [
                    TextBlock.dock Dock.Top
                    TextBlock.fontSize 32.0
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.text record.Label
                    if record.TextBlockAttributes.IsSome then yield! record.TextBlockAttributes.Value
                ]
                TextBox.create [
                    TextBox.text record.Text
                    TextBox.onTextChanged (record.Msg >> dispatch)
                    if record.TextBoxAttributes.IsSome then yield! record.TextBoxAttributes.Value
                ]
            ]
        ]
    ]
    |> Helpers.generalize
