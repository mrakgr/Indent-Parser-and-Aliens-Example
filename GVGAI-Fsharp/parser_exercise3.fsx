// This one is straight copied from the manual.

#r "../packages/FParsec.1.0.2/lib/net40-client/FParsecCS.dll"
#r "../packages/FParsec.1.0.2/lib/net40-client/FParsec.dll"
open FParsec

type Element = Text of string
             | Bold of Element list
             | Italic of Element list
             | Url of string * Element list
             | Quote of char * Element list

type UserState =
    {InLink: bool
     QuoteStack: char list}
    with
       static member Default = {InLink = false; QuoteStack = []}

let ws    = spaces
let ws1   = spaces1
let str s = pstring s

let elements, elementsR = createParserForwardedToRef()

let text = many1Satisfy (isNoneOf "<>'\"\\") |>> Text
let escape = str "\\" >>. (anyChar |>> (string >> Text))

let quote (q: char) =
  let pq = str (string q)

  let pushQuote =
      updateUserState (fun us -> {us with QuoteStack = q::us.QuoteStack})

  let popQuote =
      updateUserState (fun us -> {us with QuoteStack = List.tail us.QuoteStack})

  let isNotInQuote =
      userStateSatisfies (fun us -> match us.QuoteStack with
                                    | c::_ when c = q -> false
                                    | _ -> true)

  isNotInQuote >>. between pq pq
                           (between pushQuote popQuote
                                    (elements |>> fun ps -> Quote(q, ps)))

// helper functions for defining tags

let tagOpenBegin tag =
    str ("<" + tag)
    >>? nextCharSatisfiesNot isLetter // make sure tag name is complete
    <?> "<" + tag + "> tag"

let tagOpen tag = tagOpenBegin tag >>. str ">"
let tagClose tag = str ("</" + tag + ">")

let tag t p f =
    between (tagOpen t) (tagClose t)
            (p |>> f)

let attributeValue =
    ws >>. str "=" >>. ws
    >>. between (str "\"") (str "\"")
                (manySatisfy (isNoneOf "\n\""))

let attribute s = str s >>. attributeValue

let nonNestedTag tag pAttributesAndClose pBody f
                 isInTag setInTag setNotInTag =
    tagOpenBegin tag
    >>. ((fun stream ->
            if not (isInTag stream.UserState) then
                stream.UserState <- setInTag stream.UserState
                Reply(())
            else // generate error at start of tag
                stream.Skip(-tag.Length - 1)
                Reply(FatalError,
                      messageError ("Nested <" + tag + "> tags are not allowed.")))
         >>. pipe2 pAttributesAndClose pBody f
             .>> (tagClose tag >>. updateUserState setNotInTag))

// the tags

let bold   = tag "b" elements Bold
let italic = tag "i" elements Italic

let url = nonNestedTag "a" (ws >>. attribute "href" .>> (ws >>. str ">"))
                       elements
                       (fun url phrases -> Url(url, phrases))
                       (fun us -> us.InLink)
                       (fun us -> {us with InLink = true})
                       (fun us -> {us with InLink = false})


let element = choice [text
                      escape
                      quote '\''
                      quote '\"'
                      bold
                      italic
                      url]

do elementsR:= many element

let document = elements .>> eof
