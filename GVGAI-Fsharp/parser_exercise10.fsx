// A bit better, but I am not there yet.

// Edit: Ah fuck it. #9 is really the best I can do. I'll adjust it a little so it returns arrays, but that is it.

#r "../packages/FParsec.1.0.2/lib/net40-client/FParsecCS.dll"
#r "../packages/FParsec.1.0.2/lib/net40-client/FParsec.dll"

open FParsec

type IndentTypes =
| Opening of string * IndentTypes
| Statement of string
| Statements of IndentTypes []

let skipIndent (stream: CharStream<_>) =
    let indent=stream.SkipNewlineThenWhitespace(4,false)
    if indent = -1 then Reply(Error, expectedString "new line")
    else
        Reply(Ok)

type Expectation =
| SAME = 0
| UP = 1

type UserState =
    struct
    val indent: int
    val depth: int
    end
    new (indent,depth) = {indent=indent;depth=depth}
    static member Default = new UserState(-1,-1)


// Hmmm, I can't figure out what the example indent parser is supposed to be doing.

// But after all this practice, it does not seems too difficult to implement many1Indents.
// What I have to take into consideration is two different scenarios.

// 1: When many1Indents is calls at the beginning of a statement.
// 2: When it is called at a newline, which would be the situation after it has been called recursively after parsing the opening.

// I just have to take those two situations into account when making the function and it will be easy.
// It is amazing how much the Fparsec example confused me. I still have no idea what it is supposed to be doing.

let many1Indents (p: Parser<IndentTypes,_>) (stream: CharStream<UserState>) : Reply<IndentTypes []>=
    let getIndent() =
        let indent = stream.SkipNewlineThenWhitespace(4,false) // Returns -1 if not at newline.
        if indent = -1 then stream.Column-1L |> int else indent // If at newline skip it, else set indent to column.

    let indent = getIndent()// Gets the current indent
    let depth = stream.UserState.depth+1

    let results = ResizeArray()
    let mutable result = p stream
    //printfn "result=%A" result.Result
    let mutable indent2 = -1

    if indent > stream.UserState.indent then
        stream.UserState <- UserState(indent,depth) // Sets the userstate to the current indent
        //printfn "indent=%i depth=%i" indent depth
        while 
            (
            results.Add(result.Result)
            indent2 <- getIndent()
            result <- p stream
            //printfn "result=%A" result.Result
            if stream.UserState.indent <> indent then stream.UserState <- UserState(indent,depth)
            result.Status = Ok && indent = indent2) do ()

        printfn "result=%A" result.Result

        //if indent2 > indent || (indent2 < indent && depth = 0 && not stream.IsEndOfStream) then Reply(Error, messageError "wrong indent")
        if result.Status = Ok || (stream.IsEndOfStream && results.Count > 0) then // Without results.count > 0 it will not read after an open.
            if (indent2 < indent && depth > 0) || stream.IsEndOfStream then
                Reply(results.ToArray())
            else Reply(Error, messageError "wrong indent")
        else
            Reply(result.Status, result.Error)
    else Reply(Error, expected "up indent")

let indented_statements, indented_statements_ref = createParserForwardedToRef()

let statement = pstring "s" .>> skipRestOfLine false |>> (fun x -> Statement x)
let opening = pipe2 (pstring "o" .>> skipRestOfLine false) indented_statements (fun a b -> Opening(a,Statements b))

indented_statements_ref := many1Indents (statement <|> opening)

let parser = indented_statements

let ex1 = 
    """
 s
 o
   s
 s
"""


let t = runParserOnString parser UserState.Default "indentation test" ex1


