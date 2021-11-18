# ⚡ The Railgun Programming Language

**Warning: Railgun is currently in alpha, and none of this is production-ready. Expect syntaxes to change drastically from what is implemented so far.**

The main repository for Railgun, a modern take on Lisp with a cleaner syntax and eventually, a homoiconic type system. Railgun is a Lisp with access to the full power of the Lisp ecosystem.

## Installation
See [Nightly Releases.](https://github.com/Academy-City/Railgun/releases/tag/nightly)

Railgun can also be used as an embedded Library: A NuGet package will be published soon.

## Expressions
Railgun has two major syntactic constructs: Atoms and Composites.

```rg
; comments are written with a ';'
42 ; integers
"bob" ; strings
(print "hello, world") ; a sequence, with the callable as the first, and arguments as rest
```

### Sweet-Expressions
Railgun supports sweet-expressions, provided that you use the .rgx or .⚡ file extension. This allows the outermost parentheses to be inferred.

For example, a factorial without sweet-expressions:

```rg
(let-fn factorial (n)
    ; A simple recursive factorial function.
    (if (<= n 1)
        1
        (* n (factorial (- n 1)))))
```

can be written as this, with sweet-expressions enabled:
```rg
let-fn factorial (n)
    ; A simple recursive factorial function.
    if (<= n 1)
        1
        * n (factorial (- n 1))
```

## Examples

```rg
# a struct defining a person.
def struct Person
    name
    age

; List of students.
let students [
    (Person "Misaka Mikoto" 14)
    (Person "Shirai Kuroko" 13)
]

let-fn displayStudent (student)
    print (str/fmt "Name: {0}, Age: {1}" student.name student.age)

foreach s students
    displayStudent s
```

## Features to add

- Import C# modules
- use Railgun itself more to implement more function, not C#.
- Better error messages
- Gradual Typing and Typechecker
- async/await?
- Compile things down to IL
- Bootstrap the compiler
- More tests
- support other number types
