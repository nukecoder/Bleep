// Exercise in creating an Elmish.WPF applciation
// adapted from Racket example: https://dev.to/goober99/learn-racket-by-example-gui-programming-3epm
open System
open Elmish
open Elmish.WPF
open BleepViews
open System.Threading
open System.Windows

// Globals
// scale used by slider
let minPosition = 0
let maxPosition = 20000

// rage of frequencies used by System.Console.Beep1
let minFrequency = 37.0
let maxFrequency = 32767.0

// logarithmic scale for frequency (so middle A [440Hz] falls about in the middle)
let minFreq = minFrequency |> float |> Math.Log
let maxFreq = maxFrequency |> float |> Math.Log
let frequencyScale = (maxFreq - minFreq) / float (maxPosition - minPosition)

// Convert the slider position to frequency
let sliderPosToFreq pos =
    Math.Exp (minFreq + frequencyScale * (double pos))
// Convert the frequency to slider position
let freqToSliderPos freq =
    ((Math.Log (double freq)) - (double minFreq)) / (frequencyScale + (double minPosition)) |> Math.Round

// Validation Fucntions
let requireNotEmpty s =
  if String.IsNullOrEmpty s then Error "This field is required" else Ok s

let parseInt (s:string) =
    match Int32.TryParse s with
    | true,i when i <= 0 -> Error "Please enter a positive integer."
    | true,i -> Ok i
    | false,_ -> Error "Please enter a valid integer."

let validateDuration =
  requireNotEmpty
  >> Result.bind parseInt

// Some Notes!
let notes =
    [ "A", 440.00
    ; "B", 493.88
    ; "C", 261.33
    ; "D", 293.66
    ; "E", 329.63
    ; "F", 349.23
    ; "G", 392.00 // the sources page lists this as 292, but their source lists it as 392.
    ] |> Map.ofList


// MODEL
type Model =
  { Frequency : double
    Duration : int 
    DurationRawValue : string
    SelectedNote : string
    PlayInProgress : bool
    }

let initialNote = "A"
let init () = { Frequency = Map.find initialNote notes
                Duration = 2000
                DurationRawValue = "2000"
                SelectedNote = initialNote
                PlayInProgress = false},
                []

type CmdMsg =
    | Play of double * int

type Msg =
    | Lower
    | Raise
    | RequestPlay
    | SetFrequency of double
    | SetDuration of int
    | PlayFinished
    | DurationFieldUpdate of string
    | FrequencyFieldUpdate of string
    | SelectNewNote of string
    | DoNothing

let updateFrequency f m =
    {m with Frequency = match f with
                        | f when f > maxFrequency -> maxFrequency
                        | f when f < minFrequency -> minFrequency
                        | _ -> f }


// UPDATE
let update msg m =
    match msg with
    | Lower -> updateFrequency (m.Frequency / 2.0) m, []
    | Raise -> updateFrequency (m.Frequency * 2.0) m, []
    | RequestPlay -> {m with PlayInProgress = true }, [Play (m.Frequency, m.Duration)]
    | SetFrequency f -> updateFrequency f m, []
    | SetDuration i -> {m with Duration = i}, []
    | PlayFinished -> {m with PlayInProgress = false }, []
    | DurationFieldUpdate s -> match parseInt s with
                               | Ok i -> {m with DurationRawValue = s; Duration = i}, []
                               | _ -> {m with DurationRawValue = s}, []
    | FrequencyFieldUpdate s -> match System.Double.TryParse s with
                                | true,d -> updateFrequency d m,[]
                                | _ -> m,[]
    | SelectNewNote n -> { m with SelectedNote = n} |> updateFrequency (Map.find n notes), []
    | DoNothing -> {m with PlayInProgress = false }, []


// VIEW
let bindings () =
  [
    "Frequency" |> Binding.twoWay(
      (fun m -> string m.Frequency),
      (fun newVal m -> newVal |> FrequencyFieldUpdate))
    "Lower" |> Binding.cmd (fun m -> Lower)
    "Raise" |> Binding.cmd (fun m -> Raise)
    "Play" |> Binding.cmdIf (
        (fun m -> RequestPlay),
        (fun m -> not m.PlayInProgress)
    )
    "Duration" |> Binding.twoWayValidate (
      (fun m -> string m.DurationRawValue),
      DurationFieldUpdate,
      (fun m -> validateDuration m.DurationRawValue)
    )
    "SliderPosition" |> Binding.twoWay(
      (fun m -> (float (freqToSliderPos m.Frequency))),
      (fun newVal m -> sliderPosToFreq (double newVal) |> SetFrequency))
    "Notes" |> Binding.oneWay (fun m -> notes)
    "SelectedNote" |> Binding.twoWay(
        (fun m -> m.SelectedNote),
        (fun newNote m -> SelectNewNote newNote))
      ]


// COMMANDS
let play (freq,dur) = 
    Application.Current.Dispatcher.Invoke(fun () ->
      async {
        do! Async.SwitchToNewThread ()
        Console.Beep(freq,dur)
      }
    )

let toCmd = function
    | Play(f,d) -> Cmd.OfAsync.perform play (int f,d) (fun _ -> PlayFinished)

[<EntryPoint; STAThread>]
let main argv = 
    Program.mkProgramWpfWithCmdMsg
        init update bindings toCmd 
        |> Elmish.Program.withConsoleTrace
        |> Program.runWindowWithConfig
          { ElmConfig.Default with LogConsole = true; Measure = true }
          (MainWindow())
