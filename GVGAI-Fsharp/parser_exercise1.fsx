// It occurs to me that I am completely lost not just in regards to how to parse indentation, but in general.

// I'll do a bunch of exercises starting from here to get myself up to speed.
// For starters, in this file I will try to immitate the Trim function except for multiple lines using Fparsec.

// I did a lot of thought regarding how to implement game effects, but not so much for this.

#r "../packages/FParsec.1.0.2/lib/net40-client/FParsecCS.dll"
#r "../packages/FParsec.1.0.2/lib/net40-client/FParsec.dll"
open FParsec

let test p str =
    match run p str with
    | Success(result, _, _)   -> printfn "Success: %A" result
    | Failure(errorMsg, _, _) -> printfn "Failure: %s" errorMsg

let ws = spaces
let str s = pstring s
let letter c = isLetter c || isDigit c

let ex1 = "  letter   "

let r = ws >>. many1Satisfy letter

test r ex1

let keep_spaces = isAnyOf [|' '|]
let space_satisfy = many1Satisfy keep_spaces

let r' = ws >>. many (many1Satisfy letter .>>. space_satisfy)

let ex2 = "   letter1   letter2  "

test r' ex2

let letters_or_whitespaces : Parser<_,unit>= (fun c -> isAsciiLetter c || isAnyOf [|' '|] c || isDigit c) |> manySatisfy |>> (fun x -> x.TrimEnd())
let r'' = sepBy (ws >>. letters_or_whitespaces) (anyOf [|'\n'|])

test r'' ex2

let ex3 = """   asd  qwe   
   123 456   
   hello world   """

let whitespace = anyOf [|' '|] |> manyChars |>> (fun x -> x.Length)
let neol = noneOf [|'\n'|] |> manyChars

let r''' = sepBy (whitespace .>>. neol) (anyOf [|'\n'|])

test r''' ex3