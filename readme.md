# ⚡ The Railgun Programming Language

**Warning: Railgun is currently in alpha, and none of this is production-ready. Expect syntaxes to change drastically from what is implemented so far.**

The main repository for Railgun, a modern take on Lisp with a cleaner syntax and eventually, a homoiconic type system. Railgun is a Lisp with access to the full power of the Lisp ecosystem.

## Expressions
```ls
# comments are written with a '#'
42 # integers
"bob" # strings
(print "hello, world") # a sequence, with the callable as the first, and arguments as rest
```

Railgun supports sweet-expressions, provided that you use the .rgx or .⚡ file extension. This allows the outermost parentheses to be inferred.

For example, a factorial without sweet-expressions:

```ls
(let-fn factorial (n)
    # A simple recursive factorial function.
    (if (<= n 1)
        1
        (* n (factorial (- n 1)))))
```

can be written as this, with sweet-expressions enabled:
```ls
let-fn factorial (n)
    # A simple recursive factorial function.
    if (<= n 1)
        1
        * n (factorial (- n 1))
```

## Examples

```ls
# a struct defining a person.
def struct Person
    name
    age

# List of students.
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
