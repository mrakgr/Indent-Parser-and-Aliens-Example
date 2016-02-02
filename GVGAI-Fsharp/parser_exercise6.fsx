// Try #3. I suck at this. Hopefully the recursive idea will work, now that I know that I have to initialize it properly.

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
            let t = stream.UserState.indent
            stream.UserState <- UserState(indent)
            Reply(Error, expected <| sprintf "same indent(%i), instead got %i" t indent)

let rec manyIndents (p: Parser<IndentTypes,_>) (stream: CharStream<UserState>) =
    let results = ResizeArray()
    let userstate = stream.UserState
    let mutable result = p stream
    let mutable userstate2 = stream.UserState
    printfn "userstate.indent=%A" userstate.indent
    while result.Status = Ok do
        printfn "I am in the loop. %A" result.Result
        results.Add(result.Result)
        if userstate2 > userstate then 
             result <- manyIndents p stream
        else result <- p stream
        userstate2 <- stream.UserState
    Reply(Statements results)

let statement = pstring "s" .>> skipRestOfLine false |>> (fun x -> Statement x)
let opening = pstring "o" .>> skipRestOfLine false |>> (fun x -> Opening <| ResizeArray([|Statement x|]))

let parser = getIndent >>. manyIndents ((statement .>>? getSameIndent) <|> (opening .>>? getUpIndent))

let ex1 = 
    """
s
o
    s
"""


let t = runParserOnString parser UserState.Default "indentation test" ex1
let t' = match t with
| Success(t,_,_) -> t

let rec printallt = 
    function
    | Statement x -> printfn "%A" x
    | Statements x -> for i in x do printallt i
    | Opening x -> printfn "%A" x

printallt t'