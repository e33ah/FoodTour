---
name: update-changelog
description: Update the FoodTour changelog from commits since the last Git tag. Use this when asked to update the changelog, prepare release notes, or add the next release entry to CHANGELOG.md.
---

Use this skill for changelog updates in this repository.

## Target file

Update this file only unless the user explicitly asks for something else:

- `_Dependencies\Publish_Support\CHANGELOG.md`

This changelog follows Keep a Changelog headings and uses release headings like:

- `## [1.0.2] - 2026-04-28`

Release links at the bottom use tags with a leading `v`, for example:

- `[1.0.2]: https://github.com/e33ah/FoodTour/releases/tag/v1.0.2`

## Required workflow

1. Find the most recent Git tag reachable from `HEAD`.
2. Review **all commits since that tag** before making any edits.
3. Read enough commit detail to produce a short review for the user before editing the changelog.
4. If you find anything of significant concern, stop and ask the user whether to continue.
5. If there are no significant concerns, update the changelog with a new release entry dated with the current date.

## Versioning rules

- Assume the next version is a **patch release** unless the user explicitly says otherwise.
- If the latest tag is prefixed with `v`, strip the `v` before writing the changelog heading.
- For a patch release, increment the third semantic version component.
- Examples:
  - `v1.0.2` -> `1.0.3`
  - `1.4.9` -> `1.4.10`
- If the user explicitly asks for a minor or major release, follow their instruction instead of the default patch bump.

## Review before editing

Before changing the changelog, send the user a concise review of the commits since the last tag. The review should:

- summarise the main themes of the changes
- call out notable user-facing, infrastructure, or test-impacting work
- mention any migrations, breaking changes, removals, or risk

Treat the following as **significant concerns** that require user confirmation before continuing:

- likely breaking changes
- database migrations or data-shape changes that may warrant a higher version bump
- security-sensitive changes
- unclear or conflicting commit history
- changes that do not look suitable for a patch release

If any significant concern is present, use the ask-user flow to confirm whether to proceed with the changelog update.

## Changelog writing rules

- Insert the new release section at the top, immediately above the current latest release entry.
- Use the current date in `YYYY-MM-DD` format.
- Preserve the existing structure and tone of the file.
- Group entries under Keep a Changelog headings such as `Added`, `Changed`, and `Fixed`.
- Reuse the repository's existing subheadings where they fit, such as `#### Booking Flow`, `#### Notifications`, or `#### Infrastructure & DevOps`.
- Write concise, reader-friendly summaries of the actual changes rather than copying commit subjects verbatim.
- Do not create an `Unreleased` section unless the user explicitly asks for one.
- Add the new release link at the bottom using the Git tag form `v<version>`.

## Practical guidance

- Prefer Git commands that inspect the tag and commit range, such as `git describe --tags --abbrev=0` and `git log <tag>..HEAD`.
- If the commit subjects alone are not enough, inspect commit bodies and file stats before writing the review or changelog entry.
- If there are no commits since the last tag, tell the user plainly and do not modify the changelog.
- Keep the review separate from the file edit: review first, then update after any required user confirmation.
