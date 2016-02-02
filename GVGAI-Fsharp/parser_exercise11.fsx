// I am just imitating a genetic algorithm at this point. No way I can handle those branches.
// Let me touch up #9 a bit so it returns arrays and then I will call it a day.

// Edit: No, actually this is wrong again.
// Somehow this parser is turning far far harder than I thought it would.
// Just now it occured to me that if I was imitating a genetic algorithm that I should have
// created unit tests ahead of time instead of just pumping up my effort.

// The unit tests would not have been as effective for neural nets as they would have been for this here.

// No, let me make this my last attempt. I'll just make a tail recursive loop inside.

// Edit: This works, I am sure of it. No need to fit things with unit tests.
// I finally did it and this form is quite a bit more pleasing than the example parser I found on the net.

// I should have done it in a tail recursive manner from the start instead of mimicking the form
// of a parser that I found on the net. Oh, well.

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

