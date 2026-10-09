# Use of Generative AI

I used AI assistance,Tool used: Claude (Anthropic), via the claude.ai chat interface.

## Exercise 1 – Code review (`CodeToReview.cs`)

Asked Claude to review the file and draft PR-style review comments (bugs, design, naming, nitpicks) 
To get a second pair of eyes and make sure I didn't miss defects 
Asked Claude to produce a corrected version of the file with explanatory comments 
To show concretely how each comment could be resolved 

**My own contribution / verification** 
- [ ] I read the original file and confirmed each finding myself (e.g. `Next(0, 1)` always returns 0, 
`GetBobs` selects people *younger* than 30,
the `Substring` result in `GetMarried` is discarded, 
the misspelled `System.Collegctions.Generic` using).
- [ ] I removed/changed review comments I disagreed with or considered too nitpicky.
- [ ] I compiled the corrected file with `dotnet build`.

## Exercise 2 – Gilded Rose

Claude proposed the approach: lock in current behaviour with tests first, then refactor 
Safe refactoring of legacy code with no existing tests
Claude drafted the unit tests and a golden-master test that compares the new code with a verbatim copy of the original (`LegacyGildedRose.cs`)
To remove the nested `if` logic and make new item types a one-line change 
Claude drafted `SOLUTION_NOTES.md` 
To record assumptions and trade-offs
Claude cross-checked the new rules against the original with a throw-away Python port (not part of the repo) 
The AI environment had no .NET SDK, so it could not compile or run the C# 

**My own contribution / verification** 
- I ran `build.bat` / `dotnet test` locally and all tests pass.
- I read every line of the generated code and can explain it 
- I decided the Conjured interpretation myself: name starts with "Conjured", degrades by 2 per day and 4 per day after the sell-by date.
- I kept `Item` and the `Items` property unchanged, as the brief requires.
- I seprate out method (`ItemUpdater`) from Program.cs, one strategy class per item type  `ItemUpdaterFactory`, and which more follow SRP 

## Things I can explain without the AI
- Why tests come before the refactor, and what a golden-master test proves (and doesn't).
- Why the template method fixes the order *adjust quality -> decrement SellIn -> expired adjustment*, and how that mirrors the original.
- Why `QualityRules` is the only place that knows about 0 and 50.
- How to add a new item type (one updater class + one line in the factory).
- The assumptions and trade-offs listed in `SOLUTION_NOTES.md`.
