// http://www.ietf.org/rfc/rfc4627.txt

// An attempt at making a JSON parser on my own for practice.

// Well, I got the string literal wrong. Nevermind.

#r "../packages/FParsec.1.0.2/lib/net40-client/FParsecCS.dll"
#r "../packages/FParsec.1.0.2/lib/net40-client/FParsec.dll"
open FParsec

type Json = JString of string
          | JNumber of float
          | JBool   of bool
          | JNull
          | JList   of Json list
          | JObject of Map<string, Json>

let json_ex1 = 
    let t =
        """   
{
      "Image": {
          "Whacky": "hello \"I am testing the string\"435 \n Line 2\tqwe\rasd",
          "Width":  800,
          "Height": 600,
          "Title":  "View from 15th Floor",
          "Thumbnail": {
              "Url":    "http://www.example.com/image/481989943",
              "Height": 125,
              "Width":  "100"
          },
          "IDs": [116, 943, 234, 38793]
        }
   }
""" t.Trim()

let json_ex2 =
    let t = """
       [
      {
         "precision": "zip",
         "Latitude":  37.7668,
         "Longitude": -122.3959,
         "Address":   "",
         "City":      "SAN FRANCISCO",
         "State":     "CA",
         "Zip":       "94107",
         "Country":   "US"
      },
      {
         "precision": "zip",
         "Latitude":  37.371991,
         "Longitude": -122.026020,
         "Address":   "",
         "City":      "SUNNYVALE",
         "State":     "CA",
         "Zip":       "94085",
         "Country":   "US"
      }
   ]
""" t.Trim()

let stringLiteral =
    let normalCharSnippet = manySatisfy (fun c -> c <> '\\' && c <> '"')
    let unescape = function
                     | 'n' -> "\n"
                     | 'r' -> "\r"
                     | 't' -> "\t"
                     | c   -> string c
    let escapedChar = pstring "\\" >>. (anyOf "\\nrt\"" |>> unescape)
    between (pstring "\"") (pstring "\"")
            (stringsSepBy normalCharSnippet escapedChar)

let string_ws = stringLiteral .>> spaces
let json_string_ws = string_ws |>> (fun x -> JString x)
let json_number_ws = pfloat .>> spaces |>> (fun x -> JNumber x)
let json_bool = (stringReturn "true" (JBool true)) <|> (stringReturn "false" (JBool false))
let json_bool_ws = json_bool .>> spaces
let json_null_ws = (stringReturn "null" JNull) .>> spaces

let json_object, json_object_impl = createParserForwardedToRef()
let json_array, json_array_impl = createParserForwardedToRef()

let json_expr = choice [|json_string_ws; json_number_ws; json_bool_ws; json_null_ws; json_object; json_array|]
json_array_impl := 
    (pstring "[" .>> spaces) 
    >>. sepBy json_expr (pstring "," .>> spaces) 
    .>> (pstring "]" .>> spaces) 
    |>> (fun x -> JList x)
json_object_impl := 
    (pstring "{" .>> spaces) >>. 
    sepBy (pipe3 string_ws (pstring ":" .>> spaces) json_expr (fun a b c -> (a,c))) (pstring "," .>> spaces) 
    .>> (pstring "}" .>> spaces)
    |>> (fun x -> JObject <| Map(x))

let json_start = choice [|json_array; json_object|]

run json_start json_ex2