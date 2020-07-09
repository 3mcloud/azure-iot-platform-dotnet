# Pull Requests
When preparing pull requests:
- Feel free to write commit messages in whatever style you please
- Feel free to use as many commits as necessary to represent a changeset
- In general*, limit pull requests to a single changeset

When merging pull requests:
- If there is a single commit, so long as it adheres to [the spec], click "Rebase and merge"
- If there is a single commit that doesn't adhere, or if there are multiple commits*, click "Squash and merge" and write a new commit message that adheres to [the spec]

\* It would be fine to have a pull request containing multiple changesets, but only if each changeset comprised a single commit whose message adheres to [the spec]

[Conventional Commits specification]: https://www.conventionalcommits.org/en/v1.0.0/#specification
[the spec]: https://www.conventionalcommits.org/en/v1.0.0/#specification