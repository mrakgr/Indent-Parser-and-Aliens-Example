// I am not done yet with the parser. I need to figure out how to make it recursive and generic.

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
    end
    new indent = {indent=indent}
    static member Default = new UserState(-1)


// Hmmm, I can't figure out what the example indent parser is supposed to be doing.

// But after all this practice, it does not seems too difficult to implement many1Indents.
// What I have to take into consideration is two different scenarios.

// 1: When many1Indents is calls at the beginning of a statement.
// 2: When it is called at a newline, which would be the situation after it has been called recursively after parsing the opening.

// I just have to take those two situations into account when making the function and it will be easy.
// It is amazing how much the Fparsec example confused me. I still have no idea what it is supposed to be doing.

let many1Indents (p: Parser<IndentTypes,_>) (stream: CharStream<UserState>) =
    let getIndent() =
        let indent = stream.SkipNewlineThenWhitespace(4,false) // Returns -1 if not at newline.
        if indent = -1 then stream.Column-1L |> int else indent // If at newline skip it, else set indent to column.

    let indent = getIndent()// Gets the current indent

    if stream.UserState.indent < indent then // Go up an indent level
        stream.UserState <- UserState(indent) // Sets the userstate to the current indent

        let results = ResizeArray()
        let mutable result = p stream
        let mutable indent2 = getIndent() // As it is possible for the many1Indents to be called recursively, a call to this function is needed.
        if result.Status <> Ok || indent2 <> indent then result 
        else 
            while 
                (
                results.Add(result.Result)
                if indent = indent2 then
                    if stream.UserState.indent <> indent then stream.UserState <- UserState(indent) // Sets the userstate.indent to the current level in case it has been modified.
                    result <- p stream
                    indent2 <- getIndent()
                    result.Status = Ok
                else false)
                do ()
            Reply(Statements <| results.ToArray())
    else Reply(Error, expected "up indent")

let (indented_statements: Parser<IndentTypes, UserState>), indented_statements_ref = createParserForwardedToRef()

let statement = pstring "s" .>> skipRestOfLine false |>> (fun x -> Statement x)
let opening = pipe2 (pstring "o" .>> skipRestOfLine false) indented_statements (fun a b -> Opening(a,b))

indented_statements_ref := many1Indents (statement <|> opening)

let parser = skipIndent >>. indented_statements

let ex1 = 
    """
s
o
  s
  o
   s
   s
  s
o
    s
    s
s
"""


let t = runParserOnString parser UserState.Default "indentation test" ex1
