// I pretty much read the Fparsec documentation from cover to cover now.
// I still do not understand the indentation parser, so I will try to rebuild it by hand in order to attain that understanding.

// #4 sucked, so I'll chop and then rebuild again.

#r "../packages/FParsec.1.0.2/lib/net40-client/FParsecCS.dll"
#r "../packages/FParsec.1.0.2/lib/net40-client/FParsec.dll"

open FParsec

type UserState =
    struct
    val indent: int
    end
    new indent = {indent=indent}
    static member Default = new UserState(-1)

type IndentTypes =
| Opening of ResizeArray<IndentTypes>
| Statement of string
| Statements of ResizeArray<IndentTypes>

let manyIndents (up: Parser<IndentTypes,_>) (same: Parser<IndentTypes,_>) (stream: CharStream<UserState>) =
    let indent=stream.SkipNewlineThenWhitespace(4,false)
    let rec loop (results: ResizeArray<_>) indent =
        let mutable result = same stream
        if result.Status = Error then result <- up stream
        while result.Status = Ok do
            results.Add(result.Result)
            stream.UserState <- UserState(indent) // Restores the stream to the correct indent in case it has been modified.
            let indent_new=stream.SkipNewlineThenWhitespace(4,false)
            printfn "I am in main. Indent new is %i" indent_new
            match indent_new with
            | _ when indent_new > indent -> result <- loop <| ResizeArray() <| indent_new // Calls the up parser
            | indent -> result <- same stream // Calls the same parser
            | _ -> result.Status <- Error // Breaks the loop
        Reply(Statements results)
    if indent = -1 then Reply(Error, expectedString "new line")
    else loop <| ResizeArray() <| indent
        

let statement = pstring "s" .>> skipRestOfLine false |>> (fun x -> Statement x)
let opening = pstring "o" .>> skipRestOfLine false |>> (fun x -> Opening <| ResizeArray([|Statement x|]))

let parser = manyIndents opening statement

let ex1 = 
    """
s
o
    s
    s
    o
        s
        s
    s
"""


let t = runParserOnString parser UserState.Default "indentation test" ex1
//match t with | Success(x,_,_) -> x |> Seq.iter (printfn "%A")
