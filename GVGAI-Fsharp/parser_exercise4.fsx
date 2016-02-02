// I pretty much read the Fparsec documentation from cover to cover now.
// I still do not understand the indentation parser, so I will try to rebuild it by hand in order to attain that understanding.

#r "../packages/FParsec.1.0.2/lib/net40-client/FParsecCS.dll"
#r "../packages/FParsec.1.0.2/lib/net40-client/FParsec.dll"

open FParsec

type UserState =
    struct
    val indent: int
    end
    new indent = {indent=indent}
    static member Default = new UserState(-1)

//let isBlank c = if c = ' ' then true else false
//let skipBlanks = skipManySatisfy isBlank

let skipIndent (stream: CharStream<UserState>) =
    let indent=stream.SkipNewlineThenWhitespace(4,false)
    if indent = -1 then Reply(Error, expectedString "new line")
    else
        stream.UserState <- UserState(indent)
        Reply(Ok)

let getIndent (stream: CharStream<UserState>) =
    let indent=stream.SkipNewlineThenWhitespace(4,false)
    if indent = -1 then Reply(Error, expectedString "new line")
    else
        stream.UserState <- UserState(indent)
        Reply(indent)

let getUpIndent (stream: CharStream<UserState>) =
    let indent=stream.SkipNewlineThenWhitespace(4,false)
    if indent = -1 then Reply(Error, expectedString "new line")
    else
        if indent > stream.UserState.indent then
            stream.UserState <- UserState(indent)
            Reply(indent)
        else 
            stream.UserState <- UserState(indent)
            Reply(Error, expected "increased indent")

let getSameIndent (stream: CharStream<UserState>) =
    let indent=stream.SkipNewlineThenWhitespace(4,false)
    if indent = -1 then Reply(Error, expectedString "new line")
    else
        if indent = stream.UserState.indent || stream.IsEndOfStream then // Without the end of stream check it throws an error inside the many statement.
            stream.UserState <- UserState(indent)
            Reply(indent)
        else 
            stream.UserState <- UserState(indent)
            Reply(Error, expected <| sprintf "same indent (%i), instead got %i" stream.UserState.indent indent)

let manyIndents (up: Parser<_,_>) (same: Parser<_,_>) (stream: CharStream<UserState>) =
    let state = System.Collections.Generic.Stack()
    state.Push(stream.UserState)
    let results = ResizeArray()
    let mutable result = p stream
    while result.Status = Ok do
        results.Add(result.Result)
        result <- p stream
        if result.Status = Ok then
            printfn "ident=%i" stream.UserState.indent
            state.Push(stream.UserState)
        else if state.Count > 0 then
            let state = state.Pop()
            printfn "pop_state=%i" state.indent
            stream.UserState <- state
            result.Status <- Ok
    printfn "status=%A" result.Status
    Reply(results)

let statement = skipString "s" >>. skipRestOfLine false
let opening = skipString "o" >>. skipRestOfLine false

let parser = getIndent >>. manyIndents ((statement >>. getSameIndent) <|> (opening >>. getUpIndent))

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
match t with | Success(x: ResizeArray<int>,_,_) -> x |> Seq.iter (printfn "%i")