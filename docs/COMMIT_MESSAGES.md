# Commit Messages
Commit messages in `master` must adhere to the [Conventional Commits specification]:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

Commits in branches other than `master` may but need not adhere.

When writing commit messages, please note the following details:
- Begin the description with a lowercase letter
- Do not include internal ticket or issue numbers in the description
  - Include them in the optional body or optional footer instead
- Use lowercase for the type and optional scope
- In the optional scope, use hyphens to separate words
- In general, use one of `fix`, `feat`, or `chore` for the type

[Conventional Commits specification]: https://www.conventionalcommits.org/en/v1.0.0/#specification