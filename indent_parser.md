In this solution, the `parser_exercise#.fsx` files contain my past attempts at reconstructing the [Fparsec](http://www.quanttec.com/fparsec/) [indentation parser](https://gist.github.com/impworks/3772212) which I will definitely need to parse VGDL grammars.

```F#

#r "../packages/FParsec.1.0.2/lib/net40-client/FParsecCS.dll"
#r "../packages/FParsec.1.0.2/lib/net40-client/FParsec.dll"

open FParsec

type IndentTypes =
| Opening of string * IndentTypes
| Statement of string
| Statements of IndentTypes []

type Expectation =
| SAME = 0
| UP = 1

type UserState =
    struct
    val indent: int
    end
    new indent = {indent=indent}
    static member Default = new UserState(-1)


let many1Indents (p: Parser<IndentTypes,_>) (stream: CharStream<UserState>) : Reply<IndentTypes []> =
    let getIndent() =
        let indent = stream.SkipNewlineThenWhitespace(4,false) // Returns -1 if not at newline.
        if indent = -1 then stream.Column-1L |> int else indent // If at newline skip it, else set indent to column.

    let indent = getIndent()// Gets the current indent
    let result = p stream

    // As expected, it is much easier to do it like this. In F# when one needs to break out of the loop, one does tail recursive calls.
    // This version does not even use mutable state inside the loop apart from the UserState.

    // Damn, I am finally happy with this. It took me like a whole week to converge on this form and maybe two 
    // if one counts when I first decided to work on VGDL.
    let rec loop (acc: IndentTypes list) = 
        if stream.UserState.indent <> indent then stream.UserState <- UserState(indent)
        let indent2 = getIndent()

        if stream.IsEndOfStream then Reply (acc |> List.rev |> List.toArray)
        else if indent2 = indent then
            let result = p stream
            if result.Status = Ok 
            then loop <| result.Result::acc else Reply(result.Status, result.Error)
        else if indent2 > indent then Reply(Error, expected "same indent")
        else Reply(acc |> List.rev |> List.toArray) // indent2 < indent

    if result.Status = Ok && indent > stream.UserState.indent then loop [result.Result]
    else if result.Status <> Ok then Reply(result.Status, result.Error)
    else Reply(Error,expected "up indent")


let indented_statements, indented_statements_ref = createParserForwardedToRef()

let statement = pstring "s" .>> skipRestOfLine false |>> (fun x -> Statement x)
let opening = pipe2 (pstring "o" .>> skipRestOfLine false) indented_statements (fun a b -> Opening(a,Statements b))

indented_statements_ref := many1Indents (statement <|> opening)

let parser = indented_statements .>> (eof <?> "wrong indent") 

let ex1 = 
    """
 s
 o
  s
  o
   s
   s
  o
    s
    s
 
"""

let t = runParserOnString parser UserState.Default "indentation test" ex1

```

Comparing it with the original indent parser by Fparsec's author, I think the above code is much more readable in regards to the control flow. The technique used is to code the loop in a separate function. In functional languages as there are no return or break statements, when one want to emulate that functionality one uses a tail recursive function with conditionals instead. It should have been the first thing to try instead of last.

The above parser is relatively simple, it just returns an error if it encounters higher indentation inside the loop or if the parser function call (`let result = p stream`) fails. It uses Userstate to keep track of the indentation level from call to call.

It is actually possible to do it without using state, as shown in `parser_exercise7.fsx` and I think that parser would be more efficient for VGDL given the structure of the formal language it uses.