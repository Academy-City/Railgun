# ⚡ The Railgun Programming Language

The main repository for Railgun, a modern lisp. It builds upon the .NET ecosystem.

## Examples

```railgun
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
