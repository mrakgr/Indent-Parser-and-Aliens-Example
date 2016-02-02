// I will never give up.
// Human level AI - No Problem.
// Indentation Parser - Holy Shit!

// Edit: Done. Uses no user state and no backtracking.

#r "../packages/FParsec.1.0.2/lib/net40-client/FParsecCS.dll"
#r "../packages/FParsec.1.0.2/lib/net40-client/FParsec.dll"

open FParsec

type IndentTypes =
| Opening of string
| Statement of string
| Statements of ResizeArray<IndentTypes>

let skipIndent (stream: CharStream<_>) =
    let indent=stream.SkipNewlineThenWhitespace(4,false)
    if indent = -1 then Reply(Error, expectedString "new line")
    else
        Reply(Ok)

type Expectation =
| SAME = 0
| UP = 1

let manyIndents (same: Parser<IndentTypes,_>) (up: Parser<IndentTypes,_>) (stream: CharStream<_>) =
    let getResultExp() =
        let r1 = same stream
        if r1.Status = Ok then r1, Expectation.SAME else up stream, Expectation.UP

    let rec loop depth =
        let results = ResizeArray()
        let indent = stream.Column-1L |> int
        let mutable result, expectation = getResultExp()
        let mutable next_indent = stream.SkipNewlineThenWhitespace(4,false)
        while result.Status = Ok do
            results.Add(result.Result)
            match expectation with
            | Expectation.SAME ->
                match indent with
                | _ when indent = next_indent -> 
                let result', expectation' = getResultExp()
                result <- result'; expectation <- expectation'
                next_indent <- stream.SkipNewlineThenWhitespace(4,false)
                | _ -> result <- Reply(Error, expected "same indent")
            | (*Expectation.UP*) _ ->
                match indent with
                | _ when indent < next_indent -> 
                result <- loop <| depth+1; expectation <- Expectation.SAME
                next_indent <- stream.Column-1L |> int
                | _ -> result <- Reply(Error, expected "up indent")
        let er = if depth > 0 then Ok else if not stream.IsEndOfStream && depth = 0 then Error else Ok
        Reply(er, Statements results, result.Error)
    loop 0

let statement = pstring "s" .>> skipRestOfLine false |>> (fun x -> Statement x)
let opening = pstring "o" .>> skipRestOfLine false |>> (fun x -> Opening x)

let parser = skipIndent >>. manyIndents statement opening

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


let t = run parser ex1
let t',y,z = 
    match t with
    | Success(t,y,z) -> t,y,z
    | Failure(t,y,z) -> 
        printfn "%A" t
        failwith "failure"

let rec printallt indent = 
    function
    | Statement x -> printfn ("%sStatement %A") indent x
    | Statements x -> 
        printfn "%sStatements" indent
        for i in x do printallt (indent+"    ") i
    | Opening x -> printfn "%sOpening %A" indent x

printallt "" t'
